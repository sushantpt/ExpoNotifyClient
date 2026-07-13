using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Common.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpoTicketStatus
    {
        [JsonPropertyName("ok")]
        Ok,
        [JsonPropertyName("error")]
        Error
    }
}