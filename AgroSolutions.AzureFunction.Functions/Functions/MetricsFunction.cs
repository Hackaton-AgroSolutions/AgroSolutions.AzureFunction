using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Prometheus;

namespace AgroSolutions.AzureFunction.Functions.Functions;

public static class MetricsFunction
{
    private static readonly Counter RequestCounter = Metrics.CreateCounter("http_requests_total", "Total HTTP Requests");

    [Function("Metrics")]
    public static async Task Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metrics")] HttpRequest req)
    {
        RequestCounter.Inc();
        req.HttpContext.Response.ContentType = "text/plain; version=0.0.4";
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(req.HttpContext.Response.Body);
    }
}
