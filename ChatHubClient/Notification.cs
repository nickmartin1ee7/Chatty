namespace ChatHubClient;

public static class Notification
{
    // Subscriptions
    public static class Subscription
    {
        public const string ReceiveMessage = "ReceiveMessage";
        public const string ErrorMessage = "ErrorMessage";
        public const string UserConnected = "UserConnected";
        public const string UserDisconnected = "UserDisconnected";
        public const string UsernameRegistered = "UsernameRegistered";
    }

    public static class Action
    {
        public const string SendMessage = "SendMessage";
        public const string RegisterUsername = "RegisterUsername";
    }
}
