namespace ChattyApp
{
    public class Settings
    {
        public SignalR SignalR { get; set; }
        public Telemetry Telemetry { get; set; }
    }

    public class SignalR
    {
        public string HubUrl { get; set; }
    }

    public class Telemetry
    {
        public string DeviceId { get; set; }
        public string LoggingUrl { get; set; }
        public string LoggingApiKey { get; set; }
    }
}