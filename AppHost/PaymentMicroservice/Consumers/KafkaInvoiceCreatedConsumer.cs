using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Kafka.Handlers;

namespace PaymentMicroservice.Consumers;

public class KafkaInvoiceCreatedConsumer(IMessageHandler<InvoiceCreated> messageHandler) : IConsumer<InvoiceCreated>
{
    public Task Consume(ConsumeContext<InvoiceCreated> context)
    {
        return messageHandler.Handle(context.Message, context.CancellationToken);
    }
}