using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy;

//  Bootstrap logger (captures startup failures) 
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    //  Serilog 
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    //  JWT 
    var jwt = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero,
                ValidIssuer              = jwt["Issuer"],
                ValidAudience            = jwt["Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
            };

            o.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    ctx.NoResult();
                    ctx.Response.StatusCode  = 401;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync($"{{\"error\":\"Invalid token: {ctx.Exception.Message}\"}}");
                }
            };
        });

    builder.Services.AddAuthorization();

    //  YARP Reverse Proxy 
    builder.Services
        .AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    //  Polly: resilient named HttpClients for downstream services 
    builder.Services.AddHttpClient("AuthService", c =>
        c.BaseAddress = new Uri("http://localhost:5001"))
        .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
        .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

    builder.Services.AddHttpClient("ProductService", c =>
        c.BaseAddress = new Uri("http://localhost:5138"))
        .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
        .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

    builder.Services.AddHttpClient("CartService", c =>
        c.BaseAddress = new Uri("http://localhost:5164"))
        .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
        .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

    //  Swagger 
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Secure API Gateway", Version = "v1" });
        var scheme = new OpenApiSecurityScheme
        {
            Name        = "Authorization",
            Type        = SecuritySchemeType.Http,
            Scheme      = "bearer",
            BearerFormat = "JWT",
            In          = ParameterLocation.Header,
            Description = "Enter: Bearer {token}"
        };
        c.AddSecurityDefinition("Bearer", scheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
    });

    // ---- Rate Limiting -------
    builder.Services.AddRateLimiter(options =>
    {
        // GlobalLimiter: partitioned per IP, per path — no decoration needed
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var path = ctx.Request.Path.Value?.ToLower() ?? "";
            var ip   = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // /auth/** — strict: 5 requests per minute (brute-force protection)
            if (path.StartsWith("/auth"))
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"auth:{ip}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window       = TimeSpan.FromMinutes(1),
                        PermitLimit  = 5,
                        QueueLimit   = 0
                    });

            // Everything else — general: 100 requests per minute
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"global:{ip}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window       = TimeSpan.FromMinutes(1),
                    PermitLimit  = 100,
                    QueueLimit   = 10
                });
        });

        // Return 429 with JSON body when limit is exceeded
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                "{\"error\":\"Too many requests. Please try again later.\"}", ct);
        };
    });

    var app = builder.Build();

    //  Serilog request logging 
    app.UseSerilogRequestLogging();

    //  Swagger 
    app.UseSwagger();
    app.UseSwaggerUI();

    //  Auth 
    app.UseAuthentication();
    app.UseAuthorization();

    //  Protect write endpoints 
    app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value?.ToLower();

        if (path is not null && path.StartsWith("/api/products") &&
            (HttpMethods.IsPost(ctx.Request.Method)
            || HttpMethods.IsPut(ctx.Request.Method)
            || HttpMethods.IsDelete(ctx.Request.Method)))
        {
            if (!(ctx.User.Identity?.IsAuthenticated ?? false))
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized: Token required for write operations.");
                return;
            }
        }

        await next();
    });

    //  Global error handler 
    app.Use(async (ctx, next) =>
    {
        try
        {
            await next.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"error\":\"Something went wrong. Please try again later.\"}");
        }
    });

    // ── Rate Limiting middleware ──────────────────────────────────────────────
    app.UseRateLimiter();

    //  Reverse Proxy
    app.MapReverseProxy();

    //  Health check 
    app.MapGet("/", () => new
    {
        Status      = "Gateway is running",
        Uptime      = DateTime.UtcNow.ToString("u"),
        Environment = app.Environment.EnvironmentName
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API-Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
