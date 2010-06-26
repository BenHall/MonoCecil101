using System;
using System.IO;

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

            DisableEventsDuringExecution(() =>
                                             {
                                                 string namespaceClassMethod = parser.GetChangedMethod(e);
                                                 Console.WriteLine(namespaceClassMethod);
                                             });
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


            Console.WriteLine("Watching...");
            watcher.EnableRaisingEvents = true;
        }
    }
}