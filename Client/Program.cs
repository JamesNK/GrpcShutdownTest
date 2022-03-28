// See https://aka.ms/new-console-template for more information
using System.Net;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger(typeof(Program));

HttpClient client = new HttpClient();

var readResponseTask = StartLongRunningCall(logger, client);

logger.LogInformation("Client waiting a bit");
await Task.Delay(TimeSpan.FromSeconds(3));

await StartShutdownCall(logger, client);

await readResponseTask;

await Task.Delay(TimeSpan.FromSeconds(1));

static async Task StartShutdownCall(ILogger logger, HttpClient client)
{
    logger.LogInformation("Client starting shutdown call");

    using var shutdownResponse = await client.SendAsync(
        new HttpRequestMessage(HttpMethod.Post, "https://localhost:9000/shutdown")
        {
            Version = HttpVersion.Version20
        },
        HttpCompletionOption.ResponseHeadersRead);
    shutdownResponse.EnsureSuccessStatusCode();
}

static async Task StartLongRunningCall(ILogger logger, HttpClient client)
{
    logger.LogInformation("Client starting long running call");

    using var longrunningResponse = await client.SendAsync(
        new HttpRequestMessage(HttpMethod.Post, "https://localhost:9000/longrunning")
        {
            Version = HttpVersion.Version20,
            Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
        },
        HttpCompletionOption.ResponseHeadersRead);
    longrunningResponse.EnsureSuccessStatusCode();

    logger.LogInformation($"Starting to read response content");
    using var longrunningStream = await longrunningResponse.Content.ReadAsStreamAsync();
    var buffer = new byte[1024 * 128];
    var readCount = 0;
    var totalCount = 0;
    while ((readCount = await longrunningStream.ReadAsync(buffer)) != 0)
    {
        totalCount += readCount;
        logger.LogInformation($"Received {readCount} bytes. Total {totalCount} bytes.");
    }
    logger.LogInformation($"Finished reading response content");
}

