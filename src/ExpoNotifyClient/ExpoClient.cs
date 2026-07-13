using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ExpoNotifyClient.Common;
using ExpoNotifyClient.Interfaces;
using ExpoNotifyClient.Models;
using Microsoft.Extensions.Logging;

namespace ExpoNotifyClient;

public class ExpoClient : IExpoClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ExpoClientOptions _options;
    private readonly ILogger<ExpoClient>? _logger;
    private readonly bool _ownsHttpClient;
    private readonly Random _rng;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ExpoClient() : this(new ExpoClientOptions(), null) { }

    public ExpoClient(ExpoClientOptions options) : this(options, null) { }

    public ExpoClient(ExpoClientOptions options, ILogger<ExpoClient>? logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _ownsHttpClient = true;
        _rng = new Random();

        var handler = new HttpClientHandler();

        if (_options.EnableCompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (_options.EnableCompression)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        }

        if (!string.IsNullOrEmpty(options.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }
    }

    public ExpoClient(HttpClient httpClient, ExpoClientOptions options, ILogger<ExpoClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _ownsHttpClient = false;
        _rng = new Random();
    }

    public async Task<ExpoPushResponse> SendAsync(ExpoPushMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return await SendPushAsync(new[] { message }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExpoPushResponse> SendBatchAsync(IEnumerable<ExpoPushMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        var messageList = messages.ToList();

        if (messageList.Count == 0)
        {
            return new ExpoPushResponse { Data = new List<ExpoPushTicket>() };
        }

        if (messageList.Count > _options.MaxNotificationsPerBatch)
        {
            throw new ArgumentException(
                $"Batch size ({messageList.Count}) exceeds maximum of {_options.MaxNotificationsPerBatch}. " +
                $"Use SendBulkAsync for large collections.",
                nameof(messages));
        }

        return await SendPushAsync(messageList, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ExpoPushResponse>> SendBulkAsync(IEnumerable<ExpoPushMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        var results = new List<ExpoPushResponse>();

        foreach (var chunk in Chunk(messages, _options.MaxNotificationsPerBatch))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await SendPushAsync(chunk, cancellationToken).ConfigureAwait(false);
            results.Add(response);
        }

        return results;
    }

    public async Task<ExpoReceiptResponse> GetReceiptsAsync(IEnumerable<string> ticketIds, CancellationToken cancellationToken = default)
    {
        if (ticketIds == null)
        {
            throw new ArgumentNullException(nameof(ticketIds));
        }

        var ids = ticketIds.ToList();

        if (ids.Count == 0)
        {
            return new ExpoReceiptResponse { Data = new Dictionary<string, ExpoReceipt>() };
        }

        if (ids.Count > ExpoPushConstants.MaxTicketIdsPerRequest)
        {
            _logger?.LogWarning(
                "Requesting {Count} receipts exceeds recommended max of {Max}. " +
                "The API may reject the request.",
                ids.Count, ExpoPushConstants.MaxTicketIdsPerRequest);
        }

        var payload = new ReceiptRequest { Ids = ids };
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var responseBody = await SendApiRequestAsync(ExpoPushConstants.PushGetReceiptsPath, json, cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ExpoReceiptResponse>(responseBody, JsonOptions);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Expo receipt response.");
        }

        return result;
    }

    public async Task<ExpoReceiptResponse> GetReceiptAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        if (ticketId == null)
        {
            throw new ArgumentNullException(nameof(ticketId));
        }

        if (string.IsNullOrWhiteSpace(ticketId))
        {
            throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));
        }

        return await GetReceiptsAsync(new[] { ticketId }, cancellationToken).ConfigureAwait(false);
    }

    public ValidationResult ValidateMessage(ExpoPushMessage message)
    {
        if (message == null)
        {
            return ValidationResult.Failure("Message cannot be null.");
        }

        var errors = new List<string>();

        ValidateToken(message.to, errors);
        ValidateTitle(message.title, errors);
        ValidateBody(message.body, errors);
        ValidateTtl(message.ttl, errors);

        if (message.data != null)
        {
            ValidateDataPayload(message.data, errors);
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    public ValidationResult ValidateMessages(IEnumerable<ExpoPushMessage> messages)
    {
        if (messages == null)
        {
            return ValidationResult.Failure("Messages collection cannot be null.");
        }

        var errors = new List<string>();
        var index = 0;

        foreach (var message in messages)
        {
            var msgResult = ValidateMessage(message);
            if (!msgResult.IsValid)
            {
                errors.AddRange(msgResult.Errors.Select(e => $"[{index}] {e}"));
            }

            index++;
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    private async Task<ExpoPushResponse> SendPushAsync(IReadOnlyList<ExpoPushMessage> messages, CancellationToken cancellationToken)
    {
        string json = messages.Count == 1 ? JsonSerializer.Serialize(messages[0], JsonOptions) : JsonSerializer.Serialize(messages, JsonOptions);
        var responseBody = await SendApiRequestAsync(ExpoPushConstants.PushSendPath, json, cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ExpoPushResponse>(responseBody, JsonOptions);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Expo push response.");
        }
        LogFailedTickets(result);
        return result;
    }

    private async Task<string> SendApiRequestAsync(string path, string jsonContent, CancellationToken cancellationToken)
    {
        /*
         *
         * https://docs.expo.dev/push-notifications/sending-notifications/
            Notifications per request	100 messages maximum
            Push receipts per request	1,000 receipt IDs maximum
            Rate limit	                600 notifications/second/project
            Payload size	            4 KB maximum (title + body + data combined)
         */
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var apiPath = path.TrimStart('/');
        var fullUrl = $"{baseUrl}/{apiPath}";

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var content = new ByteArrayContent(jsonBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(fullUrl, content, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (attempt < _options.MaxRetryAttempts)
            {
                _logger?.LogWarning("HTTP request timed out (attempt {Attempt}/{MaxRetries})",
                    attempt + 1, _options.MaxRetryAttempts);
                await BackoffDelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetryAttempts)
            {
                _logger?.LogWarning(ex, "HTTP request failed (attempt {Attempt}/{MaxRetries})",
                    attempt + 1, _options.MaxRetryAttempts);
                await BackoffDelayAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return body;
            }

            var shouldRetry = IsTransientStatusCode(response.StatusCode)
                              && attempt < _options.MaxRetryAttempts;

            if (!shouldRetry)
            {
                throw new ExpoApiException(response.StatusCode, body, body);
            }

            _logger?.LogWarning(
                "Expo API returned {(int)StatusCode} ({StatusCode}), retrying (attempt {Attempt}/{MaxRetries})",
                (int)response.StatusCode, response.StatusCode,
                attempt + 1, _options.MaxRetryAttempts);

            await BackoffDelayAsync(attempt, response, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task BackoffDelayAsync(int attempt, HttpResponseMessage? response, CancellationToken cancellationToken)
    {
        var delay = CalculateBackoff(attempt, response);
        _logger?.LogDebug("Waiting {DelayMs}ms before retry", delay.TotalMilliseconds);
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan CalculateBackoff(int attempt, HttpResponseMessage? response)
    {
        if (response?.Headers.RetryAfter?.Delta is { } retryAfter)
        {
            return retryAfter;
        }

        if (response?.Headers.RetryAfter?.Date is { } retryDate)
        {
            var delta = retryDate - DateTimeOffset.UtcNow;
            if (delta > TimeSpan.Zero)
            {
                return delta;
            }
        }

        var backoff = _options.InitialBackoffMs * Math.Pow(2, attempt);
        backoff = Math.Min(backoff, _options.MaxBackoffMs);

        var jitter = _rng.NextDouble() * 0.3 + 0.85;
        backoff *= jitter;

        return TimeSpan.FromMilliseconds(backoff);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code == 429 || code >= 500;
    }

    private static void ValidateToken(string? token, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            errors.Add("Push token is required.");
            return;
        }

        if (!token.StartsWith("ExponentPushToken[", StringComparison.Ordinal) ||
            !token.EndsWith("]", StringComparison.Ordinal))
        {
            errors.Add($"Push token '{token}' has an invalid format. Expected format: ExponentPushToken[<token>]");
        }
    }

    private static void ValidateTitle(string? title, List<string> errors)
    {
        if (title != null && title.Length > ExpoPushConstants.MaxTitleLength)
        {
            errors.Add(
                $"Title exceeds maximum length of {ExpoPushConstants.MaxTitleLength} characters " +
                $"(current: {title.Length}).");
        }
    }

    private static void ValidateBody(string? body, List<string> errors)
    {
        if (body != null && body.Length > ExpoPushConstants.MaxBodyLength)
        {
            errors.Add(
                $"Body exceeds maximum length of {ExpoPushConstants.MaxBodyLength} characters " +
                $"(current: {body.Length}).");
        }
    }

    private static void ValidateTtl(int? ttl, List<string> errors)
    {
        if (ttl > ExpoPushConstants.MaxTtlSeconds)
        {
            errors.Add(
                $"TTL ({ttl}s) exceeds maximum of {ExpoPushConstants.MaxTtlSeconds}s " +
                $"({ExpoPushConstants.MaxTtlSeconds / 86400} days).");
        }
    }

    private static void ValidateDataPayload(object data, List<string> errors)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetByteCount(serialized);

            if (bytes > ExpoPushConstants.MaxDataPayloadBytes)
            {
                errors.Add(
                    $"Data payload ({bytes} bytes) exceeds maximum of " +
                    $"{ExpoPushConstants.MaxDataPayloadBytes} bytes.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Data payload is not valid JSON: {ex.Message}");
        }
    }

    private void LogFailedTickets(ExpoPushResponse response)
    {
        if (_logger == null)
        {
            return;
        }

        foreach (var ticket in response.FailedTickets)
        {
            _logger.LogWarning(
                "Push notification failed for ticket. Status: {Status}, Message: {Message}, Details: {Details}",
                ticket.Status, ticket.Message, ticket.Details?.Error);
        }

        if (response.Errors?.Count > 0)
        {
            foreach (var error in response.Errors)
            {
                _logger.LogError("Push API returned error: {Error}", error.Error);
            }
        }
    }

    private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var chunk = new List<T>(size);
        foreach (var item in source)
        {
            chunk.Add(item);
            if (chunk.Count >= size)
            {
                yield return chunk;
                chunk = new List<T>(size);
            }
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }

    private sealed class ReceiptRequest
    {
        public List<string> Ids { get; set; } = new();
    }
}