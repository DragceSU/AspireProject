using MessageContracts.Messages;
using NSubstitute;
using NUnit.Framework;
using Messaging.Producers;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceMicroservice.Tests.Producers;

[TestFixture]
public class MessageProducerTests
{
    [Test]
    public async Task Produce_Delegates_To_PublishEndpoint()
    {
        var publishEndpoint = Substitute.For<MassTransit.IPublishEndpoint>();
        var logger = NullLogger<MessageProducer<TestMessage>>.Instance;
        var producer = new MessageProducer<TestMessage>(publishEndpoint, logger);
        var message = new TestMessage();

        await producer.Produce(message, CancellationToken.None);

        await publishEndpoint.Received(1).Publish(message, CancellationToken.None);
    }

    private sealed class TestMessage : Message
    {
        public TestMessage()
        {
            Type = MessageType.Unknown;
            MessageId = Guid.NewGuid().ToString();
        }
    }
}
