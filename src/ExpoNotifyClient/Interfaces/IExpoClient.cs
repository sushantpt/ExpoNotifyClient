using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpoNotifyClient.Common;
using ExpoNotifyClient.Models;

namespace ExpoNotifyClient.Interfaces
{
    /// <summary>
    /// Client for sending push notifications through the Expo Push Service.
    /// </summary>
    public interface IExpoClient
    {
        /// <summary>
        /// Sends a single push notification.
        /// </summary>
        /// <param name="message">The push notification message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The push response containing the ticket.</returns>
        Task<ExpoPushResponse> SendAsync(ExpoPushMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple push notifications in a single batch request.
        /// </summary>
        /// <param name="messages">The push notification messages (max 100).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The push response containing tickets for each message.</returns>
        Task<ExpoPushResponse> SendBatchAsync(IEnumerable<ExpoPushMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends multiple push notifications, automatically batching them into chunks of 100.
        /// </summary>
        /// <param name="messages">The push notification messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The combined push responses from all batches.</returns>
        Task<IEnumerable<ExpoPushResponse>> SendBulkAsync(IEnumerable<ExpoPushMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets push receipts for previously sent notifications.
        /// </summary>
        /// <param name="ticketIds">The ticket IDs to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The receipt response containing delivery status for each ticket.</returns>
        Task<ExpoReceiptResponse> GetReceiptsAsync(IEnumerable<string> ticketIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a push receipt for a single ticket ID.
        /// </summary>
        /// <param name="ticketId">The ticket ID to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The receipt response containing delivery status.</returns>
        Task<ExpoReceiptResponse> GetReceiptAsync(string ticketId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a push message before sending.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>A validation result containing any errors.</returns>
        ValidationResult ValidateMessage(ExpoPushMessage message);

        /// <summary>
        /// Validates multiple push messages before sending.
        /// </summary>
        /// <param name="messages">The messages to validate.</param>
        /// <returns>A validation result containing any errors.</returns>
        ValidationResult ValidateMessages(IEnumerable<ExpoPushMessage> messages);
    }
}