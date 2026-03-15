using Microsoft.Extensions.Logging;

namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed class TelegramDeliveryFlow : ITelegramDeliveryFlow
{
    private readonly ITelegramDeliverySlice[] list;
    private readonly ILogger<TelegramDeliveryFlow> log;
    public TelegramDeliveryFlow(IEnumerable<ITelegramDeliverySlice> flow, ILogger<TelegramDeliveryFlow> log)
    {
        ArgumentNullException.ThrowIfNull(flow);
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        list = [.. flow];
    }
    public async ValueTask Run(string contract, ReadOnlyMemory<byte> body, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(contract))
        {
            log.LogWarning("Telegram delivery contract was rejected because the contract name was blank");
            throw new DeliveryException("Telegram delivery contract is not supported", false);
        }
        ITelegramDeliverySlice[] item = [.. list.Where(item => item.Match(contract)).Take(2)];
        if (item.Length == 0)
        {
            log.LogWarning("Telegram delivery contract was rejected because no slice matched");
            throw new DeliveryException("Telegram delivery contract is not supported", false);
        }
        if (item.Length > 1)
        {
            throw new InvalidOperationException("Telegram delivery contract matched multiple slices");
        }
        await item[0].Run(body, token);
    }
}
