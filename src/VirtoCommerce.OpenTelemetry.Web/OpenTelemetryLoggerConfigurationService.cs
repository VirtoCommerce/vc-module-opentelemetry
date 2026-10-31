using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using VirtoCommerce.Platform.Core.Logger;

namespace VirtoCommerce.OpenTelemetry.Web;

/// <summary>
/// Configures Serilog to send logs to OpenTelemetry via OTLP.
/// Integrates with the platform's Serilog configuration pipeline.
/// </summary>
public class OpenTelemetryLoggerConfigurationService(IConfiguration configuration) : ILoggerConfigurationService
{
    // One sink for the process: the platform builds its logger more than once while it starts.
    private static readonly object _azureMonitorLock = new();
    private static AzureMonitorLogSink _azureMonitorSink;

    public void Configure(LoggerConfiguration loggerConfiguration)
    {
        var azureMonitorConnectionString = ServiceCollectionExtensions.GetAzureMonitorConnectionString(configuration);
        if (azureMonitorConnectionString != null)
        {
            loggerConfiguration.WriteTo.Sink(GetAzureMonitorSink(azureMonitorConnectionString));
        }

        var endpoint = configuration["OpenTelemetry:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = OtlpProtocol.Grpc;

            // Include trace context for correlation with distributed traces
            options.IncludedData = IncludedData.TraceIdField |
                                   IncludedData.SpanIdField |
                                   IncludedData.MessageTemplateTextAttribute |
                                   IncludedData.MessageTemplateMD5HashAttribute;

            var serviceName = ServiceCollectionExtensions.GetServiceName(configuration);
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
            };
        });
    }

    private AzureMonitorLogSink GetAzureMonitorSink(string connectionString)
    {
        lock (_azureMonitorLock)
        {
            return _azureMonitorSink ??= new AzureMonitorLogSink(connectionString, ServiceCollectionExtensions.GetServiceName(configuration));
        }
    }
}
