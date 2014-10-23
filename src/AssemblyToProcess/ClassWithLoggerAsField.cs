using Tracy.Fody.Attributes;

namespace AssemblyToProcess
{
    [LogCall]
    class ClassWithLoggerAsField
    {
        public readonly Logger Logger = new Logger();

        void Foo()
        {
        }
    }
}
