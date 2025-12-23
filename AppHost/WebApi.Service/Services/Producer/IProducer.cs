namespace WebApi.Service.Services.Producer;

public interface IMessageProducer<T> where T : class
{
    PublisherType Type { get; }

    Task Publish(T message, CancellationToken cancellationToken);
}