using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;
using WebApi.Service.Dtos;

namespace WebApi.Service.Extensions;

public static class MappingExtensions
{
    public static OrderSubmission ToOrderSubmissionMessage(this OrderSubmissionDto dto)
    {
        return new OrderSubmission
        {
            MessageId = dto.OrderId,
            OrderId = dto.OrderId,
            Items = ToOrderItemMessages(dto.Items).ToList(),
            Total = dto.Total,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency,
            Notes = dto.Notes,
            PlacedAt = dto.PlacedAt
        };
    }

    public static IEnumerable<OrderItem> ToOrderItemMessages(this IEnumerable<OrderItemDto> orderItemDtos) =>
        orderItemDtos.Select(dto => dto.ToOrderItemMessage());

    public static OrderItem ToOrderItemMessage(this OrderItemDto dto)
    {
        return new OrderItem
        {
            ProductId = dto.ProductId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };
    }

    public static IEnumerable<InvoiceItem> ToInvoiceItems(this IEnumerable<OrderItem> orderItemDtos) =>
        orderItemDtos.Select(dto => dto.ToInvoiceItem());

    public static InvoiceItem ToInvoiceItem(this OrderItem dto)
    {
        return new InvoiceItem
        {
            ProductId = dto.ProductId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };
    }
}
