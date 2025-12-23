using InvoiceMicroservice.Extensions;
using MassTransit;
using MessageContracts.Messages;
using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;
using Messaging.Kafka.Producers;
using Messaging.RabbitMq.Handlers;

namespace InvoiceMicroservice.Consumers;

public class OrderConsumer(IMessageHandler<OrderSubmission> messageHandler, IMessagePublisher<InvoiceCreated> kafkaOrderProducer)
    : IConsumer<OrderSubmission>
{
    public async Task Consume(ConsumeContext<OrderSubmission> context)
    {
        await messageHandler.Handle(context.Message, context.CancellationToken);

        var invoice = new InvoiceCreated
        {
            Type = MessageType.InvoiceCreated,
            InvoiceNumber = Random.Shared.Next(150, 15000),
            MessageId = context.Message.MessageId,
            InvoiceData = new InvoiceToCreate
            {
                MessageId = context.Message.MessageId,
                Type = MessageType.InvoiceToCreate,
                InvoiceItems = context.Message.Items.ToInvoiceItems().ToList()
            }
        };

        await kafkaOrderProducer.Publish(invoice, CancellationToken.None);
    }
}