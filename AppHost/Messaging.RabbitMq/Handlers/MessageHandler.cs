using MessageContracts.Messages;
using Microsoft.Extensions.Logging;

namespace Messaging.RabbitMq.Handlers;

public class MessageHandler<T>(ILogger<MessageHandler<T>> logger) : IMessageHandler<T> where T : Message
{
    public Task Handle(T message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            logger.LogWarning("Received null invoice message.");
            return Task.CompletedTask;
        }

        logger.LogInformation("Received message with type {MessageType} and invoice number {InvoiceNumber}", message.Type, message.MessageId);
        return Task.CompletedTask;
    }
}