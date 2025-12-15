namespace MessageContracts.Messages;

public abstract class Message
{
    public MessageType Type { get; set; } = MessageType.Unknown;
    public string MessageId { get; set; } = string.Empty;
}
