using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Telegram;

internal sealed class TelegramBotPort : ITelegramPort
{
    private readonly HttpClient client;
    private readonly TelegramBotOptions option;
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
            TelegramResponse? data = await note.Content.ReadFromJsonAsync<TelegramResponse>(cancellationToken: token);
            if (note.IsSuccessStatusCode && data is { Ok: true })
            {
                return;
            }
            string text = !string.IsNullOrWhiteSpace(data?.Description) ? data.Description : note.ReasonPhrase ?? "Telegram delivery failed";
            throw new DeliveryException(text, Retryable(note.StatusCode, data?.ErrorCode));
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
    private static bool Retryable(HttpStatusCode code, int? errorCode) => code switch
    {
        HttpStatusCode.RequestTimeout => true,
        HttpStatusCode.TooManyRequests => true,
        HttpStatusCode.BadGateway => true,
        HttpStatusCode.ServiceUnavailable => true,
        HttpStatusCode.GatewayTimeout => true,
        _ when (int)code >= 500 => true,
        _ when errorCode is 429 or >= 500 => true,
        _ => false
    };
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
