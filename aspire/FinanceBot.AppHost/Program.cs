IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<ParameterResource> bot = builder.AddParameter("telegram-bot-token", secret: true);
IResourceBuilder<ParameterResource> hook = builder.AddParameter("telegram-webhook-secret", secret: true);
IResourceBuilder<ParameterResource> key = builder.AddParameter("telegram-key-secret", secret: true);
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
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(rabbit);
await builder.Build().RunAsync();
