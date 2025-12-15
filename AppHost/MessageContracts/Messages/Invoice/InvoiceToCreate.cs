namespace MessageContracts.Messages.Invoice;

public class InvoiceToCreate : Message
{
    public InvoiceToCreate()
    {
        Type = MessageType.InvoiceToCreate;
    }

    public int CustomerNumber { get; set; }
    public List<InvoiceItems>? InvoiceItems { get; set; }
}