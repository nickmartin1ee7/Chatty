namespace ChatHubClient;

public record Message(User Sender, string Content, User? Recipient = null)
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
