using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Models
{
    public class ExpoErrorDetails
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public object? AdditionalDetails { get; set; }
    }
}