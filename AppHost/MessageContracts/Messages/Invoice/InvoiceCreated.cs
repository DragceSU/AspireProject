namespace MessageContracts.Messages.Invoice;

public class InvoiceCreated : Message
{
    public InvoiceCreated()
    {
        Type = MessageType.InvoiceCreated;
    }

    public int InvoiceNumber { get; set; }
    public InvoiceToCreate? InvoiceData { get; set; }
}