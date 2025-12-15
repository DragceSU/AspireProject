namespace PaymentMicroservice;

public class AppConfiguration
{
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
}

public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public string QueueName { get; set; } = "payment-microservice";
    public string ExchangeName { get; set; } = "invoice-service";
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
}
