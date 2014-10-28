using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace Tracy.Fody.Tests
{
    internal class Waver
    {
        public static Assembly WaveAssembly(string assemblyPath)
        {
#if (!DEBUG)
            assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            string newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, newAssemblyPath, true);

            var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
            moduleDefinition.Write(newAssemblyPath);

            return Assembly.LoadFile(newAssemblyPath);
        }
    }
}
