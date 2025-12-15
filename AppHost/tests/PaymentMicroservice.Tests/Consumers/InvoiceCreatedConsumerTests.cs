using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Handlers;
using NSubstitute;
using NUnit.Framework;
using PaymentMicroservice;
using PaymentMicroservice.Consumers;

namespace PaymentMicroservice.Tests.Consumers;

[TestFixture]
public class InvoiceCreatedConsumerTests
{
    [Test]
    public async Task Consume_Delegates_To_MessageHandler()
    {
        var handler = Substitute.For<IMessageHandler<InvoiceCreated>>();
        var consumer = new InvoiceCreatedConsumer(handler);
        var message = new InvoiceCreated { InvoiceNumber = 42, InvoiceData = new InvoiceToCreate { CustomerNumber = 123 } };
        var context = Substitute.For<ConsumeContext<InvoiceCreated>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await handler.Received(1).Handle(message, CancellationToken.None);
    }
}
