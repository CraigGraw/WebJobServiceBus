using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;

namespace AzureClient.ServiceBus
{
    public class SystemLogger : ISystemLogger
    {
        private ILog _adaptee;

        public void Debug(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            Log(filePath);
            _adaptee.Debug(message, exception);
        }

        public void Error(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            Log(filePath);
            _adaptee.Error(message, exception);
        }

        public void Fatal(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            Log(filePath);
            _adaptee.Fatal(message, exception);
        }

        public void Info(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            Log(filePath);
            _adaptee.Info(message, exception);
        }

        public void Warn(string message, Exception exception = null, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "")
        {
            Log(filePath);
            _adaptee.Warn(message, exception);
        }

        private void Log(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            string className = Path.GetFileNameWithoutExtension(filePath);
            _adaptee = LogManager.GetLogger(Assembly.GetExecutingAssembly(), className);
        }
    }
}
