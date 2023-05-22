namespace ChatHubClient;

public record Message(User Sender, string Content, User? Recipient = null)
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
}
