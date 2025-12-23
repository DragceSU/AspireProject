using MessageContracts.Messages.Invoice;

namespace InvoiceMicroservice;

public class AppConfiguration
{
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
    public KafkaConfiguration Kafka { get; set; } = new();
}

public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public string QueueName { get; set; } = "invoice-order-submission";
    public string ExchangeName { get; set; } = "invoice-order-service";
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
}

public class KafkaConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = typeof(InvoiceCreated).FullName?.ToLowerInvariant() ?? "invoicecreated";
    public string ConsumerGroup { get; set; } = "payment-microservice";
}