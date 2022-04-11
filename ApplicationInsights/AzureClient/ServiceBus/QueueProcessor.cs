using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace AzureClient.ServiceBus
{
    public class QueueProcessor : IQueueProcessor
    {
        private readonly ISystemLogger _logger;

        private readonly ServiceBusSessionProcessor _serviceBusSessionProcessor;

        private QueueProcessor(
            ServiceBusClient serviceBusClient,
            string queueName,
            int maxConcurrentSessions,
            int prefetchCount,
            ISystemLogger logger,
            Func<QueueMessageType, Guid, string, Task> callback)
        {
            _logger = logger;

            Func<ProcessSessionMessageEventArgs, Task> onMessage = CreateOnMessage(callback);

            Func<ProcessErrorEventArgs, Task> onError = args =>
            {
                TelemetryClient telemetryClient = new TelemetryClient();
                telemetryClient.TrackException(args.Exception);

                return Task.CompletedTask;
            };

            var options = new ServiceBusSessionProcessorOptions
            {
                AutoCompleteMessages = false,
                SessionIdleTimeout = TimeSpan.FromMilliseconds(100)
            };

            if (maxConcurrentSessions > 0)
            {
                options.MaxConcurrentSessions = maxConcurrentSessions;
            }

            if (prefetchCount > 0)
            {
                options.PrefetchCount = prefetchCount;
            }

            ServiceBusSessionProcessor serviceBusSessionProcessor = serviceBusClient.CreateSessionProcessor(queueName, options);

            // configure the message and error handler to use
            serviceBusSessionProcessor.ProcessMessageAsync += onMessage;
            serviceBusSessionProcessor.ProcessErrorAsync += onError;

            _serviceBusSessionProcessor = serviceBusSessionProcessor;
        }

        public static IQueueProcessor CreateQueueProcessor(
            ServiceBusClient serviceBusClient, 
            string queueName, 
            int maxConcurrentSessions, 
            int prefetchCount, 
            ISystemLogger logger,
            Func<QueueMessageType, Guid, string, Task> callback)
        {
            QueueProcessor queueProcessor = new QueueProcessor(serviceBusClient, queueName, maxConcurrentSessions, prefetchCount, logger, callback);

            return queueProcessor;
        }

        public async Task StartProcessingAsync()
        {
            await _serviceBusSessionProcessor.StartProcessingAsync();
        }

        public async Task StopProcessingAsync()
        {
            await _serviceBusSessionProcessor.StopProcessingAsync();
        }
       
        private static string CreateLogMessage(string messageTitle, string sessionId, ServiceBusReceivedMessage receivedMessage)
        {
            try
            {
                string body = System.Text.Encoding.UTF8.GetString(receivedMessage.Body);
                JToken data = JToken.Parse(body);
                string logMessage
                    = Invariant($"{messageTitle} ")
                    + Invariant($"SessionId: '{sessionId}'/'{receivedMessage.SessionId}' ")
                    + Invariant($"MessageId: '{receivedMessage?.MessageId}' ")
                    + Invariant($"CorrelationId: '{receivedMessage?.CorrelationId}'");

                return logMessage;
            }
            catch (Exception e)
            {
                return $"Error occurred when creating log message. Message title: '{messageTitle}'. Error: '{e.Message}'";
            }
        }

        private async Task AbandonMessageAfterErrorAsync(ProcessSessionMessageEventArgs sessionArgs, Exception error)
        {
            try
            {
                TelemetryClient telemetryClient = new TelemetryClient();
                telemetryClient.TrackException(error);

                await sessionArgs.AbandonMessageAsync(sessionArgs.Message);

                string logMessage = CreateLogMessage("Queue message abandoned after error.", sessionArgs.SessionId, sessionArgs.Message);
                _logger.Error(logMessage, error);
            }
            catch (Exception e)
            {
                string logMessage = CreateLogMessage("Exception while abandoning queue message after error.", sessionArgs.SessionId, sessionArgs.Message);
                _logger.Error(logMessage, e);
            }
        }
        
        private async Task CompleteMessageAfterErrorAsync(ProcessSessionMessageEventArgs sessionArgs, Exception error)
        {
            try
            {
                TelemetryClient telemetryClient = new TelemetryClient();
                telemetryClient.TrackException(error);

                await sessionArgs.CompleteMessageAsync(sessionArgs.Message);

                string logMessage = CreateLogMessage("Queue message completed after error.", sessionArgs.SessionId, sessionArgs.Message);
                _logger.Error(logMessage, error);
            }
            catch (Exception e)
            {
                string logMessage = CreateLogMessage("Exception while completing queue message after error.", sessionArgs.SessionId, sessionArgs.Message);
                _logger.Error(logMessage, e);
            }
        }
        
        private Func<ProcessSessionMessageEventArgs, Task> CreateOnMessage(Func<QueueMessageType, Guid, string, Task> callback)
        {
            Func<ProcessSessionMessageEventArgs, Task> onMessage = async (sessionArgs) =>
            {
                while (true)
                {
                    try
                    {
                        ServiceBusReceivedMessage sessionBusReceivedMessage = sessionArgs.Message;
                        if (!Guid.TryParse(sessionBusReceivedMessage.CorrelationId, out Guid correlationId))
                        {
                            correlationId = Guid.NewGuid();
                            _logger.Warn(
                                $"CorrelationId: {sessionBusReceivedMessage.CorrelationId} is not a recognized GUID format. Created new id: {correlationId}");
                        }

                        QueueMessageType messageType =
                            (QueueMessageType)sessionBusReceivedMessage.ApplicationProperties[
                                QueuePropertyName.ContentType];
                        string body = System.Text.Encoding.UTF8.GetString(sessionBusReceivedMessage.Body);

                        double secondsQueued = (DateTime.UtcNow - sessionBusReceivedMessage.EnqueuedTime).TotalSeconds;

                        _logger.Info("Dequeueing  " + messageType + " message CorrelationId=" + correlationId +
                                     " SecondsQueued=" + secondsQueued);
                        string metricName =
                            $"{Assembly.GetEntryAssembly()?.FullName?.Split(',')[0]}.MessageSecondsQueued";

                        TelemetryClient telemetryClient = new TelemetryClient();
                        telemetryClient.TrackMetric(metricName, secondsQueued);

                        await callback.Invoke(messageType, correlationId, body);

                        await sessionArgs.CompleteMessageAsync(sessionBusReceivedMessage);

                        string logMessage = CreateLogMessage("Queue message completed.", sessionArgs.SessionId,
                            sessionBusReceivedMessage);
                        _logger.Info(logMessage);

                        return;
                    }
                    catch (ServiceBusException serviceBusException)
                    {
                        if (serviceBusException.IsTransient)
                        {
                            string logMessage = CreateLogMessage("Queue message is transient.", sessionArgs.SessionId,
                                sessionArgs.Message);
                            _logger.Error(logMessage, serviceBusException);

                            Thread.Sleep(200);
                            continue;
                        }

                        await AbandonMessageAfterErrorAsync(sessionArgs, serviceBusException);
                        return;
                    }
                    catch (TimeoutException timeoutException)
                    {
                        await AbandonMessageAfterErrorAsync(sessionArgs, timeoutException);
                        return;
                    }
                    catch (Exception exception)
                    {
                        await CompleteMessageAfterErrorAsync(sessionArgs, exception);
                        return;
                    }
                }
            };

            return onMessage;
        }

        public async ValueTask DisposeAsync()
        {
            if (_serviceBusSessionProcessor != null)
            {
                await _serviceBusSessionProcessor.DisposeAsync();
            }
        }
    }
}
