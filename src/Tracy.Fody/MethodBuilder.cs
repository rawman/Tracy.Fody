using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tracy.Fody
{
    class MethodBuilder
    {
        private readonly MethodDefinition _methodDefinition;
        private readonly ILProcessor _processor;
        private readonly List<Instruction> _instructions; 
        public MethodBuilder(MethodDefinition methodDefinition, ILProcessor processor)
        {
            _methodDefinition = methodDefinition;
            _processor = processor;

            _instructions = new List<Instruction>();
        }

        public MethodBuilder Add(OpCode code, MethodReference methodReference)
        {
            return Add(_processor.Create(code, methodReference));
        }

        public MethodBuilder Add(OpCode code, TypeReference typeReference)
        {
            return Add(_processor.Create(code, typeReference));
        }

        public MethodBuilder Add(OpCode code, FieldReference fieldReference)
        {
            return Add(_processor.Create(code, fieldReference));
        }

        public MethodBuilder Add(OpCode code)
        {
            return Add(_processor.Create(code));
        }

        public MethodBuilder Add(OpCode code, int value)
        {
            return Add(_processor.Create(code, value));
        }

        public MethodBuilder Add(OpCode code, string value)
        {
            return Add(_processor.Create(code, value));
        }

        public MethodBuilder Add(params Instruction[] instruction)
        {
            _instructions.AddRange(instruction);
            return this; 
        }

        public IEnumerable<Instruction> Instructions
        {
            get { return _instructions; }
        }

        public MethodBuilder AddCall<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> m)
        {
            var lambda = m as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("method");


            if (lambda.Body.NodeType != ExpressionType.Call)
                throw new ArgumentException("Expression is not a method call");
         
            var methodCall = (MethodCallExpression)lambda.Body;
            var methodInfo = methodCall.Method;
            var parametersTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodRef = _methodDefinition.Module.ImportMethod(methodInfo.DeclaringType, methodInfo.Name, parametersTypes);
            _instructions.Add(_processor.Create(OpCodes.Call, methodRef));
            return this;
        }

        public MethodBuilder AddCall(MethodDefinition methodDefinition)
        {
            var methodRef = methodDefinition.Module.Import(methodDefinition);
            _instructions.Add(_processor.Create(OpCodes.Call, methodRef));
            return this;
        }

        public int AddVariable<T>()
        {
            return _methodDefinition.AddVariable<T>();
        }

        public MethodBuilder Apply(Func<MethodBuilder, MethodBuilder> add)
        {
            return add(this);
        }
    }
}
