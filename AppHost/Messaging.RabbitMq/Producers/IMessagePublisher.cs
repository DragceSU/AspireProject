using MessageContracts.Messages;

namespace Messaging.RabbitMq.Producers;

public interface IMessagePublisher<in T> where T : Message
{
    Task Publish(T message, CancellationToken cancellationToken);
}