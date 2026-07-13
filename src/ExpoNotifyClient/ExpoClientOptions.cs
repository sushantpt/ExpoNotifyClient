using System.Text.Json;

namespace ExpoNotifyClient
{
    /// <summary>
    /// Configuration options for the Expo push notification client.
    /// </summary>
    public class ExpoClientOptions
    {
        /// <summary>
        /// Gets or sets the base URL for the Expo Push API.
        /// Default is "https://exp.host/--/api/v2".
        /// </summary>
        public string BaseUrl { get; set; } = "https://exp.host/--/api/v2";

        /// <summary>
        /// Gets or sets the access token for authenticating with the Expo Push API.
        /// Required if push security is enabled on your project.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed requests.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay in milliseconds for exponential backoff.
        /// Default is 1000ms (1 second).
        /// </summary>
        public int InitialBackoffMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum delay in milliseconds for exponential backoff.
        /// Default is 30000ms (30 seconds).
        /// </summary>
        public int MaxBackoffMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to enable gzip compression for requests.
        /// Default is true.
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of notifications per batch request.
        /// Default is 100 (Expo's limit).
        /// </summary>
        public int MaxNotificationsPerBatch { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets custom JSON serializer options.
        /// </summary>
        public JsonSerializerOptions? JsonOptions { get; set; }
    }
}