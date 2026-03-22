using FinanceCore.Application.Runtime.Flow;
using FinanceCore.Application.Workspace.Flow;
using FinanceCore.Application.Workspace.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceCore.Application.Composition;

/// <summary>
/// Registers the application services required by the finance core runtime.
/// </summary>
public static class ApplicationSetup
{
    /// <summary>
    /// Adds the application slices and supporting policies.
    /// </summary>
    /// <param name="items">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddFinanceCoreApplication(this IServiceCollection items)
    {
        items.AddSingleton<ICommandSlice>(item => new WorkspaceSlice(item.GetRequiredService<IWorkspacePort>()));
        items.AddSingleton<ICommandSlice>(item => new WorkspaceInputSlice(item.GetRequiredService<IWorkspaceInputPort>()));
        items.AddSingleton<ICommandFlow>(item => new CommandFlow(item.GetRequiredService<IEnumerable<ICommandSlice>>()));
        return items;
    }
}
