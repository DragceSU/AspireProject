using MessageContracts.Messages;
using Messaging.RabbitMq.Producers;

namespace WebApi.Service.Services.Producer;

public class RabbitMqMessageProducer<T>(IMessagePublisher<T> publisher) : IMessageProducer<T> where T : Message
{
    public PublisherType Type => PublisherType.RabbitMq;

    public async Task Publish(T message, CancellationToken cancellationToken)
    {
        await publisher.Publish(message, cancellationToken);
    }
}