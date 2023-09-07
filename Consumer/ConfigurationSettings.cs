namespace Consumer;

public sealed class ConfigurationSettings
{
    public const string Section = "Configuration";

    public bool SecureProtocol { get; init; }

    public string HostName { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int HeartBeatIntervalInMilliseconds { get; init; } = 2000;

    public int HeartBeatTimeoutInMilliseconds { get; init; } = 5000;

    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan WebSocketClientMonitorInterval { get; init; } = TimeSpan.FromSeconds(20);

    public string WebSocketUrl => $"{(SecureProtocol ? "wss" : "ws")}://{HostName}/";

    public string HttpUrl => $"{(SecureProtocol ? "https" : "http")}://{HostName}/";
}