using MassTransit;
using MessageContracts.Messages.Order;
using Messaging.Handlers;

namespace InvoiceMicroservice.Consumers;

public class OrderConsumer(IMessageHandler<OrderSubmission> messageHandler) : IConsumer<OrderSubmission>
{
    public Task Consume(ConsumeContext<OrderSubmission> context)
    {
        return messageHandler.Handle(context.Message, context.CancellationToken);
    }
}
