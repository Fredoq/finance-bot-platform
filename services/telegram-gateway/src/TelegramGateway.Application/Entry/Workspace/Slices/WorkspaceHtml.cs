using System.Globalization;
using System.Net;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceHtml
{
    private readonly NumberFormatInfo money;
    private readonly Dictionary<string, string> icon;

    internal WorkspaceHtml()
    {
        money = Note();
        icon = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["food"] = "🍽",
            ["transport"] = "🚌",
            ["home"] = "🏠",
            ["health"] = "❤️",
            ["shopping"] = "🛍",
            ["fun"] = "🎉",
            ["bills"] = "🧾",
            ["travel"] = "✈",
            ["salary"] = "💼",
            ["bonus"] = "🏅",
            ["gift"] = "🎁",
            ["cashback"] = "💳",
            ["sale"] = "🏷",
            ["interest"] = "📈",
            ["refund"] = "↩",
            ["other"] = "➕"
        };
    }

    internal string Amount(decimal? value, string code)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException("Workspace amount is required");
        }
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"{Money(value.Value)} <code>{Escape(code)}</code>" : $"{Money(value.Value)} {sign} (<code>{Escape(code)}</code>)";
    }

    internal static string Code(string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"<code>{Escape(code)}</code>" : $"{sign} <code>{Escape(code)}</code>";
    }

    internal string Label(decimal value, string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"{Money(value)} {code}" : $"{Money(value)} {sign}";
    }

    internal static string Flow(string kind) => string.Equals(kind, "income", StringComparison.Ordinal) ? "+" : "-";

    internal static string Title(string kind) => string.Equals(kind, "income", StringComparison.Ordinal) ? "Income" : "Expense";

    internal static string When(DateTimeOffset value) => value == default ? "unknown" : $"{value:yyyy-MM-dd HH:mm} UTC";

    internal string Category(string name, string code) => icon.TryGetValue(code, out string? value) ? $"{value} {name}" : name;

    internal static string Escape(string value) => WebUtility.HtmlEncode(value);

    private static string Sign(string code) => code.ToUpperInvariant() switch
    {
        "RUB" => "₽",
        "USD" => "$",
        "EUR" => "€",
        _ => string.Empty
    };

    private string Money(decimal value) => value.ToString("#,0.##", money);

    private static NumberFormatInfo Note()
    {
        var item = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        item.NumberGroupSeparator = " ";
        item.NumberDecimalSeparator = ".";
        return item;
    }
}
