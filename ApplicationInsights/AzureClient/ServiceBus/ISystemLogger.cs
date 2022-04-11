using System;
using System.Runtime.CompilerServices;

namespace AzureClient.ServiceBus
{
    public interface ISystemLogger
    {  
        void Debug(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "");
        
        void Error(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "");
        
        void Fatal(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "");
        
        void Info(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "");
        
        void Warn(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "");
    }
}
