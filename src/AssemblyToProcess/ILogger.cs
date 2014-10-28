using System;

namespace AssemblyToProcess
{
    public interface ILogger
    {
        void LogInfo(string message);
        
        void LogTrace(string message);

        void LogDebug(string message);

        void LogCustom(string message);
    }

    public class Logger : ILogger
    {
        public Logger()
        {
            LogInfoImpl = Console.WriteLine;
            LogTraceImpl = Console.WriteLine;
            LogDebugImpl = Console.WriteLine;
            LogCustomImpl = Console.WriteLine;
        }

        public Action<string> LogInfoImpl { get; set; }
        public Action<string> LogTraceImpl { get; set; }
        public Action<string> LogDebugImpl { get; set; }
        public Action<string> LogCustomImpl { get; set; }

        public void LogInfo(string message)
        {
            LogInfoImpl(message);
        }

        public void LogTrace(string message)
        {
            LogTraceImpl(message);
        }

        public void LogDebug(string message)
        {
            LogDebugImpl(message);
        }

        public void LogCustom(string message)
        {
            LogCustomImpl(message);
        }
    }
}
