using MessageContracts.Messages;

namespace Messaging.Producers;

public interface IMessageProducer<in T> where T : Message
{
    Task Produce(T message, CancellationToken cancellationToken);
}