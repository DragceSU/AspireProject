using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.RabbitMq.Handlers;

namespace PaymentMicroservice.Consumers;

public class RabbitMqInvoiceCreatedConsumer(IMessageHandler<InvoiceCreated> messageHandler) : IConsumer<InvoiceCreated>
{
    public Task Consume(ConsumeContext<InvoiceCreated> context)
    {
        return messageHandler.Handle(context.Message, context.CancellationToken);
    }
}