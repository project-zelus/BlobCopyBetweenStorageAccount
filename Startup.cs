using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;


[assembly: FunctionsStartup(typeof(Funcs_DataMovement.Startup))]

namespace Funcs_DataMovement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Registering Configurations (IOptions pattern)
            var SAS = Environment.GetEnvironmentVariable("Serilog:SAS"); 
            var AccountName = Environment.GetEnvironmentVariable("Serilog:AccountName"); 
            var tableEndpoint = new Uri(Environment.GetEnvironmentVariable("Serilog:TableUri"));

            // Registering Serilog provider
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.AzureTableStorage(SAS, AccountName, tableEndpoint, Serilog.Events.LogEventLevel.Information, storageTableName: "logs").Enrich.FromLogContext()
                .CreateLogger();

            builder.Services.AddLogging(lb => lb.AddSerilog(logger));

        }
    }
}