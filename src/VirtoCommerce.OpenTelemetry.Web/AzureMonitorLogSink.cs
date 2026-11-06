using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Serilog.Core;
using Serilog.Events;

namespace VirtoCommerce.OpenTelemetry.Web;

/// <summary>
/// Sends the platform's Serilog events to Application Insights through the OpenTelemetry log exporter of Azure Monitor.
/// The platform logs through Serilog only, so its events are replayed into an OpenTelemetry logger; the event is written
/// on the caller's thread, so the log record carries the trace and span of the current activity.
/// </summary>
public sealed class AzureMonitorLogSink : ILogEventSink, IDisposable
{
    private const string OriginalFormatKey = "{OriginalFormat}";
    private const string DefaultCategory = "VirtoCommerce.Platform";

    private readonly ILoggerFactory _loggerFactory;

    public AzureMonitorLogSink(string connectionString, string serviceName)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.ParseStateValues = true;
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                options.AddAzureMonitorLogExporter(exporter => exporter.ConnectionString = connectionString);
            }));
    }

    public void Emit(LogEvent logEvent)
    {
        var logger = _loggerFactory.CreateLogger(GetCategory(logEvent));
        var level = ToLogLevel(logEvent.Level);
        if (!logger.IsEnabled(level))
        {
            return;
        }

        var state = logEvent.Properties
            .Select(x => new KeyValuePair<string, object>(x.Key, Simplify(x.Value)))
            .Append(new KeyValuePair<string, object>(OriginalFormatKey, logEvent.MessageTemplate.Text))
            .ToList();

        logger.Log(level, default, state, logEvent.Exception, (_, _) => logEvent.RenderMessage(CultureInfo.InvariantCulture));
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    private static string GetCategory(LogEvent logEvent)
    {
        return logEvent.Properties.TryGetValue(Serilog.Core.Constants.SourceContextPropertyName, out var value) &&
            value is ScalarValue { Value: string category }
            ? category
            : DefaultCategory;
    }

    private static object Simplify(LogEventPropertyValue value)
    {
        return value is ScalarValue scalar ? scalar.Value : value.ToString();
    }

    private static LogLevel ToLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Critical,
        };
    }
}
