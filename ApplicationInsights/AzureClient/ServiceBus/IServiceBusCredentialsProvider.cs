namespace AzureClient.ServiceBus
{
    public interface IServiceBusCredentialsProvider
    {
        string ConnectionString { get; }
    }
}
