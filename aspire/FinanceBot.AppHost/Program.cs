using FinanceBot.AppHost;
using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
var app = new AppHostLayout();
app.Add(builder);
await builder.Build().RunAsync();
