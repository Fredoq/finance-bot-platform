using FinanceCore.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FinanceCore.Api.Tests.Infrastructure;

internal sealed class CoreApiFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> note;
    internal CoreApiFactory(IDictionary<string, string?> note) => this.note = note ?? throw new ArgumentNullException(nameof(note));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, data) => data.AddInMemoryCollection(note));
    }
}
