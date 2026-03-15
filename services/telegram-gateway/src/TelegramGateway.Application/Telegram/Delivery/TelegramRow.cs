namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramRow
{
    public TelegramRow(IReadOnlyList<TelegramButton> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        TelegramButton[] list = cells.Where(item => item is not null).ToArray();
        Cells = list.Length > 0 ? Array.AsReadOnly(list) : throw new ArgumentException("Telegram row cells are required", nameof(cells));
    }
    public IReadOnlyList<TelegramButton> Cells { get; }
}
