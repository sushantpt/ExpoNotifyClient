namespace ExpoNotifyClient.Common;

public static class ExpoPushConstants
{
    public const int MaxNotificationsPerRequest = 100;
    public const int MaxTicketIdsPerRequest = 1000;
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 4000;
    public const int MaxDataPayloadBytes = 4096;
    public const int MaxTtlSeconds = 2419200;
    public const int DefaultTimeoutSeconds = 30;
    public const int DefaultMaxRetries = 3;
    public const int DefaultBackoffMs = 1000;
    public const int MaxBackoffMs = 30000;

    internal const string PushSendPath = "/push/send";
    internal const string PushGetReceiptsPath = "/push/getReceipts";
}
