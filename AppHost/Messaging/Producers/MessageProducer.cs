using MassTransit;
using MessageContracts.Messages;
using Microsoft.Extensions.Logging;

namespace Messaging.Producers;

public class MessageProducer<T>(IPublishEndpoint publishEndpoint, ILogger<MessageProducer<T>> logger)
    : IMessageProducer<T> where T : Message
{
    public async Task Produce(T message, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(message, cancellationToken);
        logger.LogInformation("Published message of type {MessageType}", typeof(T).Name);
    }
}