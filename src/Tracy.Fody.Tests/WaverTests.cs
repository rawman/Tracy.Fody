using System;
using System.IO;
using System.Reflection;
using AssemblyToProcess;
using Moq;
using NUnit.Framework;

namespace Tracy.Fody.Tests
{
    class WaverTests
    {
        private static readonly string ProjectPath =
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));

        private static readonly string AssemblyPath =
            Path.Combine(Path.GetDirectoryName(ProjectPath), @"bin\Debug\AssemblyToProcess.dll");

        private Assembly _wavedAssembly;
        private dynamic _sampleClass;
        private Mock<ILogger> _loggerMock;

        [TestFixtureSetUp]
        public void Setup()
        {
            _wavedAssembly = Waver.WaveAssembly(AssemblyPath);

            _sampleClass = _wavedAssembly.CreateInstance<SampleClass>();
            _loggerMock = new Mock<ILogger>();
            _sampleClass.Logger.LogInfoImpl = new Action<string>((m) => _loggerMock.Object.LogInfo(m));
        }

#if(DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(AssemblyPath, _wavedAssembly.Location);
        }
#endif

        [Test]
        public void NoParameters()
        {
            _sampleClass.NoParameters();

            _loggerMock.Verify(x => x.LogInfo("Called NoParameters"), Times.Once);
        }

        [Test]
        public void OneParameters()
        {
            _sampleClass.OneParameter(1);

            _loggerMock.Verify(x => x.LogInfo("Called OneParameter with a=1"), Times.Once);
        }

        [Test]
        public void TwoParameters()
        {
            _sampleClass.TwoParameters(1, "wow");

            _loggerMock.Verify(x => x.LogInfo("Called TwoParameters with a=1, b=wow"), Times.Once);
        }
    }
}
