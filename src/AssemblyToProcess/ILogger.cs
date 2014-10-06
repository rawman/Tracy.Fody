using System;

namespace AssemblyToProcess
{
    public interface ILogger
    {
        void LogInfo(string message);
        
        void LogTrace(string message);

        void LogDebug(string message);
    }

    public class Logger : ILogger
    {
        public void LogInfo(string message)
        {
            Console.WriteLine("INFO:" + message);
        }

        public void LogTrace(string message)
        {
            Console.WriteLine("TRACE:" + message);
        }

        public void LogDebug(string message)
        {
            Console.WriteLine("DEBUG:" + message);
        }
    }
}
