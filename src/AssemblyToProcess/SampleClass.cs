using System;
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

        public void OneParameters(int a)
        {
        }

        public void TwoParameters(int a, string b)
        {
        }
    }
}
