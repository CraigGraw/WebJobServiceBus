using System;
using System.Threading.Tasks;

namespace AzureClient.ServiceBus
{
    public interface IQueueProcessor : IAsyncDisposable
    {
        Task StartProcessingAsync();

        Task StopProcessingAsync();
    }
}
