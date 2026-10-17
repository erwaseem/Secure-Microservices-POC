using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Application.Interfaces;
using ProductService.Infrastructure.Data;
using ProductService.Infrastructure.Repositories;
using Serilog;
using System.Text;

// ── Bootstrap logger (captures startup failures) ─────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ProductDb>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("ProductDb")));

    // ── JWT (must match AuthService and API Gateway) ──────────────────────────
    var jwt = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwt["Issuer"],
                ValidAudience            = jwt["Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    // ── DI ────────────────────────────────────────────────────────────────────
    builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // ── MassTransit + RabbitMQ ────────────────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            var rmq = builder.Configuration.GetSection("RabbitMQ");
            cfg.Host(rmq["Host"] ?? "localhost", rmq["VHost"] ?? "/", h =>
            {
                h.Username(rmq["Username"] ?? "guest");
                h.Password(rmq["Password"] ?? "guest");
            });
            cfg.ConfigureEndpoints(ctx);
        });
    });

    // ── MVC + Swagger ─────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ── CORS (allow API Gateway) ──────────────────────────────────────────────
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins("http://localhost:5020", "https://localhost:7028")
             .AllowAnyHeader()
             .AllowAnyMethod()));

    var app = builder.Build();

    // ── Auto-migrate on startup ───────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ProductDb>();
        db.Database.Migrate();
    }

    // ── Serilog request logging ───────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    // ── Global JSON error handler ─────────────────────────────────────────────
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        ctx.Response.StatusCode  = 500;
        ctx.Response.ContentType = "application/json";
        var msg = app.Environment.IsDevelopment() && ex is not null
                  ? ex.ToString().Replace("\"", "'")
                  : "An unexpected error occurred. Please try again.";
        await ctx.Response.WriteAsync($"{{\"error\":\"{msg}\"}}");
    }));

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ProductService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
