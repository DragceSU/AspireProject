namespace MessageContracts.Messages.Order;

public record OrderAck(
    string OrderId,
    DateTimeOffset ReceivedAt,
    decimal Total,
    string Currency,
    string Status,
    string? Notes
);