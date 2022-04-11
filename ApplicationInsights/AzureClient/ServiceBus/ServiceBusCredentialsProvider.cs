namespace AzureClient.ServiceBus
{
    public class ServiceBusCredentialsProvider : IServiceBusCredentialsProvider
    {
        public ServiceBusCredentialsProvider(string key)
        {
            ConnectionString = key;
        }

        public string ConnectionString { get; }
    }
}
