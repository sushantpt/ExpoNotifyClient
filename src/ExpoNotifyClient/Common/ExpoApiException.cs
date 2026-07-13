using System;
using System.Net;

namespace ExpoNotifyClient.Common;

public class ExpoApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }

    public ExpoApiException(HttpStatusCode statusCode, string message, string? responseBody = null)
        : base($"Expo API responded with {(int)statusCode} ({statusCode}): {message}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public ExpoApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
