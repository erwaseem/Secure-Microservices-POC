using Cart_Services.Application.Interfaces;
using Cart_Services.Infrastructure.Data;
using Cart_Services.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ------ Bootstrap logger (captures startup failures) -----------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog -------------------
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // -------- Database ----------
    builder.Services.AddDbContext<CartDb>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("CartDb")));

    // ---- DI --------
    builder.Services.AddScoped<ICartRepository, CartRepository>();

    // ----- MassTransit + RabbitMQ -----------
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

    // ── HttpClient for ProductService with Polly (retry + circuit breaker) ────
    // Registered here — used when Cart needs to call ProductService directly
    builder.Services.AddHttpClient("ProductService", c =>
        c.BaseAddress = new Uri(
            builder.Configuration["DownstreamServices:ProductService"] ?? "http://localhost:5138"))
        .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
        .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

    // ---------- MVC + Swagger ------------
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // --------- CORS (allow API Gateway) ---------
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.WithOrigins("http://localhost:5020", "https://localhost:7028")
             .AllowAnyHeader()
             .AllowAnyMethod()));

    var app = builder.Build();

    // ------ Auto-migrate on startup ---------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CartDb>();
        db.Database.Migrate();
    }

    // --------- Serilog request logging ---------
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cart_Services terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
