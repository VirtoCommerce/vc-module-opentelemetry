using System.Collections.Generic;
using System.Linq;
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

        var builder = services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddEventCountersInstrumentation(options =>
                    {
                        options.AddEventSources("Microsoft.AspNetCore.Hosting", "Microsoft-AspNetCore-Server-Kestrel");
                    })
                    .AddMeter("Microsoft.EntityFrameworkCore", "Elastic.Transport");

                // Module Meters are opt-in per deployment via "OpenTelemetry:Meters".
                foreach (var meter in GetMetricMeters(configuration))
                {
                    metrics.AddMeter(meter);
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
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

                // Module ActivitySources are opt-in per deployment via "OpenTelemetry:Sources".
                foreach (var source in GetTracingSources(configuration))
                {
                    tracing.AddSource(source);
                }
            });

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(configuration["OpenTelemetry:Endpoint"]))
        {
            builder.UseOtlpExporter();
        }

        return services;
    }

    private static IEnumerable<string> GetTracingSources(IConfiguration configuration)
    {
        return configuration.GetSection("OpenTelemetry:Sources")
            .GetChildren()
            // AddSource throws on a null/whitespace name — a stray empty config entry must not fail module init.
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Value!);
    }

    private static IEnumerable<string> GetMetricMeters(IConfiguration configuration)
    {
        return configuration.GetSection("OpenTelemetry:Meters")
            .GetChildren()
            // AddMeter throws on a null/whitespace name — a stray empty config entry must not fail module init.
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Value!);
    }
}
