using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using NUnit.Framework;
using Watcher.MonoCecil;

namespace Watcher
{
    class CodeChangingWatcher
    {
        static FileSystemWatcher watcher;
        static FileContentsParser parser;

        private static void fileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("File renamed " + e.OldName + " > " + e.Name);
        }

        static void fileChanged(object sender, FileSystemEventArgs e)
        {
            IEnumerable<ChangedMethod> changedMethods = null;
            
            DisableEventsDuringExecution(() => {
                                                 changedMethods = parser.GetChangedMethods(e);
                                             });

            Console.WriteLine("The following methods have changed:");
            foreach (var changedMethod in changedMethods.Distinct())
            {
                Console.WriteLine("\t" + changedMethod.Status + "\t" + changedMethod);
            }

            UnitTestFinder finder = new UnitTestFinder();
            IEnumerable<UnitTest> tests = finder.FindUnitTestsAffectedByChangedMethods(changedMethods);

            Console.WriteLine("Unit tests required to be executed:");
            foreach (var unitTest in tests)
            {
                Console.WriteLine("\t" + unitTest);
            }

            RebuildAssembly();
            ExecuteUnitTests(tests);
        }

        private static void RebuildAssembly()
        {
            Console.WriteLine("Rebuilding...");
            string sln = @"D:\SourceControl\MonoCecil101\example\UnitTesting1\UnitTesting1.sln";
            string cmd = @"C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo(cmd, sln);
            startInfo.CreateNoWindow = false;
            var process = Process.Start(startInfo);
            process.WaitForExit(1000);
            Console.WriteLine("done rebuilding...");
        }

        private static void ExecuteUnitTests(IEnumerable<UnitTest> tests)
        {
            foreach (var unitTest in tests)
            {
                Console.ResetColor();
                try
                {
                    var assembly = Assembly.LoadFrom(unitTest.AssemblyPath);
                    var type = assembly.GetType(unitTest.GetTypeName());
                    MethodInfo methodInfo = type.GetMethod(unitTest.MethodName);
                    object o = Activator.CreateInstance(type);
                    methodInfo.Invoke(o, null);
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("TEST FAILED!!");
                    Console.WriteLine(ex.InnerException.Message);
                }
            }
        }

        private static void DisableEventsDuringExecution(Action action)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                action();
            }

            finally
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        public void Watch(string path)
        {
            parser = new FileContentsParser(path);

            watcher = new FileSystemWatcher(path);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.cs";

            watcher.Changed += fileChanged;
            watcher.Deleted += fileChanged;
            watcher.Renamed += fileRenamed;


            Console.WriteLine("Started...");
            watcher.EnableRaisingEvents = true;
        }
    }
}