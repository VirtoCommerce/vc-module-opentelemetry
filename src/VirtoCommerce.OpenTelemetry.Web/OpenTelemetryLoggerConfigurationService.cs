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
    public void Configure(LoggerConfiguration loggerConfiguration)
    {
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

            var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "VirtoCommerce.Platform";
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
            };
        });
    }
}
