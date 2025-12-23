using InvoiceMicroservice;
using InvoiceMicroservice.Consumers;
using MassTransit;
using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;
using Messaging.RabbitMq.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using KafkaProducer = Messaging.Kafka.Producers;
using RabbitMQProducer = Messaging.RabbitMq.Producers;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderConsumer>();

    x.AddRider(rider =>
    {
        rider.AddProducer<InvoiceCreated>(typeof(InvoiceCreated).FullName!.ToLower(), (context, producerConfig) =>
        {
            producerConfig.EnableDeliveryReports = true;
            producerConfig.EnableIdempotence = true;      // optional, for stronger guarantees
            producerConfig.MessageSendMaxRetries = 10;
        });

        rider.UsingKafka((context, k) =>
        {
            var kafkaOptions = context.GetRequiredService<IOptions<AppConfiguration>>().Value.Kafka;
            var bootstrapServers = string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers) ? "localhost:9092" : kafkaOptions.BootstrapServers;
            k.Host(bootstrapServers);
        });
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
        var rabbitConfig = options.RabbitMq;
        var hostName = ResolveRabbitHost(rabbitConfig.Host);
        var queueName = string.IsNullOrWhiteSpace(rabbitConfig.QueueName) ? "invoice-order-submission" : rabbitConfig.QueueName;
        var exchangeName = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeName) ? "invoice-order-service" : rabbitConfig.ExchangeName;
        var exchangeType = string.IsNullOrWhiteSpace(rabbitConfig.ExchangeType) ? "fanout" : rabbitConfig.ExchangeType;

        cfg.Host(hostName);

        // Below declaration will use competing consumers pattern by default. To change this behavior, we need to set different queue name for each instance.
        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.Bind(exchangeName, bind => bind.ExchangeType = exchangeType);
            e.ConfigureConsumer<OrderConsumer>(context);
        });
    });
});

builder.Services.AddSingleton<IMessageHandler<OrderSubmission>, Messaging.RabbitMq.Handlers.MessageHandler<OrderSubmission>>();
builder.Services.AddScoped(typeof(RabbitMQProducer.IMessagePublisher<>), typeof(RabbitMQProducer.MessagePublisher<>));
builder.Services.AddScoped(typeof(KafkaProducer.IMessagePublisher<>), typeof(KafkaProducer.MessagePublisher<>));
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
