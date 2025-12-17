namespace WebApi.Service.Dtos;

public record OrderAcknowledgementDto(
    string OrderId,
    DateTimeOffset ReceivedAt,
    decimal Total,
    string Currency,
    string Status,
    string? Notes
);