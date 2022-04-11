using AzureClient.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APIService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            string instrumentationKey = Configuration["APPINSIGHTS:INSTRUMENTATIONKEY"];
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                services.AddApplicationInsightsTelemetry(instrumentationKey);
            }

            ServiceBusSettings appSettings = Configuration.GetSection("ServiceBusSettings").Get<ServiceBusSettings>();

            RegisterDependencyInjection(services, appSettings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
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
        }
    }
}
