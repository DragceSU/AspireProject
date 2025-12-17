using System.ComponentModel.DataAnnotations;

namespace WebApi.Service.Dtos;

public class OrderSubmissionDto
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string OrderId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<OrderItemDto> Items { get; init; } = new();

    [Range(0, double.MaxValue)]
    public decimal Total { get; init; }

    [StringLength(8)]
    public string Currency { get; init; } = "USD";

    [MaxLength(512)]
    public string? Notes { get; init; }

    public DateTimeOffset PlacedAt { get; init; } = DateTimeOffset.UtcNow;
}
