namespace MessageContracts.Messages.Invoice;

public class InvoiceItem : Message
{
    public InvoiceItem()
    {
        Type = MessageType.InvoiceItems;
    }

    public string ProductId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}