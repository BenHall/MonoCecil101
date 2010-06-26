﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            
            DisableEventsDuringExecution(() =>
                                             {
                                                 changedMethods = parser.GetChangedMethods(e);
                                             });

            foreach (var changedMethod in changedMethods.Distinct())
            {
                Console.WriteLine(changedMethod.Status + "\t" + changedMethod);
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


            Console.WriteLine("Watching...");
            watcher.EnableRaisingEvents = true;
        }
    }
}