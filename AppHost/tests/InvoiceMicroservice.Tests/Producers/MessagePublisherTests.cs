using MessageContracts.Messages;
using Messaging.RabbitMq.Producers;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceMicroservice.Tests.Producers;

[TestFixture]
public class MessagePublisherTests
{
    [Test]
    public async Task Produce_Delegates_To_PublishEndpoint()
    {
        var publishEndpoint = Substitute.For<MassTransit.IPublishEndpoint>();
        var logger = NullLogger<MessagePublisher<TestMessage>>.Instance;
        var producer = new MessagePublisher<TestMessage>(publishEndpoint, logger);
        var message = new TestMessage();

        await producer.Publish(message, CancellationToken.None);

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
