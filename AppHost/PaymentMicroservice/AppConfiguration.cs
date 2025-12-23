using MessageContracts.Messages.Invoice;

namespace PaymentMicroservice;

public class AppConfiguration
{
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
    public KafkaConfiguration Kafka { get; set; } = new();
}

public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public string QueueName { get; set; } = "payment-microservice";
    public string ExchangeName { get; set; } = "payment-service";
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
}

public class KafkaConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:29092";
    public string Topic { get; set; } = typeof(InvoiceCreated).FullName!.ToLowerInvariant();
    public string ConsumerGroup { get; set; } = "payment-microservice";
}
