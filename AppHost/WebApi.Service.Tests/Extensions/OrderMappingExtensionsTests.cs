using System.Linq;
using WebApi.Service.Dtos;
using WebApi.Service.Extensions;
using NUnit.Framework;

namespace WebApi.Service.Tests.Extensions;

public class OrderMappingExtensionsTests
{
    [Test]
    public void ToOrderSubmissionMessage_ShouldMapAllFieldsAndFallbackCurrency()
    {
        var placedAt = new DateTimeOffset(2025, 12, 17, 12, 0, 0, TimeSpan.Zero);
        var dto = new OrderSubmissionDto
        {
            OrderId = "ORD-001",
            Currency = "",
            Notes = "Deliver ASAP",
            PlacedAt = placedAt,
            Total = 100m,
            Items =
            [
                new OrderItemDto
                {
                    ProductId = "trolley",
                    Name = "Expedition Trolley",
                    Quantity = 2,
                    UnitPrice = 50m
                }
            ]
        };

        var message = dto.ToOrderSubmissionMessage();

        Assert.That(message.OrderId, Is.EqualTo(dto.OrderId));
        Assert.That(message.MessageId, Is.EqualTo(dto.OrderId));
        Assert.That(message.Total, Is.EqualTo(dto.Total));
        Assert.That(message.Currency, Is.EqualTo("USD"));
        Assert.That(message.Notes, Is.EqualTo(dto.Notes));
        Assert.That(message.PlacedAt, Is.EqualTo(placedAt));
        Assert.That(message.Items, Has.Count.EqualTo(1));
        var item = message.Items.Single();
        Assert.That(item.ProductId, Is.EqualTo("trolley"));
        Assert.That(item.Name, Is.EqualTo("Expedition Trolley"));
        Assert.That(item.Quantity, Is.EqualTo(2));
        Assert.That(item.UnitPrice, Is.EqualTo(50m));
    }

    [Test]
    public void ToOrderItemMessage_ShouldMapValueTypes()
    {
        var dto = new OrderItemDto
        {
            ProductId = "laptop",
            Name = "Nebula Laptop",
            Quantity = 1,
            UnitPrice = 2190m
        };

        var message = dto.ToOrderItemMessage();

        Assert.That(message.ProductId, Is.EqualTo(dto.ProductId));
        Assert.That(message.Name, Is.EqualTo(dto.Name));
        Assert.That(message.Quantity, Is.EqualTo(dto.Quantity));
        Assert.That(message.UnitPrice, Is.EqualTo(dto.UnitPrice));
    }
}
