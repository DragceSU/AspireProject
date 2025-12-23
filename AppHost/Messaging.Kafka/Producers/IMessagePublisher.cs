using MessageContracts.Messages;

namespace Messaging.Kafka.Producers;

public interface IMessagePublisher<in T> where T : Message
{
    Task Publish(T message, CancellationToken cancellationToken);
}