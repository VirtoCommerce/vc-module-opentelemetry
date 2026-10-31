# Open Telemetry Module

This module provides OpenTelemetry observability for VirtoCommerce Platform — metrics, distributed tracing, and structured logging via OTLP exporter.

## Features

- **Metrics**: ASP.NET Core, HTTP Client, Runtime, Process, EF Core, Elasticsearch, Redis instrumentation
- **Distributed Tracing**: ASP.NET Core, HTTP Client, Hangfire, EF Core, Elasticsearch, Redis instrumentation
- **Logging**: Serilog → OpenTelemetry via OTLP sink, with trace/span ID correlation
- **Application Insights**: the same traces, metrics and logs exported to Azure Monitor with the official OpenTelemetry exporter, with or without an OTLP collector
- **Conditional Activation**: Only enabled when explicitly configured

## Prerequisites

- VirtoCommerce Platform 3.1002.0+
- OTLP-compatible collector (e.g. [Grafana Alloy](https://grafana.com/docs/alloy/), [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview))

## Installation

Copy the module to your platform `modules` directory. It will be automatically discovered and loaded by VirtoCommerce Platform.

## Configuration

Add to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317",
    "ServiceName": "VirtoCommerce.Platform",
    "Sources": [
      "VirtoCommerce.UCP"
    ],
    "Meters": [
      "VirtoCommerce.UCP"
    ]
  }
}
```

| Key | Required | Default | Description |
|-----|----------|---------|-------------|
| `Enabled` | Yes | `false` | Enables the module. Set to `true` to activate. |
| `Endpoint` | No | — | OTLP collector endpoint (gRPC). Required to export to a collector. |
| `AzureMonitor:ConnectionString` | No | — | Connection string of an Application Insights resource. Exports traces, metrics and logs to it, in addition to the OTLP endpoint when both are set. |
| `ServiceName` | No | `VirtoCommerce.Platform` | Service name reported in telemetry. |
| `Sources` | No | — | Additional `ActivitySource` names to collect traces from. Lets any module expose spans without referencing OpenTelemetry packages: the module names its `ActivitySource`, the deployment opts it in here. |
| `Meters` | No | — | Additional `Meter` names to collect metrics from. Lets any module expose metrics without referencing OpenTelemetry packages: the module names its `Meter`, the deployment opts it in here. |

Settings can also be provided via environment variables:

```
OpenTelemetry__Enabled=true
OpenTelemetry__Endpoint=http://collector:4317
OpenTelemetry__ServiceName=my-store
OpenTelemetry__Sources__0=VirtoCommerce.UCP
OpenTelemetry__Meters__0=VirtoCommerce.UCP
```

## Application Insights

Application Insights does not receive OTLP, so the module exports to it with the Azure Monitor exporter of OpenTelemetry when a connection string is set:

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "ServiceName": "my-store",
    "AzureMonitor": {
      "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://...in.applicationinsights.azure.com/"
    }
  }
}
```

Or as an environment variable: `OpenTelemetry__AzureMonitor__ConnectionString`. Keep the connection string out of source control.

| Signal | In Application Insights |
|--------|-------------------------|
| Incoming requests (server spans) | `requests` |
| Database, HTTP, Redis, Elasticsearch and module spans (client and internal spans) | `dependencies` |
| Platform logs, with the trace of their request | `traces` |
| Exceptions | `exceptions` |
| Metrics | `customMetrics` |

`ServiceName` is the cloud role name. A request opens as one end-to-end transaction with its database calls, the spans of the modules listed in `Sources`, and its log lines. To follow one request in **Logs**:

```kusto
union requests, dependencies, traces, exceptions
| where operation_Id == "<trace id>"
| order by timestamp asc
```

Use either this module or the Application Insights module for a resource, not both, or every request is recorded twice.

## What Gets Collected

### Metrics

| Source | Description |
|--------|-------------|
| ASP.NET Core | Request rate, duration, active connections |
| HTTP Client | Outbound request duration and status |
| .NET Runtime | GC, thread pool, memory |
| Process | CPU, memory |
| EF Core | Query counts and duration |
| Elasticsearch | Transport-level metrics |
| Kestrel | Connection and request metrics |

### Traces

| Source | Description |
|--------|-------------|
| ASP.NET Core | Incoming HTTP requests |
| HTTP Client | Outbound HTTP calls |
| EF Core | Database queries |
| Hangfire | Background job execution |
| Elasticsearch | Search and index operations |
| Redis | Cache operations |

### Logs

Structured logs are forwarded to the OTLP endpoint via Serilog with trace/span ID fields for correlation with distributed traces.

## Module Structure

```
src/
└── VirtoCommerce.OpenTelemetry.Web/
    ├── Module.cs                                # Module entry point
    ├── ServiceCollectionExtensions.cs           # OTel metrics and tracing registration
    ├── OpenTelemetryLoggerConfigurationService.cs  # Serilog → OTLP logging
    └── VirtoCommerce.OpenTelemetry.Web.csproj
```

## Troubleshooting

**Module not activating** — verify `OpenTelemetry:Enabled` is `true` in configuration.

**No data exported** — verify `OpenTelemetry:Endpoint` is set and the collector is reachable, or that `OpenTelemetry:AzureMonitor:ConnectionString` is set. Application Insights shows new data after 1–3 minutes.

**Traces missing correlations** — ensure the collector supports OTLP gRPC on the configured endpoint.

**A module's spans are missing** — `OpenTelemetry:Sources` must be a JSON array (a comma-separated string registers nothing), and each entry must match the module's `ActivitySource` name exactly.

**A module's metrics are missing** — `OpenTelemetry:Meters` must be a JSON array (a comma-separated string registers nothing), and each entry must match the module's `Meter` name exactly.

## License

Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

https://virtocommerce.com/open-source-license

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

