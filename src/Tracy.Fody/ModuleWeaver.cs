using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Tracy.Fody
{
    public class ModuleWeaver
    {
        public const string LogCallAttributeName = "LogCallAttribute";

        public IAssemblyResolver AssemblyResolver { get; set; }

        public XElement Config { get; set; }

        public List<string> DefineConstants { get; set; }

        public Action<string> LogError { get; set; }

        public Action<string, SequencePoint> LogErrorPoint { get; set; }

        public Action<string> LogInfo { get; set; }

        public Action<string> LogWarning { get; set; }

        public Action<string, SequencePoint> LogWarningPoint { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        private bool LogDebugOutput { get; set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
            LogWarning = m => { };
            LogWarningPoint = (m, p) => { };
            LogError = m => { };
            LogErrorPoint = (m, p) => { };

            Config = new XElement("Tracy.Fody");
            DefineConstants = new List<string>();
        }


        public void Execute()
        {
            var typeDefinition = new TypeDefinition(GetType().Assembly.GetName().Name, "TypeInjectedBy" + GetType().Name, TypeAttributes.Public, ModuleDefinition.Import(typeof(object)));
            ModuleDefinition.Types.Add(typeDefinition);

            var methods = SelectMethods(ModuleDefinition).ToArray();
            foreach (var method in methods)
            {
                LogWarning("Add call logging to method " + method.Name);
                WeaveMethod(method);
            }
        }

        private IEnumerable<MethodDefinition> SelectMethods(ModuleDefinition moduleDefinition)
        {
            LogInfo(string.Format("Searching for Methods in assembly ({0}).", moduleDefinition.Name));

            var definitions = new HashSet<MethodDefinition>();

            definitions.UnionWith(
                moduleDefinition.Types.SelectMany(x => x.Methods.Where(y => y.ContainsAttribute(LogCallAttributeName))));
            definitions.UnionWith(
                moduleDefinition.Types.Where(x => x.IsClass && x.ContainsAttribute(LogCallAttributeName))
                    .SelectMany(x => x.Methods)
                    .Where(x =>(!x.IsSpecialName && !x.IsGetter && !x.IsSetter && !x.IsConstructor &&
                                !x.ContainsAttribute(moduleDefinition.ImportType<CompilerGeneratedAttribute>()))));

            return definitions;
        }

        private void WeaveMethod(MethodDefinition methodDefinition)
        {
            methodDefinition.Body.InitLocals = true;

            methodDefinition.Body.SimplifyMacros();

            var processor = methodDefinition.Body.GetILProcessor();
            var methodBodyFirstInstruction = methodDefinition.Body.Instructions.First();
            if (methodDefinition.IsConstructor)
                methodBodyFirstInstruction = methodDefinition.Body.Instructions.First(i => i.OpCode == OpCodes.Call).Next;

            var instuctions = WaveMethod(methodDefinition, processor);
            processor.InsertBefore(methodBodyFirstInstruction, instuctions);

            methodDefinition.Body.OptimizeMacros();
        }

        public IEnumerable<Instruction> WaveMethod(MethodDefinition methodDefinition, ILProcessor processor)
        {
            var propertyGet = methodDefinition.DeclaringType.GetPropertyGet("Logger");
            if (propertyGet == null)
            {
                LogError("Can not find logger property");
                //todo: try find public field
                return Enumerable.Empty<Instruction>();
            }

            var propertyGetReturnTypeDefinition = propertyGet.ReturnType.Resolve();
            var log = methodDefinition.Module.Import(
                GetLogMethod(propertyGetReturnTypeDefinition, "LogInfo"));

            if (log == null)
            {
                LogError("Can not find log method ");
                return Enumerable.Empty<Instruction>();
            }

            var builder = new MethodBuilder(methodDefinition, processor);
            var objectArrayVar = builder.AddVariable<object[]>();
            builder.Add(OpCodes.Nop)
                .Add(OpCodes.Ldarg_0) //todo: not needed when Logger is static 
                .AddCall(propertyGet)
                .Add(OpCodes.Ldstr, FormatMethodCall(methodDefinition));

            if (methodDefinition.Parameters.Count > 0)
            {
                builder.Add(OpCodes.Ldc_I4, methodDefinition.Parameters.Count)
                    .Add(OpCodes.Newarr, methodDefinition.Module.ImportType<object>())
                    .Add(OpCodes.Stloc, objectArrayVar);

                // Set object[] values
                for (int i = 0; i < methodDefinition.Parameters.Count; i++)
                {
                    var parameterType = methodDefinition.Parameters[i].ParameterType;

                    builder.Add(OpCodes.Ldloc, objectArrayVar)
                        .Add(OpCodes.Ldc_I4, i)
                        .Add(OpCodes.Ldarg, methodDefinition.IsStatic ? i : i + 1)
                        .Add(parameterType.IsValueType ? new []{processor.Create(OpCodes.Box, parameterType)} : new Instruction[0])
                        .Add(OpCodes.Stelem_Ref);
                }

                builder.Add(OpCodes.Ldloc, objectArrayVar)
                    .AddCall((string x, object[] y) => String.Format(x, y));
            }

            builder.Add(OpCodes.Callvirt, log);

            return builder.Instructions;
        }

        private string FormatMethodCall(MethodDefinition methodDefinition)
        {
            if (methodDefinition.Parameters.Count == 0)
                return String.Format("Called {0}", methodDefinition.NoInlining);
            else
                return String.Format("Called {0} with {1}", methodDefinition.Name, FormatMethodParameters(methodDefinition));
        }

        private string FormatMethodParameters(MethodDefinition methodDefinition)
        {
            return string.Join(" ",
                methodDefinition.Parameters.Select((x, i) => string.Format("{0}={{{1}}}", x.Name, i)));
        }

        private MethodDefinition GetLogMethod(TypeDefinition loggerType, string logMethod)
        {
            return (loggerType.GetMethod(logMethod, new[] { loggerType.Module.ImportType<string>() }));
        }
    }
}
