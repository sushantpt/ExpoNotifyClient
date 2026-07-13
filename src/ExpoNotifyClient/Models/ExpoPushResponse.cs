using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpoNotifyClient.Common.Enums;

namespace ExpoNotifyClient.Models
{
    public class ExpoPushResponse
    {
        [JsonPropertyName("data")]
        [JsonConverter(typeof(ExpoPushTicketConverter))]
        public List<ExpoPushTicket>? Data { get; set; } = new List<ExpoPushTicket>();

        [JsonPropertyName("errors")]
        public List<ExpoErrorDetails>? Errors { get; set; } = new List<ExpoErrorDetails>();

        /// <summary>
        /// Gets a value indicating whether the response has any errors.
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => Errors?.Count > 0;

        /// <summary>
        /// Gets a value indicating whether all tickets were successful.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => !HasErrors && Data?.All(t => t.Status == ExpoTicketStatus.Ok) == true;

        /// <summary>
        /// Gets the IDs of all successful tickets.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> TicketIds => Data?
            .Where(t => t.Status == ExpoTicketStatus.Ok && t.Id != null)
            .Select(t => t.Id!) ?? Enumerable.Empty<string>();

        /// <summary>
        /// Gets the tickets that failed.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<ExpoPushTicket> FailedTickets => Data?
            .Where(t => t.Status == ExpoTicketStatus.Error) ?? Enumerable.Empty<ExpoPushTicket>();

        /// <summary>
        /// Gets all error messages from failed tickets and response errors.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> AllErrorMessages
        {
            get
            {
                if (Errors != null)
                {
                    foreach (var error in Errors)
                    {
                        if (!string.IsNullOrEmpty(error.Error))
                            yield return error.Error;
                    }
                }

                if (Data != null)
                {
                    foreach (var ticket in Data.Where(t => t.Status == ExpoTicketStatus.Error))
                    {
                        if (!string.IsNullOrEmpty(ticket.Message))
                            yield return ticket.Message;
                        if (ticket.Details?.Error != null)
                            yield return ticket.Details.Error;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Custom converter to handle both single object and array for the data field.
    /// </summary>
    public class ExpoPushTicketConverter : JsonConverter<List<ExpoPushTicket>>
    {
        public override List<ExpoPushTicket> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // If it's an array, deserialize as list
                return JsonSerializer.Deserialize<List<ExpoPushTicket>>(ref reader, options) ?? new List<ExpoPushTicket>();
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // If it's a single object, deserialize as single ticket and wrap in list
                var ticket = JsonSerializer.Deserialize<ExpoPushTicket>(ref reader, options);
                return ticket != null ? new List<ExpoPushTicket> { ticket } : new List<ExpoPushTicket>();
            }
            
            return new List<ExpoPushTicket>();
        }

        public override void Write(Utf8JsonWriter writer, List<ExpoPushTicket> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}