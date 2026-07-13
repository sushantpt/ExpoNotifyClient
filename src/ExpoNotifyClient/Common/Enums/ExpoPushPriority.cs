using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Common.Enums
{ 
    public enum ExpoPushPriority
    {
        [JsonPropertyName("default")]
        Default,
        [JsonPropertyName("normal")]
        Normal,
        [JsonPropertyName("high")]
        High
    }
}