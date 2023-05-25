namespace ChatHubClient;

public record Message(User Sender, string Content, DateTimeOffset Timestamp, User? Recipient = null)
{
    public Guid Id { get; } = Guid.NewGuid();
}
