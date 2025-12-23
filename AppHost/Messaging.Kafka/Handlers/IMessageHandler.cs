using MessageContracts.Messages;

namespace Messaging.Kafka.Handlers;

public interface IMessageHandler<in T> where T : Message
{
    Task Handle(T message, CancellationToken cancellationToken);
}