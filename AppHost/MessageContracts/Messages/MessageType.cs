namespace MessageContracts.Messages;

public enum MessageType
{
    Unknown = -1,
    InvoiceCreated = 0,
    InvoiceItems = 1,
    InvoiceToCreate = 2
}
