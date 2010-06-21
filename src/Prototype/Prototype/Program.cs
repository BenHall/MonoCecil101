using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Prototype
{
    class Program
    {
        static void Main(string[] args)
        {
            List<InstructionCall> instructionsToInspect = new List<InstructionCall>();

            ModuleDefinition testAssembly = ModuleDefinition.ReadModule(@"D:\SourceControl\MonoCecil101\example\UnitTesting1\UnitTesting1.Tests\bin\Debug\UnitTesting1.Tests.dll");

            foreach (var type in testAssembly.Types)
            {
                Console.WriteLine("Processing... " + type.Name);
                foreach (var method in type.Methods)
                {
                    Console.WriteLine("\tProcessing... " + method.Name);
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode.FlowControl == FlowControl.Call && instruction.OpCode.Code == Code.Callvirt)
                        {
                            
                            Console.WriteLine("\t\tProcessing... " + instruction.OpCode.Name + " " + instruction.OpCode);
                            MemberReference operand = instruction.Operand as MemberReference;
                            InstructionCall instructionCall = GetCall(operand);
                            InstructionCall single = instructionsToInspect.SingleOrDefault(x => x.Equals(instructionCall));

                            if (single == null)
                            {
                                instructionsToInspect.Add(instructionCall);
                                instructionCall.UsedInTests.Add(method.Name);
                            }
                            else
                            {
                                single.UsedInTests.Add(method.Name);
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine();

            //Want to pull this from assembly references... 
            ModuleDefinition actualAssembly = ModuleDefinition.ReadModule(@"D:\SourceControl\MonoCecil101\example\UnitTesting1\UnitTesting1.Tests\bin\Debug\UnitTesting1.dll");

            foreach (var type in actualAssembly.Types)
            {
                Console.WriteLine("Processing... " + type.Name);

                foreach (var method in type.Methods)
                {
                    IEnumerable<InstructionCall> instructionCalls = instructionsToInspect.Where(x => x.Assembly == type.Scope.Name && x.Method == method.Name);
                    foreach (var instructionCall in instructionCalls)
                    {
                        Console.WriteLine("\tCalled method: " + method.Name + " " + instructionCall.Method);
                        Console.WriteLine("\tUsed in the following tests:");
                        foreach (var usedInTest in instructionCall.UsedInTests)
                        {
                            Console.WriteLine("\t\t" + usedInTest);
                        }
                    }
                }
            }

            Console.ReadLine();
        }

        private static InstructionCall GetCall(MemberReference operand)
        {
            var instructionCall = new InstructionCall();
            instructionCall.Assembly = operand.DeclaringType.Scope.Name + ".dll";
            instructionCall.Namespace = operand.DeclaringType.Namespace;
            instructionCall.Class = operand.DeclaringType.Name;
            instructionCall.Method = operand.Name;
            return instructionCall;
        }
    }

    class InstructionCall
    {
        public string Assembly { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public List<string> UsedInTests { get; set; }

        public InstructionCall()
        {
            UsedInTests = new List<string>();
        }
    }
}
