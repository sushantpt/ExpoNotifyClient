using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Common.Enums
{
    public enum ExpoInterruptionLevel
    {
        [JsonPropertyName("active")]
        Active,
        [JsonPropertyName("critical")]
        Critical,
        [JsonPropertyName("passive")]
        Passive,
        [JsonPropertyName("time-sensitive")]
        TimeSensitive
    }
}