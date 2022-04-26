using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureClient
{
    public class ServiceBusTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            var activity = Activity.Current;
            if (activity != null && activity.OperationName.StartsWith("ServiceBus"))
            {
                string endpoint = null;
                string queueName = null;

                foreach (var tag in activity.Tags)
                {
                    if (tag.Key == "peer.address")
                    {
                        endpoint = tag.Value;
                    }
                    else if (tag.Key == "message_bus.destination")
                    {
                        queueName = tag.Value;
                    }
                }

                if (endpoint == null || queueName == null)
                {
                    return;
                }

                string separator = "/";
                if (endpoint.EndsWith(separator))
                {
                    separator = string.Empty;
                }

                string eventHubInfo = string.Concat(endpoint, separator, queueName);

                if (telemetry is DependencyTelemetry dependency)
                {
                    dependency.Type = "Azure Service Bus";
                    dependency.Target = eventHubInfo;
                }
                else if (telemetry is RequestTelemetry request)
                {
                    request.Source = eventHubInfo;
                }
            }
        }
    }
}