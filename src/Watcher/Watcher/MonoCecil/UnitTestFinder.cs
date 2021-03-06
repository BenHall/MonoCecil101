using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Watcher.MonoCecil
{
    public class UnitTestFinder
    {
        public IEnumerable<UnitTest> FindUnitTestsAffectedByChangedMethods(IEnumerable<ChangedMethod> changedMethods)
        {
            var foundUnitTests = new List<UnitTest>();

            IEnumerable<InstructionCall> instructionCalls = Get();
            foreach (var changedMethod in changedMethods)
            {
                var calls = instructionCalls.Where(MatchChangedMethod(changedMethod));

                foundUnitTests.AddRange(calls.SelectMany(instructionCall => instructionCall.UsedInTests));
            }

            return foundUnitTests;
        }

        private Func<InstructionCall, bool> MatchChangedMethod(ChangedMethod changedMethod)
        {
            return x => x.Method == changedMethod.MethodName && x.Class == changedMethod.ClassName && x.Namespace == changedMethod.NamespaceName;
        }

        private IEnumerable<InstructionCall> Get()
        {
            return FindAllMethodsCalledByUnitTests();
        }

        private IEnumerable<InstructionCall> FindAllMethodsCalledByUnitTests()
        {
            List<InstructionCall> instructionsExecuted = new List<InstructionCall>();

            var assembly = @"D:\SourceControl\MonoCecil101\example\UnitTesting1\UnitTesting1.Tests\bin\Debug\UnitTesting1.Tests.dll";
            ModuleDefinition testAssembly = ModuleDefinition.ReadModule(assembly);

            foreach (var type in testAssembly.Types)
            {
                foreach (var method in type.Methods)
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (!IsMethodCall(instruction))
                            continue;

                        InstructionCall instructionCall = GetInstructionCall(instruction.Operand as MemberReference);
                        InstructionCall existingInstruction = instructionsExecuted.SingleOrDefault(x => x.Equals(instructionCall));

                        UnitTest test = new UnitTest
                                            {
                                                AssemblyPath = assembly,
                                                MethodName = method.Name,
                                                ClassName = type.Name,
                                                NamespaceName = type.Namespace
                                            };

                        if (existingInstruction == null)
                        {
                            instructionCall.UsedInTests.Add(test);
                            instructionsExecuted.Add(instructionCall);
                        }
                        else
                        {
                            existingInstruction.UsedInTests.Add(test);
                        }
                    }
                }
            }
            return instructionsExecuted;
        }

        private bool IsMethodCall(Instruction instruction)
        {
            return instruction.OpCode.FlowControl == FlowControl.Call && instruction.OpCode.Code == Code.Callvirt;
        }

        private InstructionCall GetInstructionCall(MemberReference operand)
        {
            if (operand == null) 
                return null;

            var instructionCall = new InstructionCall
                                      {
                                          Assembly = operand.DeclaringType.Scope.Name + ".dll",
                                          Namespace = operand.DeclaringType.Namespace,
                                          Class = operand.DeclaringType.Name,
                                          Method = operand.Name
                                      };
            return instructionCall;
        }
    }

    public class UnitTest
    {
        public string AssemblyPath { get; set; }
        public string NamespaceName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }

        public override string ToString()
        {
            return string.Format("{0}::{1}.{2}.{3}", AssemblyPath, NamespaceName, ClassName, MethodName);
        }

        public string GetTypeName()
        {
            return string.Format("{0}.{1}", NamespaceName, ClassName);
        }
    }

    class InstructionCall
    {
        public string Assembly { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public List<UnitTest> UsedInTests { get; set; }

        public InstructionCall()
        {
            UsedInTests = new List<UnitTest>();
        }
    }
}