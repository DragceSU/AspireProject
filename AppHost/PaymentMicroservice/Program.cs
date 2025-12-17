using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PaymentMicroservice;
using PaymentMicroservice.Consumers;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InvoiceCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
        var rabbitConfig = options.RabbitMq ?? new RabbitMqConfiguration();
        var hostName = ResolveRabbitHost(rabbitConfig.Host);
        var queueName = string.IsNullOrWhiteSpace(rabbitConfig.QueueName) ? "payment-microservice" : rabbitConfig.QueueName;
        var exchangeName = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeName) ? "payment-service" : rabbitConfig.ExchangeName;
        var exchangeType = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeType) ? "fanout" : rabbitConfig.ExchangeType;

        cfg.Host(hostName);

        // Below declaration will use competing consumers pattern by default. To change this behavior, we need to set different queue name for each instance.
        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.Bind(exchangeName, x => x.ExchangeType = exchangeType);
            e.ConfigureConsumer<InvoiceCreatedConsumer>(context);
        });
    });
});

builder.Services.AddScoped<IMessageHandler<InvoiceCreated>, Messaging.Handlers.MessageHandler<InvoiceCreated>>();
builder.Services.AddLogging();

var host = builder.Build();
await host.RunAsync();

static string ResolveRabbitHost(string? configuredHost)
{
    var hostName = string.IsNullOrWhiteSpace(configuredHost) ? "host.docker.internal" : configuredHost;
    if (!IsRunningInContainer())
    {
        if (string.IsNullOrWhiteSpace(configuredHost) ||
            string.Equals(configuredHost, "host.docker.internal", StringComparison.OrdinalIgnoreCase))
        {
            hostName = "localhost";
        }
    }

    return hostName;
}

static bool IsRunningInContainer()
{
    var aspireResource = Environment.GetEnvironmentVariable("ASPIRE_RESOURCE_NAME");
    if (!string.IsNullOrWhiteSpace(aspireResource))
    {
        return true;
    }

    var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
    return string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase);
}
