using System.ComponentModel.DataAnnotations;

namespace WebApi.Service.Dtos;

public class OrderItemDto
{
    [Required]
    [StringLength(32)]
    public string ProductId { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; init; }
}