using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Telegram;

internal sealed class TelegramBotPort : ITelegramPort
{
    private readonly HttpClient client;
    private readonly TelegramBotOptions option;
    private readonly HttpStatusCode[] list =
    [
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];
    public TelegramBotPort(HttpClient client, IOptions<TelegramBotOptions> option)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
    }
    public async ValueTask Send(TelegramOperation message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        try
        {
            using HttpResponseMessage note = await client.PostAsJsonAsync(Path(message.Method), message.Payload(), token);
            TelegramResponse? data;
            try
            {
                data = await note.Content.ReadFromJsonAsync<TelegramResponse>(cancellationToken: token);
            }
            catch (Exception error) when (error is JsonException or NotSupportedException or InvalidOperationException)
            {
                throw new DeliveryException("Telegram response payload was invalid", Retryable(note.StatusCode), error);
            }
            if (note.IsSuccessStatusCode && data is { Ok: true })
            {
                return;
            }
            string text = !string.IsNullOrWhiteSpace(data?.Description) ? data.Description : note.ReasonPhrase ?? "Telegram delivery failed";
            bool retry = data?.ErrorCode is int code ? Retryable(note.StatusCode, code) : Retryable(note.StatusCode);
            throw new DeliveryException(text, retry);
        }
        catch (OperationCanceledException error) when (!token.IsCancellationRequested)
        {
            throw new DeliveryException("Telegram delivery timed out", true, error);
        }
        catch (HttpRequestException error)
        {
            throw new DeliveryException("Telegram transport failed", true, error);
        }
    }
    private Uri Path(string method) => new($"/bot{option.Token}/{method}", UriKind.Relative);
    private bool Retryable(HttpStatusCode code) => list.Contains(code) || (int)code >= 500;
    private bool Retryable(HttpStatusCode code, int errorCode) => Retryable(code) || errorCode is 429 or >= 500;
    private sealed record TelegramResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }
        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; init; }
        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;
    }
}
