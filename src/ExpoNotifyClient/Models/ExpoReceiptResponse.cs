using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ExpoNotifyClient.Common.Enums;

namespace ExpoNotifyClient.Models
{
    public class ExpoReceiptResponse
    {
        [JsonPropertyName("data")]
        public Dictionary<string, ExpoReceipt>? Data { get; set; }
        
        [JsonPropertyName("errors")]
        public List<ExpoErrorDetails>? Errors { get; set; }

        [JsonIgnore]
        public bool HasErrors => Errors?.Count > 0;

        [JsonIgnore]
        public bool IsSuccess => !HasErrors && Data?.All(r => r.Value.Status == ExpoTicketStatus.Ok) == true;

        /// <summary>
        /// Gets all receipt IDs that have failed.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> FailedReceiptIds => Data?
            .Where(kvp => kvp.Value.Status == ExpoTicketStatus.Error)
            .Select(kvp => kvp.Key) ?? Enumerable.Empty<string>();
    }

    public class ExpoReceipt
    {
        [JsonPropertyName("status")]
        public ExpoTicketStatus Status { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("details")]
        public ExpoErrorDetails? Details { get; set; }
    }
}