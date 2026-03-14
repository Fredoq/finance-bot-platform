using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace TelegramGateway.Api;

/// <summary>
/// Validates the Telegram secret header before the webhook handler runs.
/// Example:
/// <code>
/// group.AddEndpointFilter&lt;SecretGate&gt;();
/// </code>
/// </summary>
internal sealed class SecretGate(IOptions<TelegramWebhookOptions> option) : IEndpointFilter
{
    private const string Header = "X-Telegram-Bot-Api-Secret-Token";
    private readonly byte[] secret = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(option.Value.SecretToken) ? throw new InvalidOperationException("Telegram webhook secret token is required") : option.Value.SecretToken);
    /// <summary>
    /// Validates the current request secret header.
    /// Example:
    /// <code>
    /// object? item = await gate.InvokeAsync(context, next);
    /// </code>
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <param name="next">The next filter in the chain.</param>
    /// <returns>The endpoint result.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        string head = context.HttpContext.Request.Headers[Header].ToString();
        if (string.IsNullOrWhiteSpace(head) || secret.Length == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Webhook secret is invalid");
        }
        byte[] left = Encoding.UTF8.GetBytes(head);
        if (left.Length != secret.Length || !CryptographicOperations.FixedTimeEquals(left, secret))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Webhook secret is invalid");
        }
        return await next(context);
    }
}
