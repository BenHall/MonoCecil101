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
        private static InstructionCall _instructionCall;

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
                            _instructionCall = new InstructionCall();
                            _instructionCall.Assembly = operand.DeclaringType.Scope.Name + ".dll";
                            _instructionCall.Namespace = operand.DeclaringType.Namespace;
                            _instructionCall.Class = operand.DeclaringType.Name;
                            _instructionCall.Method = operand.Name;
                            instructionsToInspect.Add(_instructionCall);
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
                    }
                }
            }

            Console.ReadLine();
        }
    }

    class InstructionCall
    {
        public string Assembly { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
    }
}
