using MessageContracts.Messages;
using Messaging.Kafka.Producers;

namespace WebApi.Service.Services.Producer;

public class KafkaProducer<T>(IMessagePublisher<T> publisher) : IMessageProducer<T> where T : Message
{
    public PublisherType Type => PublisherType.Kafka;

    public async Task Publish(T message, CancellationToken cancellationToken)
    {
        await publisher.Publish(message, cancellationToken);
    }
}