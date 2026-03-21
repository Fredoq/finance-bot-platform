using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
string bot = builder.Configuration["Parameters:telegram-bot-token"] ?? throw new InvalidOperationException("Missing Parameters:telegram-bot-token");
string hook = builder.Configuration["Parameters:telegram-webhook-secret"] ?? throw new InvalidOperationException("Missing Parameters:telegram-webhook-secret");
string key = builder.Configuration["Parameters:telegram-key-secret"] ?? throw new InvalidOperationException("Missing Parameters:telegram-key-secret");
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
await builder.Build().RunAsync();
