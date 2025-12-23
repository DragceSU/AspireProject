using MassTransit;
using MessageContracts.Messages;
using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;
using Microsoft.Extensions.Options;
using RabbitMq = Messaging.RabbitMq.Producers;
using Kafka = Messaging.Kafka.Producers;
using WebApi.Service;
using WebApi.Service.Services.Producer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddRider(rider =>
    {
        rider.AddProducer<OrderSubmission>(typeof(OrderSubmission).FullName!.ToLower(), (context, producerConfig) =>
        {
            producerConfig.EnableDeliveryReports = true;
            producerConfig.EnableIdempotence = true;      // optional, for stronger guarantees
            producerConfig.MessageSendMaxRetries = 10;
        });

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

        cfg.Host(hostName);
    });
});

builder.Services.AddScoped(typeof(RabbitMq.IMessagePublisher<>), typeof(RabbitMq.MessagePublisher<>));
builder.Services.AddScoped(typeof(Kafka.IMessagePublisher<>), typeof(Kafka.MessagePublisher<>));

builder.Services.AddScoped(typeof(IMessageProducer<OrderSubmission>), typeof(RabbitMqMessageProducer<OrderSubmission>));
builder.Services.AddScoped(typeof(IMessageProducer<InvoiceCreated>), typeof(RabbitMqMessageProducer<InvoiceCreated>));
builder.Services.AddScoped(typeof(IMessageProducer<OrderSubmission>), typeof(KafkaProducer<OrderSubmission>));
builder.Services.AddScoped(typeof(IMessageProducer<InvoiceCreated>), typeof(KafkaProducer<InvoiceCreated>));

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppCors", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("WebAppCors");

app.MapControllers();

app.UseSwagger();

app.UseSwaggerUI();

app.Run();

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
    if (!string.IsNullOrWhiteSpace(aspireResource)) return true;

    var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
    return string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
