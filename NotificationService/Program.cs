using MassTransit;
using NotificationService.Consumers;
using Serilog;
using Serilog.Events;

// ── Bootstrap logger (captures startup failures) ─────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
        .ConfigureServices((ctx, services) =>
        {
            // ── MassTransit + RabbitMQ ────────────────────────────────────────
            services.AddMassTransit(x =>
            {
                // Register all consumers
                x.AddConsumer<OrderPlacedConsumer>();
                x.AddConsumer<UserRegisteredConsumer>();
                x.AddConsumer<ProductChangedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    var rmq = ctx.Configuration.GetSection("RabbitMQ");
                    cfg.Host(rmq["Host"] ?? "localhost", rmq["VHost"] ?? "/", h =>
                    {
                        h.Username(rmq["Username"] ?? "guest");
                        h.Password(rmq["Password"] ?? "guest");
                    });

                    // Auto-create queues for every consumer
                    cfg.ConfigureEndpoints(context);
                });
            });
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
