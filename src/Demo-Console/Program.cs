using ExpoNotifyClient;
using ExpoNotifyClient.Common;
using ExpoNotifyClient.Models;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== Expo Push Notification Sender ===");
Console.ResetColor();
Console.WriteLine();

Console.Write("Expo Push Tokens (comma separated): ");
var tokenInput = Console.ReadLine();

Console.Write("Title: ");
var title = Console.ReadLine();

Console.Write("Message: ");
var body = Console.ReadLine();

var tokens = tokenInput?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Distinct(StringComparer.Ordinal)
    .ToList();

if (tokens == null || tokens.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("At least one Expo push token is required.");
    Console.ResetColor();
    return;
}

var client = new ExpoClient();

var messages = tokens.Select(token => new ExpoPushMessage
{
    to = token,
    title = title,
    body = body,
    sound = "default",
    priority = "normal"
}).ToList();

Console.WriteLine();
Console.Write($"Sending to {messages.Count} device(s)... ");

try
{
    var responses = await client.SendBulkAsync(messages);

    Console.WriteLine("Done!");
    Console.WriteLine();

    int successCount = 0;
    int failedCount = 0;

    foreach (var response in responses)
    {
        successCount += response.TicketIds.Count();
        failedCount += response.FailedTickets.Count();

        foreach (var ticketId in response.TicketIds)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Ticket: {ticketId}");
            Console.ResetColor();
        }

        foreach (var error in response.AllErrorMessages)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {error}");
            Console.ResetColor();
        }
    }

    Console.WriteLine();
    Console.ForegroundColor = failedCount == 0
        ? ConsoleColor.Green
        : ConsoleColor.Yellow;

    Console.WriteLine($"Summary");
    Console.WriteLine($"--------");
    Console.WriteLine($"Devices : {tokens.Count}");
    Console.WriteLine($"Accepted: {successCount}");
    Console.WriteLine($"Failed  : {failedCount}");

    Console.ResetColor();
}
catch (ExpoApiException ex)
{
    Console.WriteLine("Failed!");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"HTTP {(int)ex.StatusCode}: {ex.Message}");

    if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
    {
        Console.WriteLine(ex.ResponseBody);
    }

    Console.ResetColor();
}
catch (Exception ex)
{
    Console.WriteLine("Failed!");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex);

    Console.ResetColor();
}