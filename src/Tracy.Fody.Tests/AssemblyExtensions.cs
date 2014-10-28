using System;
using System.Reflection;

namespace Tracy.Fody.Tests
{
    public static class AssemblyExtensions
    {
        public static object CreateInstance<T>(this Assembly assembly)
        {
            var type = assembly.GetType(typeof(T).FullName);
            return Activator.CreateInstance(type);
        }
    }
}
