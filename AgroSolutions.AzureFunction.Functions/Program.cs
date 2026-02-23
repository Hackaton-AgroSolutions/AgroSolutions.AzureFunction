using AgroSolutions.AzureFunction.Functions.Interfaces;
using AgroSolutions.AzureFunction.Functions.Services;
using AgroSolutions.AzureFunction.Infrastructure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service_name", "agro-solution-azurefunction-function")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] (CorrelationId={CorrelationId}) {Message:lj} {NewLine}{Exception}")
    .WriteTo.GrafanaLoki("http://loki:3100")
    .CreateLogger();

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("Messaging"));
builder.Services.AddSingleton<IInfluxDbService>(sp => new InfluxDbService(builder.Configuration));
builder.Services.AddSingleton<IRabbitConnectionProvider, RabbitConnectionProvider>();
builder.Services.AddScoped<IMessagingConnectionFactory, RabbitChannelFactory>();
builder.Services.AddSingleton(sp => Metrics.DefaultRegistry);

IHost host = builder.Build();

using AsyncServiceScope asyncServiceScope = host.Services.CreateAsyncScope();
IServiceProvider services = asyncServiceScope.ServiceProvider;

#region Ensures the creation of the exchange, queues, and message binds at startup.
try
{
    IMessagingConnectionFactory factory = services.GetRequiredService<IMessagingConnectionFactory>();
    IOptions<RabbitMqOptions> options = services.GetRequiredService<IOptions<RabbitMqOptions>>();
    await RabbitMqConnection.InitializeAsync(await factory.CreateChannelAsync(CancellationToken.None), options.Value);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error during messaging initialization.");
}
#endregion

host.Run();
