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
        var head = context.HttpContext.Request.Headers[Header].ToString();
        var left = Encoding.UTF8.GetBytes(head);
        var right = Encoding.UTF8.GetBytes(option.Value.SecretToken);
        if (left.Length != right.Length || !CryptographicOperations.FixedTimeEquals(left, right))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Webhook secret is invalid");
        }
        return await next(context);
    }
}
