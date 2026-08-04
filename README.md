# Open Telemetry Module

This module provides OpenTelemetry observability for VirtoCommerce Platform — metrics, distributed tracing, and structured logging via OTLP exporter.

## Features

- **Metrics**: ASP.NET Core, HTTP Client, Runtime, Process, EF Core, Elasticsearch, Redis instrumentation
- **Distributed Tracing**: ASP.NET Core, HTTP Client, Hangfire, EF Core, Elasticsearch, Redis instrumentation
- **Logging**: Serilog → OpenTelemetry via OTLP sink, with trace/span ID correlation
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
| `Endpoint` | Yes | — | OTLP collector endpoint (gRPC). Required to export data. |
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

**No data exported** — verify `OpenTelemetry:Endpoint` is set and the collector is reachable.

**Traces missing correlations** — ensure the collector supports OTLP gRPC on the configured endpoint.

**A module's spans are missing** — `OpenTelemetry:Sources` must be a JSON array (a comma-separated string registers nothing), and each entry must match the module's `ActivitySource` name exactly.

**A module's metrics are missing** — `OpenTelemetry:Meters` must be a JSON array (a comma-separated string registers nothing), and each entry must match the module's `Meter` name exactly.

## License

Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

https://virtocommerce.com/open-source-license

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

