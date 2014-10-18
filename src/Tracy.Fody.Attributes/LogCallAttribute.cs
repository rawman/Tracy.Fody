using System;

namespace Tracy.Fody.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class LogCallAttribute : Attribute
    {
        public string LogAs { get; set; }
        public string IncludeTypes { get; set; }

        public LogCallAttribute()
        {
            LogAs = LogLevel.Info;
        }
    }

    public static class LogLevel
    {
        public const string Info = "LogInfo";
        public const string Trace = "LogTrace";
        public const string Debug = "LogDebug";
    }
}
