namespace MessageContracts.Messages.Invoice;

public class InvoiceItems : Message
{
    public InvoiceItems()
    {
        Type = MessageType.InvoiceItems;
    }

    public required string Description { get; set; }
    public double Price { get; set; }
    public double ActualMileage { get; set; }
    public double BaseRate { get; set; }
    public bool IsOversized { get; set; }
    public bool IsRefrigerated { get; set; }
    public bool IsHazardousMaterial { get; set; }
}