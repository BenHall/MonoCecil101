using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Watcher
{
    class Program
    {
        static List<FileContents> files;

        static void Main(string[] args)
        {
            string path = @"D:\SourceControl\MonoCecil101\example\UnitTesting1";

            files = GetFileContents(path);

            FileSystemWatcher watcher = new FileSystemWatcher(path);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite| NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.cs";

            watcher.Changed += fileChanged;
            watcher.Deleted += fileChanged;
            watcher.Created += fileChanged;
            watcher.Renamed += fileRenamed;


            Console.WriteLine("Watching...");
            watcher.EnableRaisingEvents = true;

            Console.Read();
        }

        private static List<FileContents> GetFileContents(string path)
        {
            List<FileContents> contents = new List<FileContents>();
            foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                contents.Add(new FileContents { Path = file, Contents = File.ReadAllLines(file) });
            }

            return contents;
        }

        private static void fileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("File renamed " + e.OldName + " > " + e.Name);
        } 

        static void fileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File changed " + e.Name);
            string[] updatedContents = File.ReadAllLines(e.FullPath);
            FileContents originalContents = files.SingleOrDefault(x => x.Path == e.FullPath);

            for (int index = 0; index < updatedContents.Length; index++)
            {
                var updatedContent = updatedContents[index];
                var originalContent = originalContents.Contents[index];
                if (updatedContent != originalContent)
                {
                    Console.WriteLine("Changed line: " + originalContent + " \r\n\t " + updatedContent);
                    string name = FindMethodName(updatedContents, index);
                    Console.WriteLine(name);
                }
            }
        }

        private static string FindMethodName(string[] updatedContents, int lineOfChange)
        {
            if (lineOfChange == 0)
                return string.Empty;

            string currentLine = updatedContents[lineOfChange].Trim();
            if (currentLine.StartsWith("public") || currentLine.StartsWith("private") || currentLine.StartsWith("protected") || currentLine.StartsWith("internal"))
            {
                foreach (var s in currentLine.Split('('))
                {
                    string[] methohDefParts = s.Split(' ');
                    return methohDefParts.Last();
                }
            }
            
            return FindMethodName(updatedContents, lineOfChange - 1);
        }
    }

    class FileContents
    {
        public string Path { get; set; }
        public string[] Contents { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
