using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Logger;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.OpenTelemetry.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        if (!Configuration.GetValue("OpenTelemetry:Enabled", false))
        {
            return;
        }

        // Integrate Serilog → OpenTelemetry logging via platform's Serilog pipeline
        serviceCollection.AddTransient<ILoggerConfigurationService, OpenTelemetryLoggerConfigurationService>();

        // Register OpenTelemetry metrics, tracing, and OTLP exporter
        serviceCollection.AddOpenTelemetryModule(Configuration);
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        // Nothing to do here
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
