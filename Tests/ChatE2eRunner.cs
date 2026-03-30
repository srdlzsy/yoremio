using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

internal static class ChatE2eRunner
{
    public static async Task RunAsync()
    {
        var baseUrl = Environment.GetEnvironmentVariable("YOREMIO_BASE_URL") ?? "http://localhost:5089";
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

        var suffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var password = "Aa!12345";

        var sender = await RegisterAndLoginAsync(httpClient, $"chat.sender.{suffix}@example.com", password);
        var receiver = await RegisterAndLoginAsync(httpClient, $"chat.receiver.{suffix}@example.com", password);

        var message = $"chat-e2e-{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<(string FromUserId, string Message)>(TaskCreationOptions.RunContinuationsAsynchronously);

        var receiverConnection = BuildConnection(baseUrl, receiver.Token);
        var senderConnection = BuildConnection(baseUrl, sender.Token);

        receiverConnection.On<string, string>("ReceiveMessage", (fromUserId, incomingMessage) =>
        {
            if (fromUserId == sender.UserId && incomingMessage == message)
            {
                received.TrySetResult((fromUserId, incomingMessage));
            }
        });

        try
        {
            await receiverConnection.StartAsync();
            await senderConnection.StartAsync();

            await senderConnection.InvokeAsync("SendMessage", receiver.UserId, message);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await received.Task.WaitAsync(timeoutCts.Token);

            Console.WriteLine($"[CHAT-E2E][OK] from={result.FromUserId} message={result.Message}");
        }
        finally
        {
            await senderConnection.DisposeAsync();
            await receiverConnection.DisposeAsync();
        }
    }

    private static HubConnection BuildConnection(string baseUrl, string token)
    {
        return new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/chathub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();
    }

    private static async Task<(string Token, string UserId)> RegisterAndLoginAsync(HttpClient httpClient, string email, string password)
    {
        var registerResponse = await httpClient.PostAsJsonAsync("/api/auth/register/alici", new
        {
            email,
            password
        });

        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        if (!registerResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Register failed ({registerResponse.StatusCode}): {registerBody}");

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        if (!loginResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed ({loginResponse.StatusCode}): {loginBody}");

        using var document = JsonDocument.Parse(loginBody);
        var root = document.RootElement;

        var data = root.GetProperty("data");
        var token = data.GetProperty("token").GetString();
        var userId = data.GetProperty("userId").GetString();

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException($"Token or userId missing in login response: {loginBody}");

        return (token, userId);
    }
}
