#pragma warning disable S2325
using System.Globalization;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceAmount
{
    private const NumberStyles Styles = NumberStyles.Number;

    internal bool Try(string value, out decimal amount)
    {
        string text = value.Trim();
        if (Decimal(text, ',', out amount) && Candidate(text, ','))
        {
            return true;
        }
        if (Decimal(text, '.', out amount) && Candidate(text, '.'))
        {
            return true;
        }
        bool current = decimal.TryParse(text, Styles, CultureInfo.CurrentCulture, out decimal local);
        bool invariant = decimal.TryParse(text, Styles, CultureInfo.InvariantCulture, out decimal global);
        if (string.IsNullOrWhiteSpace(text) || text.Contains('.') && text.Contains(',') || Delimiter(text) && current && invariant && local != global)
        {
            amount = 0m;
            return false;
        }
        if (current)
        {
            amount = local;
            return true;
        }
        amount = global;
        return invariant;
    }

    internal int Scale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;

    private bool Candidate(string value, char sign)
    {
        int slot = value.LastIndexOf(sign);
        return slot > 0 && slot < value.Length - 1 && Numeric(value[..slot], true) && Numeric(value[(slot + 1)..], false) && value[(slot + 1)..].Length <= 4 && !Grouped(value, sign);
    }

    private bool Grouped(string value, char sign)
    {
        string text = value[0] is '+' or '-' ? value[1..] : value;
        string[] list = text.Split(sign);
        return list.Length > 1 && list[0].Length is > 0 and <= 3 && list[^1].Length == 3 && list.All(Numeric) && list.Skip(1).All(item => item.Length == 3);
    }

    private bool Numeric(string value) => value.Length > 0 && value.All(char.IsDigit);

    private bool Numeric(string value, bool signed) => signed && value[0] is '+' or '-' ? Numeric(value[1..]) : Numeric(value);

    private bool Decimal(string value, char sign, out decimal amount)
    {
        var item = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        item.NumberDecimalSeparator = sign.ToString();
        item.NumberGroupSeparator = sign == ',' ? "." : ",";
        return decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, item, out amount);
    }

    private bool Delimiter(string value) => value.Contains('.') || value.Contains(',') || value.Contains(' ') || value.Contains('\u00A0');
}
#pragma warning restore S2325
