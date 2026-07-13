using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Models
{
    public class ExpoRichContent
    {
        [JsonPropertyName("image")]
        public string? image { get; set; }
    }
}