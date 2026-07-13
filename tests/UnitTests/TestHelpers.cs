using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace UnitTests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;
    private readonly List<HttpRequestMessage> _requests = new();

    public void QueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    public void SetResponseFactory(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _responseFactory = factory;
    }

    public void QueueJsonResponse(object body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        QueueResponse(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer the request content for later inspection
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var bufferedRequest = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = new ByteArrayContent(contentBytes),
                Version = request.Version,
            };
            bufferedRequest.Headers.Clear();
            foreach (var header in request.Headers)
            {
                bufferedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (request.Content.Headers.ContentType != null)
            {
                bufferedRequest.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }
            _requests.Add(bufferedRequest);
        }
        else
        {
            _requests.Add(request);
        }

        if (_responseFactory != null)
        {
            return _responseFactory(request);
        }

        if (_responses.TryDequeue(out var response))
        {
            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
    }
}

internal class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}