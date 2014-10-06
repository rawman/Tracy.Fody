using Tracy.Fody.Attributes;

namespace AssemblyToProcess
{
    public class SampleClass
    {
        public SampleClass()
        {
            Logger = new Logger();
        }

        public ILogger Logger { get; set; }

        [LogCall]
        public void NoParameters()
        {
            
        }
    }
}
