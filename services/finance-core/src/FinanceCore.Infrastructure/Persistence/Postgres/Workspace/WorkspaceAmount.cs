using System.Globalization;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceAmount
{
    private readonly AmountAxis axis;

    internal WorkspaceAmount() => axis = new AmountAxis();

    internal bool Try(string value, out decimal amount)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = value.Trim();
        if (Decimal(text, axis.Comma, out amount) && Candidate(text, axis.Comma))
        {
            return true;
        }
        if (Decimal(text, axis.Dot, out amount) && Candidate(text, axis.Dot))
        {
            return true;
        }
        bool current = decimal.TryParse(text, axis.Styles, CultureInfo.CurrentCulture, out decimal local);
        bool invariant = decimal.TryParse(text, axis.Styles, CultureInfo.InvariantCulture, out decimal global);
        if (string.IsNullOrWhiteSpace(text) || text.Contains(axis.Dot) && text.Contains(axis.Comma) || Delimiter(text) && current && invariant && local != global)
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

    internal int Scale(decimal value) => axis.Scale(value);

    private bool Candidate(string value, char sign)
    {
        int slot = value.LastIndexOf(sign);
        return slot > 0 && slot < value.Length - 1 && Numeric(value[..slot], true) && Numeric(value[(slot + 1)..], false) && value[(slot + 1)..].Length <= axis.Delimiters.Length && !Grouped(value, sign);
    }

    private bool Grouped(string value, char sign)
    {
        string text = axis.Signs.Contains(value[0]) ? value[1..] : value;
        string[] list = text.Split(sign);
        return list.Length > 1 && list[0].Length is > 0 and <= 3 && list[^1].Length == 3 && list.All(Numeric) && list.Skip(1).All(item => item.Length == 3);
    }

    private bool Numeric(string value) => value.Length > axis.Empty && value.All(char.IsDigit);

    private bool Numeric(string value, bool signed) => signed && axis.Signs.Contains(value[0]) ? Numeric(value[1..]) : Numeric(value);

    private bool Decimal(string value, char sign, out decimal amount)
    {
        var item = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        item.NumberDecimalSeparator = sign.ToString();
        item.NumberGroupSeparator = sign == axis.Comma ? axis.DotText : axis.CommaText;
        return decimal.TryParse(value, axis.DecimalStyles, item, out amount);
    }

    private bool Delimiter(string value) => value.IndexOfAny(axis.Delimiters) >= axis.Empty;

    private sealed class AmountAxis
    {
        internal AmountAxis()
        {
            Styles = NumberStyles.Number;
            DecimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            Delimiters = ['.', ',', ' ', '\u00A0'];
            Signs = ['+', '-'];
            Empty = 0;
        }

        internal char Comma => Delimiters[1];

        internal char Dot => Delimiters[0];

        internal string CommaText => Delimiters[1].ToString();

        internal NumberStyles DecimalStyles { get; }

        internal char[] Delimiters { get; }

        internal string DotText => Delimiters[0].ToString();

        internal int Empty { get; }

        internal char[] Signs { get; }

        internal NumberStyles Styles { get; }

        internal int Scale(decimal value) => ((decimal.GetBits(value)[3] >> 16) & 0xFF) + Empty;
    }
}
