using MessageContracts.Messages;

namespace Messaging.RabbitMq.Handlers;

public interface IMessageHandler<in T> where T : Message
{
    Task Handle(T message, CancellationToken cancellationToken);
}