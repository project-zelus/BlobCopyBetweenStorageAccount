using Funcs_DataMovement.Logging;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;

[assembly: FunctionsStartup(typeof(Funcs_DataMovement.Startup))]

namespace Funcs_DataMovement
{
    public class Startup : FunctionsStartup
    {
        private bool InclusionPredicate(LogEvent events)
        {
            if (events.Level > LogEventLevel.Verbose
                && events.Properties.TryGetValue("SourceContext", out LogEventPropertyValue SourceContextValue))
            {
                if (SourceContextValue.ToString().Contains("Function."))
                    return true;
            }
            return false;
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<ConfigLogging>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("ConfigLogging").Bind(settings);
                });

            // Pull Serilog Location specifics
            var SAS = Environment.GetEnvironmentVariable("Serilog:SAS");
            var AccountName = Environment.GetEnvironmentVariable("Serilog:AccountName");
            var tableEndpoint = new Uri(Environment.GetEnvironmentVariable("Serilog:TableUri"));
            var tableName = Environment.GetEnvironmentVariable("Serilog:TableName");

            // Registering Serilog provider
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.AzureTableStorageWithProperties(
                    sharedAccessSignature: SAS,
                    accountName: AccountName,
                    tableEndpoint: tableEndpoint,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    storageTableName: tableName,
                    propertyColumns: new string[] { "producer", "consumer", "file", "origin", "tags", "correlationId", "stacktrace" })
                .Filter.ByIncludingOnly(InclusionPredicate)
                .CreateLogger();

            builder.Services.AddLogging(lb => lb.AddSerilog(logger));

        }

    }
}