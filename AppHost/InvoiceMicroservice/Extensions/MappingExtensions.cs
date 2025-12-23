using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;

namespace InvoiceMicroservice.Extensions;

public static class MappingExtensions
{
    public static IEnumerable<InvoiceItem> ToInvoiceItems(this IEnumerable<OrderItem> orderItems) =>
        orderItems.Select(dto => dto.ToInvoiceItem());

    public static InvoiceItem ToInvoiceItem(this OrderItem orderItem) =>
        new()
        {
            ProductId = orderItem.ProductId,
            Name = orderItem.Name,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice
        };
}