namespace InvoiceMicroservice;

public class AppConfiguration
{
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
}

public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public string QueueName { get; set; } = "invoice-order-submission";
    public string ExchangeName { get; set; } = "invoice-order-service";
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
}
