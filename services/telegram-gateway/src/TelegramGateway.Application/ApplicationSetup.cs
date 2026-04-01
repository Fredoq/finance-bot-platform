using Microsoft.Extensions.DependencyInjection;
using TelegramGateway.Application.Entry.Workspace.Slices;
using TelegramGateway.Application.Entry.Workspace.Slices.Start;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Application.Telegram.Flow;

namespace TelegramGateway.Application;

/// <summary>
/// Registers the application services required by the Telegram gateway runtime.
/// </summary>
public static class ApplicationSetup
{
    /// <summary>
    /// Adds the application slices and supporting policies.
    /// </summary>
    /// <param name="items">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddTelegramGatewayApplication(this IServiceCollection items)
    {
        items.AddSingleton<ITelegramKeys, TelegramKeys>();
        items.AddSingleton<WorkspaceBody>();
        items.AddSingleton<WorkspaceHtml>();
        items.AddSingleton<WorkspaceText>();
        items.AddSingleton<WorkspaceKeys>();
        items.AddSingleton<IWorkspaceScreen, WorkspaceScreen>();
        items.AddSingleton<ITelegramSlice>(item => new StartSlice(item.GetRequiredService<IOpaqueKey>(), item.GetRequiredService<IBusPort>()));
        items.AddSingleton<ITelegramSlice>(item => new ActionSlice(item.GetRequiredService<IOpaqueKey>(), item.GetRequiredService<IBusPort>(), item.GetRequiredService<ITelegramPort>(), item.GetRequiredService<ITelegramContextPort>()));
        items.AddSingleton<ITelegramSlice>(item => new TextSlice(item.GetRequiredService<IOpaqueKey>(), item.GetRequiredService<IBusPort>(), item.GetRequiredService<ITelegramContextPort>()));
        items.AddSingleton<ITelegramDeliverySlice>(item => new WorkspaceViewSlice(item.GetRequiredService<IOpaqueKey>(), item.GetRequiredService<ITelegramPort>(), item.GetRequiredService<ITelegramContextPort>(), item.GetRequiredService<IWorkspaceScreen>(), item.GetRequiredService<ITelegramKeys>()));
        items.AddSingleton<ITelegramFlow, TelegramFlow>();
        items.AddSingleton<ITelegramDeliveryFlow, TelegramDeliveryFlow>();
        return items;
    }
}
