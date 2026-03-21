namespace FinanceBot.AppHost;

internal interface IAppHostLayout
{
    void Add(IDistributedApplicationBuilder builder);
}

internal sealed class AppHostLayout : IAppHostLayout
{
    public void Add(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        string bot = Read(builder, "Parameters:telegram-bot-token");
        string hook = Read(builder, "Parameters:telegram-webhook-secret");
        string key = Read(builder, "Parameters:telegram-key-secret");
        IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres", port: 5432);
        IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("finance-db", "finance");
        IResourceBuilder<RabbitMQServerResource> rabbit = builder.AddRabbitMQ("rabbitmq", port: 5672).WithManagementPlugin();
        builder.AddProject<Projects.FinanceCore_Api>("finance-core")
            .WithEnvironment("Postgres__ConnectionString", database)
            .WithEnvironment("RabbitMq__ConnectionString", rabbit)
            .WithHttpEndpoint(port: 8081, name: "http")
            .WaitFor(database)
            .WaitFor(rabbit);
        builder.AddProject<Projects.TelegramGateway_Api>("telegram-gateway")
            .WithEnvironment("RabbitMq__ConnectionString", rabbit)
            .WithEnvironment("Telegram__Bot__Token", bot)
            .WithEnvironment("Telegram__Webhook__SecretToken", hook)
            .WithEnvironment("Telegram__Keys__CurrentSecret", key)
            .WithHttpEndpoint(port: 8082, name: "http")
            .WaitFor(rabbit);
    }

    private static string Read(IDistributedApplicationBuilder builder, string name) => builder.Configuration[name] ?? throw new InvalidOperationException($"Missing {name}");
}
