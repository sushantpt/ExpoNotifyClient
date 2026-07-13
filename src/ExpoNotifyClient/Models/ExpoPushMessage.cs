using System.Text.Json.Serialization;

namespace ExpoNotifyClient.Models
{
    /// <summary>
    /// Represents a push notification message sent through the Expo Push API.
    /// </summary>
    public class ExpoPushMessage
    {
        /// <summary>
        /// Gets or sets the Expo push token identifying the recipient of this message.
        /// <para>
        /// The Expo Push API also supports an array of push tokens, but this model
        /// represents a single-recipient message. Use one <see cref="ExpoPushMessage"/>
        /// instance per recipient or change this property to a collection if
        /// multi-recipient messages are required.
        /// </para>
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("to")]
        public string? to { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification should wake the iOS app
        /// in the background to execute a background task.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("_contentAvailable")]
        public bool? _contentAvailable { get; set; }

        /// <summary>
        /// Gets or sets custom data delivered to the application.
        /// <para>
        /// The payload should not exceed approximately 4 KiB, including the entire
        /// notification payload sent to APNs or FCM.
        /// </para>
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("data")]
        public object? data { get; set; }

        /// <summary>
        /// Gets or sets the notification title.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("title")]
        public string? title { get; set; }

        /// <summary>
        /// Gets or sets the notification body text.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("body")]
        public string? body { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live (TTL), in seconds, for this notification.
        /// </summary>
        [JsonPropertyName("ttl")]
        public int? ttl { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp (seconds since the Unix epoch) at which
        /// this notification expires.
        /// Ignored if <see cref="ttl"/> is specified.
        /// </summary>
        [JsonPropertyName("expiration")]
        public long? expiration { get; set; }

        /// <summary>
        /// Gets or sets the delivery priority.
        /// Valid values are <c>default</c>, <c>normal</c>, and <c>high</c>.
        /// </summary>
        [JsonPropertyName("priority")]
        public string? priority { get; set; }

        /// <summary>
        /// Gets or sets the notification subtitle.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("subtitle")]
        public string? subtitle { get; set; }

        /// <summary>
        /// Gets or sets the notification sound.
        /// Specify <c>default</c> to use the system sound, or a configured custom
        /// sound filename including its extension.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("sound")]
        public string? sound { get; set; }

        /// <summary>
        /// Gets or sets the badge count displayed on the application icon.
        /// Specify <c>0</c> to clear the badge.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("badge")]
        public int? badge { get; set; }

        /// <summary>
        /// Gets or sets the notification interruption level.
        /// Valid values are <c>active</c>, <c>critical</c>, <c>passive</c>, and
        /// <c>time-sensitive</c>.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("interruptionLevel")]
        public string? interruptionLevel { get; set; }

        /// <summary>
        /// Gets or sets the Android notification channel identifier.
        /// The notification is not displayed if the specified channel does not exist.
        /// <para>Android only.</para>
        /// </summary>
        [JsonPropertyName("channelId")]
        public string? channelId { get; set; }

        /// <summary>
        /// Gets or sets the Android drawable resource name to use as the notification icon.
        /// <para>Android only.</para>
        /// </summary>
        [JsonPropertyName("icon")]
        public string? icon { get; set; }

        /// <summary>
        /// Gets or sets rich notification content, such as an image.
        /// Expected format: { image = "https://..." }.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("richContent")]
        public ExpoRichContent? richContent { get; set; }

        /// <summary>
        /// Gets or sets the notification category identifier.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("categoryId")]
        public string? categoryId { get; set; }

        /// <summary>
        /// Gets or sets the identifier used to collapse related notifications.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("collapseId")]
        public string? collapseId { get; set; }

        /// <summary>
        /// Gets or sets the Android notification tag used to replace an already
        /// displayed notification with the same tag.
        /// <para>Android only.</para>
        /// </summary>
        [JsonPropertyName("tag")]
        public string? tag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification can be modified
        /// by an iOS Notification Service Extension before delivery.
        /// <para>iOS only.</para>
        /// </summary>
        [JsonPropertyName("mutableContent")]
        public bool? mutableContent { get; set; }
    }
    
    /// <summary>
    /// Represents a push notification message with strongly-typed data.
    /// </summary>
    /// <typeparam name="TData">The type of the custom data payload.</typeparam>
    public sealed class ExpoPushMessage<TData> : ExpoPushMessage
    {
        /// <summary>
        /// Gets or sets custom data delivered to the application.
        /// <para>Supported platforms: Android and iOS.</para>
        /// </summary>
        [JsonPropertyName("data")]
        public TData? Data { get; set; }
    }
}