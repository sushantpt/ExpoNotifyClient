using System.Text.Json.Serialization;
using ExpoNotifyClient.Common.Enums;

namespace ExpoNotifyClient.Models
{
    public class ExpoPushTicket
    {
        [JsonPropertyName("status")]
        public ExpoTicketStatus Status { get; set; }
    
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    
        [JsonPropertyName("details")]
        public ExpoErrorDetails? Details { get; set; }
    }
}