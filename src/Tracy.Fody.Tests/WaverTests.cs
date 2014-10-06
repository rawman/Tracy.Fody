using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace Tracy.Fody.Tests
{
    class WaverTests
    {
        Assembly assembly;
        string newAssemblyPath;
        string assemblyPath;

        [TestFixtureSetUp]
        public void Setup()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
            assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, newAssemblyPath, true);

            var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
            moduleDefinition.Write(newAssemblyPath);

            assembly = Assembly.LoadFile(newAssemblyPath);
        }

#if(DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(assemblyPath, newAssemblyPath);
        }
#endif
    }
}
