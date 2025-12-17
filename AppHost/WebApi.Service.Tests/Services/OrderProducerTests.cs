using Messaging.Producers;
using Moq;
using WebApi.Service.Dtos;
using WebApi.Service.Services;
using NUnit.Framework;
using MessageContracts.Messages.Order;

namespace WebApi.Service.Tests.Services;

public class OrderProducerTests
{
    [Test]
    public async Task Publish_ShouldSendMappedMessageThroughProducer()
    {
        var dto = new OrderSubmissionDto
        {
            OrderId = "ORD-XYZ",
            Currency = "EUR",
            Notes = "Careful handling",
            Total = 2379m,
            Items =
            [
                new OrderItemDto
                {
                    ProductId = "laptop",
                    Name = "Nebula Laptop",
                    Quantity = 1,
                    UnitPrice = 2190m
                },
                new OrderItemDto
                {
                    ProductId = "trolley",
                    Name = "Expedition Trolley",
                    Quantity = 1,
                    UnitPrice = 189m
                }
            ]
        };

        var producerMock = new Mock<IMessageProducer<OrderSubmission>>();
        producerMock.Setup(p => p.Produce(It.IsAny<OrderSubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new OrderProducer(producerMock.Object);

        await sut.Publish(dto, CancellationToken.None);

        producerMock.Verify(p => p.Produce(
            It.Is<OrderSubmission>(message =>
                message.OrderId == dto.OrderId &&
                message.Currency == dto.Currency &&
                message.Items.Count == dto.Items.Count),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
