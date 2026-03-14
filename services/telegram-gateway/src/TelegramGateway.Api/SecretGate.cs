using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace TelegramGateway.Api;

internal sealed class SecretGate(IOptions<TelegramWebhookOptions> option) : IMiddleware
{
    private const string Header = "X-Telegram-Bot-Api-Secret-Token";
    private readonly byte[] secret = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(option.Value.SecretToken) ? throw new InvalidOperationException("Telegram webhook secret token is required") : option.Value.SecretToken);
    /// <summary>
    /// Validates the webhook secret header before request processing continues.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="next">The next middleware delegate.</param>
    /// <returns>A task that completes when the middleware finishes.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string head = context.Request.Headers[Header].ToString();
        if (string.IsNullOrWhiteSpace(head) || secret.Length == 0)
        {
            await TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Webhook secret is invalid").ExecuteAsync(context);
            return;
        }
        byte[] left = Encoding.UTF8.GetBytes(head);
        if (left.Length != secret.Length || !CryptographicOperations.FixedTimeEquals(left, secret))
        {
            await TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Webhook secret is invalid").ExecuteAsync(context);
            return;
        }
        await next(context);
    }
}
