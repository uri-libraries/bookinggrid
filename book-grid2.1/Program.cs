using BookGrid.Services;
using BookGrid.Hubs;
using Serilog;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bookgrid-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add HTTP clients with Polly retry policies
builder.Services.AddHttpClient<LibCalService>(client =>
{
    client.BaseAddress = new Uri("https://uri.libcal.com/api/1.1/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Add HTTP client for TokenManager
builder.Services.AddHttpClient<TokenManager>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

// Register services
builder.Services.AddScoped<RoomService>();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

var app = builder.Build();

// Link LibCal token at startup
using (var scope = app.Services.CreateScope())
{
    var tokenManager = scope.ServiceProvider.GetRequiredService<TokenManager>();
    var accessToken = "ec048acd6c943b97204b839efcc8e57759ec5178";
    var expiresAt = DateTime.UtcNow.AddSeconds(3600); // 1 hour from now
    var linked = tokenManager.LinkLibCalTokenAsync(accessToken, expiresAt).GetAwaiter().GetResult();
    if (linked)
        Log.Information("LibCal token linked successfully at startup.");
    else
        Log.Error("Failed to link LibCal token at startup.");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<BookingHub>("/bookingHub");
app.MapFallbackToPage("/_Host");

app.Run();

// Polly policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} after {Delay}ms for {Uri}", 
                    retryCount, timespan.TotalMilliseconds, context.OperationKey);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, duration) =>
            {
                Log.Error("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker closed");
            });
}