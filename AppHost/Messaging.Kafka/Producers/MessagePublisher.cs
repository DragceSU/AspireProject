using MassTransit;
using MessageContracts.Messages;
using Microsoft.Extensions.Logging;

namespace Messaging.Kafka.Producers;

public class MessagePublisher<T>(ITopicProducer<T> publishEndpoint, ILogger<MessagePublisher<T>> logger)
    : IMessagePublisher<T> where T : Message
{
    public async Task Publish(T message, CancellationToken cancellationToken)
    {
        await publishEndpoint.Produce(message, cancellationToken);
        logger.LogInformation("Published message of type {MessageType}", typeof(T).Name);
    }
}