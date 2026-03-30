using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers Telegram Bot API transport behavior.
/// </summary>
public sealed class TelegramBotPortTests
{
    /// <summary>
    /// Verifies that the bot client posts a sendMessage request.
    /// </summary>
    [Fact(DisplayName = "Posts a sendMessage request to the Telegram Bot API")]
    public async Task Sends_message()
    {
        var lane = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = new { } }) });
        using var client = new HttpClient(lane) { BaseAddress = new Uri("https://api.telegram.org/") };
        var item = new TelegramBotPort(client, Options.Create(new TelegramBotOptions { Token = "token", BaseUrl = "https://api.telegram.org", TimeoutSeconds = 10 }));
        await item.Send(new TelegramText(100, "hello", [new TelegramRow([new TelegramButton("➕ Add account", "account.add", "primary")])]), default);
        Assert.Equal("/bottoken/sendMessage", lane.Path);
        using var body = JsonDocument.Parse(lane.Body);
        Assert.Equal("HTML", body.RootElement.GetProperty("parse_mode").GetString());
        Assert.Equal("primary", body.RootElement.GetProperty("reply_markup").GetProperty("inline_keyboard")[0][0].GetProperty("style").GetString());
    }
    /// <summary>
    /// Verifies that the bot client posts an answerCallbackQuery request.
    /// </summary>
    [Fact(DisplayName = "Posts an answerCallbackQuery request to the Telegram Bot API")]
    public async Task Answers_callback()
    {
        var lane = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = true }) });
        using var client = new HttpClient(lane) { BaseAddress = new Uri("https://api.telegram.org/") };
        var item = new TelegramBotPort(client, Options.Create(new TelegramBotOptions { Token = "token", BaseUrl = "https://api.telegram.org", TimeoutSeconds = 10 }));
        await item.Send(new TelegramCallbackAck("callback-1"), default);
        Assert.Equal("/bottoken/answerCallbackQuery", lane.Path);
        using var body = JsonDocument.Parse(lane.Body);
        Assert.Equal("callback-1", body.RootElement.GetProperty("callback_query_id").GetString());
    }
    /// <summary>
    /// Verifies that the bot client posts an editMessageText request.
    /// </summary>
    [Fact(DisplayName = "Posts an editMessageText request to the Telegram Bot API")]
    public async Task Edits_message()
    {
        var lane = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = new { } }) });
        using var client = new HttpClient(lane) { BaseAddress = new Uri("https://api.telegram.org/") };
        var item = new TelegramBotPort(client, Options.Create(new TelegramBotOptions { Token = "token", BaseUrl = "https://api.telegram.org", TimeoutSeconds = 10 }));
        await item.Send(new TelegramEditText(100, 8, "hello", [new TelegramRow([new TelegramButton("↩ Back", "transaction.recent.back")])]), default);
        Assert.Equal("/bottoken/editMessageText", lane.Path);
        using var body = JsonDocument.Parse(lane.Body);
        Assert.Equal(8, body.RootElement.GetProperty("message_id").GetInt64());
    }
    /// <summary>
    /// Verifies that throttling errors are marked retryable.
    /// </summary>
    [Fact(DisplayName = "Marks Telegram throttling responses as retryable")]
    public async Task Retries_throttle()
    {
        var lane = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = JsonContent.Create(new { ok = false, error_code = 429, description = "Too Many Requests" }) });
        using var client = new HttpClient(lane) { BaseAddress = new Uri("https://api.telegram.org/") };
        var item = new TelegramBotPort(client, Options.Create(new TelegramBotOptions { Token = "token", BaseUrl = "https://api.telegram.org", TimeoutSeconds = 10 }));
        DeliveryException error = await Assert.ThrowsAsync<DeliveryException>(() => item.Send(new TelegramText(100, "hello", []), default).AsTask());
        Assert.True(error.Retryable);
    }
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> next;
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> next) => this.next = next;
        public string Path { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return next(request);
        }
    }
}
