namespace ChatHubClient;

public interface IMessage
{
    Guid Id { get; }
    DateTimeOffset Timestamp { get; }
    User Sender { get; }
    string Content { get; }
    User? Recipient { get; }
    MessageType MessageType { get; }
}

public enum MessageType
{
    Chat, // Content from user represents a chat message
    System // Content from user (System) represents a server notice
}

/// <inheritdoc />
public record Message(MessageType MessageType, User Sender, string Content, User? Recipient = null) : IMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; }

    public Message WithTimestamp(DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        return this;
    }

    public bool IsValid() =>
        !string.IsNullOrEmpty(Content)
        && !string.IsNullOrEmpty(Sender.Username);
}
