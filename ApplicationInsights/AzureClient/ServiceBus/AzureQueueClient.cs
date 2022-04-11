using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace AzureClient.ServiceBus
{
    [ExcludeFromCodeCoverage]
    public class AzureQueueClient : IQueueClient
    {
        private readonly string _connectionString;

        private readonly ISystemLogger _logger;

        private readonly int _maxConcurrentSessions;

        private readonly int _prefetchCount;

        private readonly Lazy<ServiceBusClient> _serviceBusClient;

        private readonly string _queueName;

        public AzureQueueClient(IServiceBusCredentialsProvider credentialsProvider, string queueName, int maxConcurrentSessions, int prefetchCount, ISystemLogger logger)
        {
            _connectionString = credentialsProvider.ConnectionString;
            _queueName = queueName;
            _logger = logger;
            _serviceBusClient = new Lazy<ServiceBusClient>(InitialiseQueueClient);
            _maxConcurrentSessions = maxConcurrentSessions;
            _prefetchCount = prefetchCount;
        }

        public IQueueProcessor CreateQueueProcessor(Func<QueueMessageType, Guid, string, Task> callback)
        {
            return QueueProcessor.CreateQueueProcessor(
                _serviceBusClient.Value,
                _queueName,
                _maxConcurrentSessions,
                _prefetchCount,
                _logger,
                callback);
        }

        public async Task SendAsync(QueueMessageType type, string body, string sessionId, Guid correlationId, DateTime? schedule = null)
        {
            ServiceBusSender serviceBusSender = _serviceBusClient.Value.CreateSender(_queueName);
            ServiceBusMessage message = new ServiceBusMessage(System.Text.Encoding.UTF8.GetBytes(body))
            {
                ApplicationProperties =
                {
                    [QueuePropertyName.ContentType] = (int)type
                },

                CorrelationId = correlationId.ToString(),

                SessionId = sessionId
            };

            if (schedule != null)
            {
                message.ScheduledEnqueueTime = schedule.Value;
            }

            await serviceBusSender.SendMessageAsync(message);
        }

        private ServiceBusClient InitialiseQueueClient()
        {
            ServiceBusClient serviceBusClient = new ServiceBusClient(_connectionString);

            return serviceBusClient;
        }
    }
}
