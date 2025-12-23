using MassTransit;
using MessageContracts.Messages.Invoice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PaymentMicroservice;
using PaymentMicroservice.Consumers;
using KafkaHandlers = Messaging.Kafka.Handlers;
using RabbitHandlers = Messaging.RabbitMq.Handlers;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RabbitMqInvoiceCreatedConsumer>();
    x.AddRider(rider =>
    {
        rider.AddConsumer<KafkaInvoiceCreatedConsumer>();

        rider.UsingKafka((context, config) =>
        {
            var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
            var kafka = options.Kafka;
            var bootstrapServers = string.IsNullOrWhiteSpace(kafka.BootstrapServers)
                ? "localhost:9092"
                : kafka.BootstrapServers;
            var topic = typeof(InvoiceCreated).FullName!.ToLowerInvariant();
            var groupId = string.IsNullOrWhiteSpace(kafka.ConsumerGroup) ? "payment-microservice" : kafka.ConsumerGroup;

            config.Host(bootstrapServers);
            config.TopicEndpoint<InvoiceCreated>(topic, groupId, endpoint =>
            {
                endpoint.ConfigureConsumer<KafkaInvoiceCreatedConsumer>(context);
            });
        });
    });

    x.UsingRabbitMq((context, config) =>
    {
        var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
        var rabbitConfig = options.RabbitMq ?? new RabbitMqConfiguration();
        var hostName = ResolveRabbitHost(rabbitConfig.Host);
        var queueName = string.IsNullOrWhiteSpace(rabbitConfig.QueueName) ? "payment-microservice" : rabbitConfig.QueueName;
        var exchangeName = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeName) ? "payment-service" : rabbitConfig.ExchangeName;
        var exchangeType = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeType) ? "fanout" : rabbitConfig.ExchangeType;

        config.Host(hostName);

        // Below declaration will use competing consumers pattern by default. To change this behavior, we need to set different queue name for each instance.
        config.ReceiveEndpoint(queueName, e =>
        {
            e.Bind(exchangeName, x => x.ExchangeType = exchangeType);
            e.ConfigureConsumer<RabbitMqInvoiceCreatedConsumer>(context);
        });
    });
});

builder.Services.AddScoped<RabbitHandlers.IMessageHandler<InvoiceCreated>, RabbitHandlers.MessageHandler<InvoiceCreated>>();
builder.Services.AddScoped<KafkaHandlers.IMessageHandler<InvoiceCreated>, KafkaHandlers.MessageHandler<InvoiceCreated>>();
//builder.Services.AddScoped<IConsumer<InvoiceCreated>, KafkaInvoiceCreatedConsumer>();
builder.Services.AddLogging();

var host = builder.Build();
var startupLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var startupOptions = host.Services.GetRequiredService<IOptions<AppConfiguration>>().Value;
startupLogger.LogInformation(
    "Kafka config: BootstrapServers={BootstrapServers}, Topic={Topic}, ConsumerGroup={ConsumerGroup}",
    startupOptions.Kafka.BootstrapServers,
    typeof(InvoiceCreated).Name,
    startupOptions.Kafka.ConsumerGroup);
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
