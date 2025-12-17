using System.ComponentModel.DataAnnotations;

namespace MessageContracts.Messages.Order;

public class OrderSubmission : Message
{
    public OrderSubmission()
    {
        Type = MessageType.OrderSubmission;
    }

    public string OrderId { get; init; } = string.Empty;

    public List<OrderItem> Items { get; init; } = new();

    [Range(0, double.MaxValue)]
    public decimal Total { get; init; }


    public string Currency { get; init; } = "USD";

 
    public string? Notes { get; init; }

    public DateTimeOffset PlacedAt { get; init; } = DateTimeOffset.UtcNow;
}
