namespace WebApi.Service;

public class AppConfiguration
{
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000"];
}

public class RabbitMqConfiguration
{
    public string Host { get; set; } = "host.docker.internal";
}
