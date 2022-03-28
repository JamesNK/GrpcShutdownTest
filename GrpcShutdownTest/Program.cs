var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapPost("/longrunning", async context =>
{
    app.Logger.LogInformation("Server long running started");

    app.Logger.LogInformation("Server reading request");
    var result = await context.Request.BodyReader.ReadAtLeastAsync(4);
    context.Request.BodyReader.AdvanceTo(result.Buffer.End);

    app.Logger.LogInformation("Server waiting a bit");
    await Task.Delay(TimeSpan.FromSeconds(5));

    var randomBytes = Enumerable.Range(1, 2_000_000).Select(i => (byte)i).ToArray();


    var memory = context.Response.BodyWriter.GetMemory(randomBytes.Length);

    app.Logger.LogInformation($"Server writing {randomBytes.Length} bytes response");
    randomBytes.CopyTo(memory);

    app.Logger.LogInformation($"Server advancing {randomBytes.Length} bytes response");
    context.Response.BodyWriter.Advance(randomBytes.Length);
});
app.MapPost("/shutdown", context =>
{
    app.Logger.LogInformation("Server stopping application");

    var hostApplicationLifetime = context.RequestServices.GetRequiredService<IHostApplicationLifetime>();
    hostApplicationLifetime.StopApplication();
    return Task.CompletedTask;
});

app.Run();
