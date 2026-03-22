using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal static class WorkspaceScreen
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static readonly NumberFormatInfo money = Note();
    public static TelegramText Message(long chatId, WorkspaceViewRequestedCommand command) => new(chatId, Text(command), Keys(command));
    private static string Text(WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        WorkspaceData data = Data(command.Frame.StateData);
        return command.Frame.State switch
        {
            "account.name" => Name(data),
            "account.currency" => Currency(data),
            "account.balance" => Balance(data),
            "account.confirm" => Confirm(data),
            _ => Home(command, data)
        };
    }
    private static string Home(WorkspaceViewRequestedCommand command, WorkspaceData data)
    {
        var text = new StringBuilder();
        if (data.Accounts.Count == 0)
        {
            text.AppendLine("<b>Finance workspace</b>");
            if (!string.IsNullOrWhiteSpace(data.Notice))
            {
                text.AppendLine(Escape(data.Notice));
            }
            text.Append(command.Freshness.IsNewUser ? "Add your first account to start tracking your balance" : "Add an account to start tracking your balance");
            return text.ToString().TrimEnd();
        }
        text.AppendLine("<b>Your accounts</b>");
        foreach (AccountData item in data.Accounts)
        {
            text.AppendLine($"- <b>{Escape(item.Name)}</b>: {Amount(item.Amount, item.Currency)}");
        }
        if (!string.IsNullOrWhiteSpace(data.Notice))
        {
            text.AppendLine(Escape(data.Notice));
        }
        text.Append("Choose the next action");
        return text.ToString().TrimEnd();
    }
    private static string Name(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Error))
        {
            text.AppendLine(Escape(data.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.Append("Send the account name");
        return text.ToString().TrimEnd();
    }
    private static string Currency(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Error))
        {
            text.AppendLine(Escape(data.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Name)}</b>");
        text.Append(data.Custom ? "Send a 3 letter currency code" : "Choose the account currency");
        return text.ToString().TrimEnd();
    }
    private static string Balance(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Error))
        {
            text.AppendLine(Escape(data.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Name)}</b>");
        text.AppendLine($"Currency: {Code(data.Currency)}");
        text.Append("Send the current balance");
        return text.ToString().TrimEnd();
    }
    private static string Confirm(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Error))
        {
            text.AppendLine(Escape(data.Error));
        }
        text.AppendLine("<b>Confirm account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Name)}</b>");
        text.AppendLine($"Currency: {Code(data.Currency)}");
        text.Append($"Balance: <b>{Amount(data.Amount ?? 0m, data.Currency)}</b>");
        return text.ToString().TrimEnd();
    }
    private static IReadOnlyList<TelegramRow> Keys(WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TelegramButton[] item = [.. command.Frame.Actions.Select(Button)];
        return [.. item.Chunk(2).Select(item => new TelegramRow([.. item]))];
    }
    private static TelegramButton Button(string code) => code switch
    {
        "account.add" => new TelegramButton("➕ Add account", code, "primary"),
        "account.currency.rub" => new TelegramButton("RUB ₽", code),
        "account.currency.usd" => new TelegramButton("USD $", code),
        "account.currency.eur" => new TelegramButton("EUR €", code),
        "account.currency.other" => new TelegramButton("Other", code),
        "account.create" => new TelegramButton("✅ Create account", code, "success"),
        "account.cancel" => new TelegramButton("✖ Cancel", code, "danger"),
        _ => new TelegramButton(code, code)
    };
    private static WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return item ?? new WorkspaceData();
    }
    private static string Amount(decimal value, string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"{Money(value)} <code>{Escape(code)}</code>" : $"{Money(value)} {sign} (<code>{Escape(code)}</code>)";
    }
    private static string Code(string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"<code>{Escape(code)}</code>" : $"{sign} <code>{Escape(code)}</code>";
    }
    private static string Sign(string code) => code.ToUpperInvariant() switch
    {
        "RUB" => "₽",
        "USD" => "$",
        "EUR" => "€",
        _ => string.Empty
    };
    private static string Escape(string value) => WebUtility.HtmlEncode(value);
    private static string Money(decimal value) => value.ToString("#,0.##", money);
    private static NumberFormatInfo Note()
    {
        var item = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        item.NumberGroupSeparator = " ";
        item.NumberDecimalSeparator = ".";
        return item;
    }
}
