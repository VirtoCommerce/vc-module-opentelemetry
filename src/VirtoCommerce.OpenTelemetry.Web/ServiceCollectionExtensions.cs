using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace VirtoCommerce.OpenTelemetry.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenTelemetryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Note: OpenTelemetry logging is integrated via Serilog sink (see OpenTelemetryLoggerConfigurationService)
        // This avoids duplicate logging providers and follows VirtoCommerce Platform patterns

        var otelBuilder = services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddEventCountersInstrumentation(options =>
                    {
                        options.AddEventSources("Microsoft.AspNetCore.Hosting", "Microsoft-AspNetCore-Server-Kestrel");
                    })
                    .AddMeter("Microsoft.EntityFrameworkCore", "Elastic.Transport");
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddHangfireInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddElasticsearchClientInstrumentation(options =>
                    {
                        options.SuppressDownstreamInstrumentation = true;
                        options.ParseAndFormatRequest = true;
                    })
                    .AddSource("Elastic.Transport")
                    .AddRedisInstrumentation();
            });

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(configuration["OpenTelemetry:Endpoint"]))
        {
            otelBuilder.UseOtlpExporter();
        }

        return services;
    }
}
