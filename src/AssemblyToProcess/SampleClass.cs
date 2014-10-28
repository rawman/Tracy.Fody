using Tracy.Fody.Attributes;

namespace AssemblyToProcess
{
    [LogCall]
    public class SampleClass
    {
        public SampleClass()
        {
            Logger = new Logger();
        }

        public ILogger Logger { get; set; }


        public void NoParameters()
        {
        }

        public void OneParameter(int a)
        {
        }

        public void TwoParameters(int a, string b)
        {
        }

        public void Generic<T>(int a)
        {
        }

        [LogCall(LogAs = LogLevel.Trace)]
        public void WithTraceLevel()
        {
        }

        [LogCall(LogAs = "LogCustom")]
        public void WithCustomLog()
        {
        }
    }
}
