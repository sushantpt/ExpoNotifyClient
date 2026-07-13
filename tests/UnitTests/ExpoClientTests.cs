using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ExpoNotifyClient;
using ExpoNotifyClient.Common;
using ExpoNotifyClient.Common.Enums;
using ExpoNotifyClient.Models;

namespace UnitTests;

public class ExpoClientTests
{
    private static ExpoPushMessage ValidMessage => new()
    {
        to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
        title = "Hello",
        body = "World",
    };

    private static ExpoPushMessage[] MakeMessages(int count)
    {
        return Enumerable.Range(1, count).Select(i => new ExpoPushMessage
        {
            to = $"ExponentPushToken[{i:D24}]",
            title = $"Title {i}",
            body = $"Body {i}",
        }).ToArray();
    }

    private static ExpoClient CreateClient(MockHttpMessageHandler handler, ExpoClientOptions? options = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options?.BaseUrl ?? "https://exp.host/--/api/v2"),
        };

        return new ExpoClient(httpClient, options ?? new ExpoClientOptions(), new NullLogger<ExpoClient>());
    }

    [Test]
    public async Task SendAsync_SendsSingleMessage_ReturnsTicket()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse
        {
            Data = new List<ExpoPushTicket>
            {
                new() { Status = ExpoTicketStatus.Ok, Id = "ticket-1" },
            },
        });

        var client = CreateClient(handler);
        var result = await client.SendAsync(ValidMessage);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.TicketIds, Is.EquivalentTo(new[] { "ticket-1" }));
        Assert.That(handler.Requests, Has.Count.EqualTo(1));

        var request = handler.Requests[0];
        Assert.That(request.RequestUri?.AbsolutePath, Does.EndWith("/push/send"));
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
    }

    [Test]
    public async Task SendAsync_NullMessage_Throws()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync(null!));
    }

    [Test]
    public async Task SendBatchAsync_SendsMultipleMessages_ReturnsTickets()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse
        {
            Data = new List<ExpoPushTicket>
            {
                new() { Status = ExpoTicketStatus.Ok, Id = "ticket-1" },
                new() { Status = ExpoTicketStatus.Ok, Id = "ticket-2" },
            },
        });

        var client = CreateClient(handler);
        var messages = MakeMessages(2);
        var result = await client.SendBatchAsync(messages);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.TicketIds, Is.EquivalentTo(new[] { "ticket-1", "ticket-2" }));
    }

    [Test]
    public async Task SendBatchAsync_ExceedsLimit_Throws()
    {
        var handler = new MockHttpMessageHandler();
        var client = CreateClient(handler, new ExpoClientOptions { MaxNotificationsPerBatch = 100 });
        var messages = MakeMessages(101);

        Assert.ThrowsAsync<ArgumentException>(() => client.SendBatchAsync(messages));
    }

    [Test]
    public async Task SendBatchAsync_EmptyList_ReturnsEmptyResponse()
    {
        var handler = new MockHttpMessageHandler();
        var client = CreateClient(handler);
        var result = await client.SendBatchAsync(Array.Empty<ExpoPushMessage>());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Has.Count.Zero);
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SendBulkAsync_ChunksMessages_SendsMultipleBatches()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse
        {
            Data = new List<ExpoPushTicket> { new() { Status = ExpoTicketStatus.Ok, Id = "ticket-1" } },
        });
        handler.QueueJsonResponse(new ExpoPushResponse
        {
            Data = new List<ExpoPushTicket> { new() { Status = ExpoTicketStatus.Ok, Id = "ticket-2" } },
        });

        var client = CreateClient(handler, new ExpoClientOptions { MaxNotificationsPerBatch = 1 });
        var messages = MakeMessages(2);
        var results = (await client.SendBulkAsync(messages)).ToList();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SendBulkAsync_NullMessages_Throws()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        Assert.ThrowsAsync<ArgumentNullException>(() => client.SendBulkAsync(null!));
    }

    [Test]
    public async Task GetReceiptsAsync_ReturnsReceipts()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoReceiptResponse
        {
            Data = new Dictionary<string, ExpoReceipt>
            {
                ["ticket-1"] = new() { Status = ExpoTicketStatus.Ok },
                ["ticket-2"] = new() { Status = ExpoTicketStatus.Error, Message = "DeviceNotRegistered" },
            },
        });

        var client = CreateClient(handler);
        var result = await client.GetReceiptsAsync(new[] { "ticket-1", "ticket-2" });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailedReceiptIds, Is.EquivalentTo(new[] { "ticket-2" }));
    }

    [Test]
    public async Task GetReceiptAsync_WrapsSingleId()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoReceiptResponse
        {
            Data = new Dictionary<string, ExpoReceipt>
            {
                ["ticket-1"] = new() { Status = ExpoTicketStatus.Ok },
            },
        });

        var client = CreateClient(handler);
        var result = await client.GetReceiptAsync("ticket-1");

        Assert.That(result.IsSuccess, Is.True);

        var requestBody = Encoding.UTF8.GetString(handler.Requests[0].Content?.ReadAsByteArrayAsync().Result!);
        Assert.That(requestBody, Does.Contain("ticket-1"));
    }

    [Test]
    public async Task GetReceiptAsync_NullOrEmpty_Throws()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        Assert.ThrowsAsync<ArgumentNullException>(() => client.GetReceiptAsync(null!));
        Assert.ThrowsAsync<ArgumentException>(() => client.GetReceiptAsync(" "));
    }

    [Test]
    public void ValidateMessage_ValidMessage_ReturnsSuccess()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(ValidMessage);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateMessage_NullToken_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(new ExpoPushMessage { title = "test", body = "test" });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Does.Contain("token"));
    }

    [Test]
    public void ValidateMessage_InvalidTokenFormat_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(new ExpoPushMessage { to = "invalid-token" });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("format"));
    }

    [Test]
    public void ValidateMessage_TitleTooLong_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(new ExpoPushMessage
        {
            to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            title = new string('x', 201),
        });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Title"));
    }

    [Test]
    public void ValidateMessage_BodyTooLong_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(new ExpoPushMessage
        {
            to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            body = new string('x', 4001),
        });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Body"));
    }

    [Test]
    public void ValidateMessage_TtlExceedsMax_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(new ExpoPushMessage
        {
            to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            ttl = 99999999,
        });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("TTL"));
    }

    [Test]
    public void ValidateMessage_NullMessage_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessage(null!);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateMessages_NullCollection_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var result = client.ValidateMessages(null!);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateMessages_MultipleMessages_ReportsAllErrors()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var messages = new[]
        {
            ValidMessage,
            new ExpoPushMessage { title = "no token" },
            new ExpoPushMessage { to = "ExponentPushToken[yyyyyyyyyyyyyyyyyyyyyy]", title = new string('x', 201) },
        };

        var result = client.ValidateMessages(messages);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(2));
        Assert.That(result.Errors[0], Does.StartWith("[1]"));
        Assert.That(result.Errors[1], Does.StartWith("[2]"));
    }

    [Test]
    public async Task Retry_On429_RetriesAndSucceeds()
    {
        var handler = new MockHttpMessageHandler();
        var attempt = 0;

        handler.SetResponseFactory(request =>
        {
            attempt++;
            if (attempt <= 2)
            {
                var retryResponse = new HttpResponseMessage((HttpStatusCode)429);
                retryResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(10));
                retryResponse.Content = new StringContent("{\"errors\":[{\"error\":\"Too many requests\"}]}", Encoding.UTF8, "application/json");
                return retryResponse;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"data\":[{\"status\":\"ok\",\"id\":\"ticket-1\"}]}",
                    Encoding.UTF8, "application/json"),
            };
        });

        var client = CreateClient(handler, new ExpoClientOptions
        {
            MaxRetryAttempts = 3,
            InitialBackoffMs = 10,
            MaxBackoffMs = 100,
        });

        var result = await client.SendAsync(ValidMessage);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(attempt, Is.EqualTo(3));
    }

    [Test]
    public async Task Retry_OnTimeout_RetriesAndSucceeds()
    {
        var handler = new MockHttpMessageHandler();
        var attempt = 0;

        handler.SetResponseFactory(request =>
        {
            attempt++;
            if (attempt <= 2)
            {
                throw new TaskCanceledException("timeout");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"data\":[{\"status\":\"ok\",\"id\":\"ticket-1\"}]}",
                    Encoding.UTF8, "application/json"),
            };
        });

        var client = CreateClient(handler, new ExpoClientOptions
        {
            MaxRetryAttempts = 3,
            InitialBackoffMs = 10,
            MaxBackoffMs = 100,
        });

        var result = await client.SendAsync(ValidMessage);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(attempt, Is.EqualTo(3));
    }

    [Test]
    public async Task Retry_Exhausted_ThrowsExpoApiException()
    {
        var handler = new MockHttpMessageHandler();

        handler.SetResponseFactory(request =>
            new HttpResponseMessage((HttpStatusCode)503)
            {
                Content = new StringContent("{\"errors\":[{\"error\":\"Service Unavailable\"}]}", Encoding.UTF8, "application/json"),
            });

        var client = CreateClient(handler, new ExpoClientOptions
        {
            MaxRetryAttempts = 2,
            InitialBackoffMs = 10,
            MaxBackoffMs = 100,
        });

        var ex = Assert.ThrowsAsync<ExpoApiException>(() => client.SendAsync(ValidMessage));
        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task SendAsync_WithAccessToken_SetsAuthHeader()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse { Data = new List<ExpoPushTicket>() });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://exp.host/--/api/v2"),
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "my-secret-token");

        var options = new ExpoClientOptions { AccessToken = "my-secret-token" };
        var client = new ExpoClient(httpClient, options, new NullLogger<ExpoClient>());
        await client.SendAsync(ValidMessage);

        var authHeader = handler.Requests[0].Headers.Authorization;
        Assert.That(authHeader, Is.Not.Null);
        Assert.That(authHeader!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(authHeader.Parameter, Is.EqualTo("my-secret-token"));
    }

    [Test]
    public async Task SendAsync_ApiErrorResponse_ThrowsExpoApiException()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(
            new { errors = new[] { new { error = "InvalidCredentials" } } },
            HttpStatusCode.Unauthorized);

        var client = CreateClient(handler);
        var ex = Assert.ThrowsAsync<ExpoApiException>(() => client.SendAsync(ValidMessage));

        Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(ex.Message, Does.Contain("Unauthorized"));
    }

    [Test]
    public async Task SendBatchAsync_NullMessages_Throws()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        Assert.ThrowsAsync<ArgumentNullException>(() => client.SendBatchAsync(null!));
    }

    [Test]
    public void ValidateMessage_ValidGenericMessage_ReturnsSuccess()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var message = new ExpoPushMessage<Dictionary<string, string>>
        {
            to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            title = "Hello",
            Data = new Dictionary<string, string> { { "key", "value" } },
        };

        var result = client.ValidateMessage(message);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateMessage_DataPayloadTooLarge_ReturnsFailure()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        var hugeData = new string('x', 10000);

        var result = client.ValidateMessage(new ExpoPushMessage
        {
            to = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            data = hugeData,
        });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Data"));
    }

    [Test]
    public async Task SendBulkAsync_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        var client = CreateClient(handler);
        var results = await client.SendBulkAsync(Array.Empty<ExpoPushMessage>());

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SendBulkAsync_LargeBatch_ChunksCorrectly()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetResponseFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[{\"status\":\"ok\",\"id\":\"ticket\"}]}", Encoding.UTF8, "application/json"),
        });

        var client = CreateClient(handler, new ExpoClientOptions { MaxNotificationsPerBatch = 50 });
        var messages = MakeMessages(120);
        var results = (await client.SendBulkAsync(messages)).ToList();

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(handler.Requests, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetReceiptsAsync_EmptyIds_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        var client = CreateClient(handler);
        var result = await client.GetReceiptsAsync(Array.Empty<string>());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Has.Count.Zero);
    }

    [Test]
    public async Task GetReceiptsAsync_NullIds_Throws()
    {
        var client = CreateClient(new MockHttpMessageHandler());
        Assert.ThrowsAsync<ArgumentNullException>(() => client.GetReceiptsAsync(null!));
    }

    [Test]
    public void ExpoClient_Dispose_DoesNotThrow()
    {
        var client = new ExpoClient();
        Assert.DoesNotThrow(() => client.Dispose());
    }

    [Test]
    public void ExpoClient_Dispose_InjectedHttpClient_DoesNotDispose()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = new ExpoClientOptions
        {
            BaseUrl = "https://exp.host/--/api/v2",
        };

        var client = new ExpoClient(httpClient, options);
        client.Dispose();

        Assert.DoesNotThrow(() => httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost")));
    }

    [Test]
    public async Task SendAsync_ExpoResponseWithTicketError_YieldsFailedTickets()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse
        {
            Data = new List<ExpoPushTicket>
            {
                new() { Status = ExpoTicketStatus.Ok, Id = "ticket-1" },
                new()
                {
                    Status = ExpoTicketStatus.Error,
                    Message = "DeviceNotRegistered",
                    Details = new ExpoErrorDetails { Error = "DeviceNotRegistered" },
                },
            },
        });

        var client = CreateClient(handler);
        var result = await client.SendAsync(ValidMessage);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.HasErrors, Is.False);
        Assert.That(result.FailedTickets.ToList(), Has.Count.EqualTo(1));
        Assert.That(result.TicketIds, Is.EquivalentTo(new[] { "ticket-1" }));
        Assert.That(result.AllErrorMessages, Does.Contain("DeviceNotRegistered"));
    }

    [Test]
    public void EnableCompression_Enabled_SetsAcceptEncodingHeader()
    {
        var handler = new MockHttpMessageHandler();
        handler.QueueJsonResponse(new ExpoPushResponse { Data = new List<ExpoPushTicket>() });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://exp.host/--/api/v2"),
        };

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");

        var client = new ExpoClient(new ExpoClientOptions
        {
            EnableCompression = true,
            AccessToken = "test",
        });

        var hasGzip = client.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(client) is HttpClient hc
            && hc.DefaultRequestHeaders.TryGetValues("Accept-Encoding", out var values)
            && values.Any(v => v.Contains("gzip"));

        Assert.That(hasGzip, Is.True);
    }

    [Test]
    public void EnableCompression_Disabled_OmitsAcceptEncodingHeader()
    {
        using var client = new ExpoClient(new ExpoClientOptions
        {
            EnableCompression = false,
        });

        var hasEncoding = client.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(client) is HttpClient hc
            && hc.DefaultRequestHeaders.TryGetValues("Accept-Encoding", out var _);

        Assert.That(hasEncoding, Is.False);
    }
}