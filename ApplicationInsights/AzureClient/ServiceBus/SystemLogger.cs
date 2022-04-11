using System;
using System.Runtime.CompilerServices;
using log4net;

namespace AzureClient.ServiceBus
{
    public class SystemLogger : ISystemLogger
    {
        private ILog _adaptee;

        public void Debug(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {   
            _adaptee.Debug(message, exception);
        }

        public void Error(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            _adaptee.Error(message, exception);
        }

        public void Fatal(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {   
            _adaptee.Fatal(message, exception);
        }

        public void Info(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {   
            _adaptee.Info(message, exception);
        }

        public void Warn(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {   
            _adaptee.Warn(message, exception);
        }
    }
}
