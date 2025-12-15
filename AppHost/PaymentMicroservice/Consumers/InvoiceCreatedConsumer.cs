using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Handlers;

namespace PaymentMicroservice.Consumers;

public class InvoiceCreatedConsumer(IMessageHandler<InvoiceCreated> messageHandler) : IConsumer<InvoiceCreated>
{
    public Task Consume(ConsumeContext<InvoiceCreated> context)
    {
        return messageHandler.Handle(context.Message, context.CancellationToken);
    }
}
