using System;

namespace Tracy.Fody.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LogCallAttribute : Attribute
    {
    }
}
