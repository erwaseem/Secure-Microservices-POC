using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureMicroservicesPOC.Application.Interfaces;
using SecureMicroservicesPOC.Infrastructure.Data;
using SecureMicroservicesPOC.Infrastructure.Middleware;
using SecureMicroservicesPOC.Infrastructure.Repositories;
using SecureMicroservicesPOC.Infrastructure.Security;
using Serilog;
using System.Text;

//  Bootstrap logger (captures startup failures) 
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    //  Serilog ----------------------
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // ---------- Database ------------------
    var conn = builder.Configuration.GetConnectionString("AuthDb")
               ?? "Server=.;Database=SecureAuthDb;Trusted_Connection=True;TrustServerCertificate=True";

    builder.Services.AddDbContext<AuthDb>(o => o.UseSqlServer(conn));

    // --------------- JWT -----------
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
                ValidIssuer              = jwt["Issuer"],
                ValidAudience            = jwt["Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
                ClockSkew                = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    // ----------- DI -----------------------
    builder.Services.AddScoped<IAuthRepository,  AuthRepository>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    // ----------- MassTransit + RabbitMQ ----------------
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

    // ------------- MVC + Swagger --------------
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ---------------- CORS (allow API Gateway) --------------
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins("http://localhost:5020", "https://localhost:7028")
             .AllowAnyHeader()
             .AllowAnyMethod()));

    var app = builder.Build();

    // ------------- Auto-migrate on startup ---------------------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AuthDb>();
        db.Database.Migrate();
    }

    // ----------- Serilog request logging ----------------
    app.UseSerilogRequestLogging();

    // -------------- Global exception middleware -----------------
    app.UseMiddleware<GlobalExceptionMiddleware>();

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
    Log.Fatal(ex, "Auth-Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
