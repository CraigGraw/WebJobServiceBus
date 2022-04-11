using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using AzureClient.ServiceBus;

namespace APIService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ISystemLogger _logger;

        private readonly IQueueClient _azureQueueClient;

        public MessageController(ISystemLogger logger, IQueueClient client)
        {
            _logger = logger;
            _azureQueueClient = client;
        }

        [HttpPost]
        public async Task<string> PostAsync()
        {
            return await SendMessageAsync();
        }

        [HttpGet]
        public async Task<string> GetAsync()
        {
            return await SendMessageAsync();
        }

        private async Task<string> SendMessageAsync()
        {
            var messageId = 1;
            var sessionId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid();
            var jsonString = CreateMessagePayload(messageId, sessionId.ToString());

            Console.WriteLine(
                $"2.  Sending Message | Type: {QueueMessageType.Message}, correlationId: {correlationId}, Body: {jsonString}");

            _logger.Info($"2.  Sending Message | Type: {QueueMessageType.Message}, correlationId: {correlationId}, Body: {jsonString}");

            await _azureQueueClient.SendAsync(QueueMessageType.Message, jsonString, sessionId,
                correlationId);

            return $"Message send: {correlationId}";
        }

        private static string CreateMessagePayload(int messageId, string sessionId)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture);
            string messageBody1 = $"Message{messageId}@{timestamp}";
            QueueTestMessage queueTestMessage = new QueueTestMessage(messageBody1, sessionId);
            string jsonString = JsonSerializer.Serialize(queueTestMessage);
            return jsonString;
        }
    }
}
