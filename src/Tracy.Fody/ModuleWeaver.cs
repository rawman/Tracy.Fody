using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Tracy.Fody
{
    public class ModuleWeaver
    {
        public const string LoggerInstanceName = "Logger";
        public const string LogCallAttributeName = "LogCallAttribute";
        public const string DefaultLogMethod = "LogInfo";

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
            foreach (var method in SelectMethods(ModuleDefinition).ToArray())
            {
                LogWarning("Add call logging to method " + method.Name);
                WeaveMethod(method);
            }
        }

        private IEnumerable<MethodDefinition> SelectMethods(ModuleDefinition moduleDefinition)
        {
            LogInfo(string.Format("Searching for Methods in assembly ({0}).", moduleDefinition.Name));

            var definitions = new HashSet<MethodDefinition>();

            definitions.UnionWith(SelectMethods(moduleDefinition, x => true, x => x.ContainsAttribute(LogCallAttributeName)));
            definitions.UnionWith(SelectMethods(moduleDefinition, x => x.ContainsAttribute(LogCallAttributeName), x => true));

            var logCallAttribute = moduleDefinition.Assembly.FindAttribute(LogCallAttributeName);
            if (logCallAttribute != null)
            {
                var includeTypes = logCallAttribute.GetPropertyValue<string>("IncludeTypes") ?? ".*";
                var regex = new Regex(includeTypes);
                definitions.UnionWith(SelectMethods(moduleDefinition, x => regex.IsMatch(x.FullName), x => true));
            }

            return definitions;
        }

        private IEnumerable<MethodDefinition> SelectMethods(ModuleDefinition moduleDefinition,
            Func<TypeDefinition, bool> typeSelector, Func<MethodDefinition, bool> methodSelector)
        {
            return moduleDefinition.Types.Where(x => x.IsClass && typeSelector(x))
                .SelectMany(x => x.Methods)
                .Where(x => (!x.IsSpecialName && !x.IsGetter && !x.IsSetter && !x.IsConstructor &&
                             !x.ContainsAttribute(moduleDefinition.ImportType<CompilerGeneratedAttribute>())) &&
                              methodSelector(x));
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

        private LoggerAccessor FindLogger(MethodDefinition methodDefinition)
        {
            var loggerProperty = methodDefinition.DeclaringType.GetPropertyGet(LoggerInstanceName);
            if (loggerProperty != null)
                return GetLoggerFromProperty(methodDefinition, loggerProperty);

            var loggerFiled = methodDefinition.DeclaringType.GetField(LoggerInstanceName);
            if (loggerFiled != null)
                return GetLoggerFromField(methodDefinition, loggerFiled);

            LogError("Can not find logger instance");
            return null;
        }

        private LoggerAccessor GetLoggerFromField(MethodDefinition methodDefinition, FieldDefinition loggerFiled)
        {
            var fieldTypeDefinition = loggerFiled.FieldType.Resolve();
            var logMethod = FindLogMethod(methodDefinition, fieldTypeDefinition);
            if (logMethod == null)
            {
                LogError("Can not find log method");
                return null;
            }

            return new LoggerAccessor
            {
                LoadLogger = x => x.Add(OpCodes.Ldfld, methodDefinition.Module.Import(loggerFiled)),
                CallLogger = x => x.Add(OpCodes.Callvirt, logMethod),
            };
        }

        private LoggerAccessor GetLoggerFromProperty(MethodDefinition methodDefinition, MethodDefinition loggerProperty)
        {
            var propertyGetReturnTypeDefinition = loggerProperty.ReturnType.Resolve();
            var logMethod = FindLogMethod(methodDefinition, propertyGetReturnTypeDefinition);
            if (logMethod == null)
            {
                LogError("Can not find log method");
                return null;
            }

            return new LoggerAccessor
            {
                LoadLogger = (x) => x.AddCall(loggerProperty),
                CallLogger = (x) => x.Add(OpCodes.Callvirt, logMethod)
            };
        }

        private MethodReference FindLogMethod(MethodDefinition methodDefinition, TypeDefinition loggerType)
        {
            var logMethodName = GetLogMethodName(methodDefinition);
            var logMethod = (loggerType.GetMethod(logMethodName, new[] { loggerType.Module.ImportType<string>() }));
            return methodDefinition.Module.Import(logMethod);
        }

        public IEnumerable<Instruction> WaveMethod(MethodDefinition methodDefinition, ILProcessor processor)
        {
            var loggerAccessor = FindLogger(methodDefinition);
            if (loggerAccessor == null)
            {
                return Enumerable.Empty<Instruction>();
            }

            var builder = new MethodBuilder(methodDefinition, processor);
            var objectArrayVar = builder.AddVariable<object[]>();
            builder.Add(OpCodes.Nop)
                .Add(OpCodes.Ldarg_0) //todo: not needed when Logger is static 
                .Apply(loggerAccessor.LoadLogger)
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

            builder.Apply(loggerAccessor.CallLogger);

            return builder.Instructions;
        }

        private static string GetLogMethodName(MethodDefinition methodDefinition)
        {
            var logCallAttribute = methodDefinition.FindAttribute(LogCallAttributeName) ??
                                   methodDefinition.DeclaringType.FindAttribute(LogCallAttributeName);

            if (logCallAttribute != null)
            {
                return logCallAttribute.GetPropertyValue<string>("LogAs") ?? DefaultLogMethod;
            }
            return DefaultLogMethod;
        }

        private string FormatMethodCall(MethodDefinition methodDefinition)
        {
            if (methodDefinition.Parameters.Count == 0)
                return String.Format("Called {0}", methodDefinition.Name);
            else
                return String.Format("Called {0} with {1}", methodDefinition.Name, FormatMethodParameters(methodDefinition));
        }

        private string FormatMethodParameters(MethodDefinition methodDefinition)
        {
            return string.Join(", ",
                methodDefinition.Parameters.Select((x, i) => string.Format("{0}={{{1}}}", x.Name, i)));
        }

        private class LoggerAccessor
        {
            public Func<MethodBuilder, MethodBuilder> LoadLogger;
            public Func<MethodBuilder, MethodBuilder> CallLogger;
        }
    }
}
