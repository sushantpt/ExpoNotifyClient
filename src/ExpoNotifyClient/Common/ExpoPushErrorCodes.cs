namespace ExpoNotifyClient.Common;

public static class ExpoPushErrorCodes
{
    public const string DeviceNotRegistered = "DeviceNotRegistered";
    public const string MessageTooBig = "MessageTooBig";
    public const string MessageRateExceeded = "MessageRateExceeded";
    public const string MismatchSenderId = "MismatchSenderId";
    public const string InvalidCredentials = "InvalidCredentials";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string PushTooManyExperienceIds = "PUSH_TOO_MANY_EXPERIENCE_IDS";
    public const string PushTooManyNotifications = "PUSH_TOO_MANY_NOTIFICATIONS";
    public const string PushTooManyReceipts = "PUSH_TOO_MANY_RECEIPTS";
    public const string Unauthorized = "UNAUTHORIZED";
}
