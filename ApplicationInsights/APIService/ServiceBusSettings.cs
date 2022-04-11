namespace APIService
{
    public class ServiceBusSettings
    {
        public string ServiceBus { get; set; }

        public string QueueName { get; set; }

        public int MaxSessions { get; set; }

        public int PreFetchCount { get; set; }
    }
}