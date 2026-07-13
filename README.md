# ExpoNotifyClient

.NET client library for sending push notifications via the Expo Push Service. Supports single and batch push notifications, delivery receipt retrieval, automatic batching, exponential backoff retry, and gzip compression.

Send push notifications from .NET through [Expo's Push Service](https://docs.expo.dev/push-notifications/overview/).

## Install

```shell
dotnet add package ExpoNotifyClient
```

## Quick start

```csharp
using ExpoNotifyClient;
using ExpoNotifyClient.Models;

var client = new ExpoClient();

var message = new ExpoPushMessage
{
    to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
    title = "Hello from .NET",
    body = "This is a push notification."
};

ExpoPushResponse response = await client.SendAsync(message);

if (response.IsSuccess)
    Console.WriteLine($"Sent! Ticket: {response.TicketIds.First()}");
else
    foreach (var err in response.AllErrorMessages)
        Console.WriteLine($"Error: {err}");
```

That's it. One message, one response.

## Sending in bulk (many users)

If you have more than one message, you can send them in a batch. Expo accepts up to 100 at a time in 1 single request. [Faq](https://docs.expo.dev/push-notifications/faq/#limit-of-sending-notifications)

```csharp
var messages = new List<ExpoPushMessage> { msg1, msg2, msg3 };
ExpoPushResponse response = await client.SendBatchAsync(messages);
```

If you have a lot — hundreds or thousands — use `SendBulkAsync`. It splits them into batches of 100 automatically.

```csharp
var results = await client.SendBulkAsync(hugeListOfMessages);
```

## Checking delivery

Send gives you tickets. Tickets are receipts-to-be. You can check if a notification actually arrived.

```csharp
ExpoReceiptResponse receipt = await client.GetReceiptAsync(ticketId);
```

Or check a bunch at once.

```csharp
ExpoReceiptResponse receipts = await client.GetReceiptsAsync(listOfTicketIds);
```

## Validation

You can check a message before sending it. Catches bad tokens, titles that are too long, body over 4000 chars, oversized data payloads, TTL that's too high.

```csharp
ValidationResult result = client.ValidateMessage(message);

if (!result.IsValid)
    Console.WriteLine(result.ErrorMessage);
```

`ValidateMessages` works on a list and tells you which index has the problem.

## Configuration

Three ways to create a client:

```csharp
// Defaults
var client = new ExpoClient();

// Custom options
var client = new ExpoClient(options);

// For DI / IHttpClientFactory
var client = new ExpoClient(httpClient, options);
```

Options:

```csharp
var options = new ExpoClientOptions
{
    AccessToken = "your-token",          // Required if push security is on
    TimeoutSeconds = 15,                  // Default 30
    MaxRetryAttempts = 5,                 // Default 3
    EnableCompression = true              // Default true
};
```

| Property | Default | What it does |
|---|---|---|
| `BaseUrl` | `https://exp.host/--/api/v2` | Expo API endpoint |
| `AccessToken` | `null` | Bearer token |
| `TimeoutSeconds` | `30` | HTTP timeout |
| `MaxRetryAttempts` | `3` | Retries on 429, 5xx, timeouts |
| `InitialBackoffMs` | `1000` | Starting retry delay |
| `MaxBackoffMs` | `30000` | Max retry delay |
| `EnableCompression` | `true` | Gzip/deflate |
| `MaxNotificationsPerBatch` | `100` | Max per request |

## Errors

Bad requests, bad tokens, unauthorized — those throw `ExpoApiException` right away.

```csharp
try
{
    var response = await client.SendAsync(message);
}
catch (ExpoApiException ex)
{
    Console.WriteLine($"{(int)ex.StatusCode}: {ex.Message}");
}
```

Transient failures — 429, 5xx, timeouts, network blips — get retried automatically with exponential backoff. The library handles those. You don't have to do anything.

Sometimes the HTTP request itself succeeds but individual messages in the batch fail. Check `response.FailedTickets` or `response.AllErrorMessages`.

```csharp
foreach (var ticket in response.FailedTickets)
    Console.WriteLine($"{ticket.Message} — {ticket.Details?.Error}");
```

Common error codes from Expo:

| Code | What it means |
|---|---|
| `DeviceNotRegistered` | Remove this token from your database |
| `MessageTooBig` | Payload is too large |
| `MessageRateExceeded` | You're sending too fast |
| `InvalidCredentials` | Access token is wrong |
| `TooManyRequests` | You're being rate-limited |

## Message fields

```csharp
var msg = new ExpoPushMessage
{
    to = "ExponentPushToken[...]",   // required
    title = "Hi",                     // max 200
    body = "This is the body",        // max 4000
    data = new { screen = "home" },   // custom JSON, max 4KB
    ttl = 3600,                       // seconds, max 28 days
    priority = "high",                // default, normal, high
    sound = "default",                // iOS
    badge = 1,                        // iOS
    subtitle = "sub",                 // iOS
    interruptionLevel = "critical",   // iOS
    mutableContent = true,            // iOS
    channelId = "updates",            // android
    icon = "notification_icon",       // android
    richContent = new ExpoRichContent { image = "https://..." },
    categoryId = "new_message",
    collapseId = "thread-1",
    tag = "order-123",                // android, replaces existing
};
```

If you want strongly-typed data:

```csharp
var msg = new ExpoPushMessage<MyData> { Data = new MyData { ... } };
```

## Responses

`ExpoPushResponse` has some helpers:

```csharp
response.IsSuccess       // all tickets were OK
response.HasErrors       // any errors at all
response.TicketIds       // IDs of successful tickets
response.FailedTickets   // tickets that failed
response.AllErrorMessages // everything that went wrong
```

`ExpoReceiptResponse`:

```csharp
receipt.IsSuccess         // all receipts OK
receipt.HasErrors         // any errors
receipt.FailedReceiptIds  // IDs with errors
receipt.Data[ticketId]    // look up a specific receipt
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Building

```shell
dotnet build src/ExpoNotifyClient
dotnet test tests/UnitTests
dotnet pack src/ExpoNotifyClient -c Release
```
