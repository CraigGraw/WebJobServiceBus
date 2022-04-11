using System;
using System.IO;
using System.Threading.Tasks;
using AzureClient.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebJobApp
{
    class Program
    {
        private static IServiceProvider _serviceProvider;

        private static IConfiguration _configuration;

        private static void RegisterServices()
        {
            var services = new ServiceCollection();

            // build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            _configuration = configuration;

            services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBusSettings"));

            _serviceProvider = services.BuildServiceProvider(true);
        }

        private static void RegisterDependencyInjection(IServiceCollection services, ServiceBusSettings appSettings)
        {
            services.AddTransient<ISystemLogger>(x => new SystemLogger());
            services.AddTransient<IServiceBusCredentialsProvider>(x => new ServiceBusCredentialsProvider(appSettings.ServiceBus));

            services.AddTransient<IQueueClient>(x =>
                new AzureQueueClient(x.GetRequiredService<IServiceBusCredentialsProvider>(),
                appSettings.QueueName,
                appSettings.MaxSessions,
                appSettings.PreFetchCount,
                x.GetRequiredService<ISystemLogger>()
                ));

            services.AddHostedService<Application>();
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("WebJobApp");

            RegisterServices();
            
            await RunAsync();
            DisposeServices();

        }

        private static async Task RunAsync()
        {
            HostBuilder builder = new HostBuilder();
            builder.UseEnvironment("Development")
                .ConfigureWebJobs().ConfigureLogging((hostBuilderContext, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddLog4Net();


                    string instrumentationKey = _configuration["APPINSIGHTS:INSTRUMENTATIONKEY"];
                    if (!string.IsNullOrWhiteSpace(instrumentationKey))
                    {
                        loggingBuilder.AddApplicationInsightsWebJobs(o =>
                            o.InstrumentationKey = instrumentationKey
                        );
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    ServiceBusSettings serviceBusSettings = _configuration.GetSection("ServiceBusSettings").Get<ServiceBusSettings>();

                    RegisterDependencyInjection(services, serviceBusSettings);
                });

            IHost host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
