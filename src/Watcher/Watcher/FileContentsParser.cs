using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Watcher
{
    class FileContentsParser
    {
        static List<FileContents> files;

        public FileContentsParser(string path)
        {
            files = GetFileContents(path);
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

        public string GetChangedMethod(FileSystemEventArgs e)
        {
            string[] updatedContents = File.ReadAllLines(e.FullPath);
            FileContents originalContents = files.SingleOrDefault(x => x.Path == e.FullPath);

            for (int index = 0; index < updatedContents.Length; index++)
            {
                var updatedContent = updatedContents[index];
                var originalContent = originalContents.Contents[index];
                if (updatedContent != originalContent)
                {
                    string methodName = FindName(updatedContents, index, IsLineMethodDefinition);
                    string className = FindName(updatedContents, index, IsLineClassDefinition);
                    string namespaceName = FindName(updatedContents, index, IsLineNamespaceDefinition);

                    return String.Format("{0}.{1}.{2}", namespaceName, className, methodName);
                }
            }

            return String.Empty;
        }

        private string FindName(string[] updatedContents, int lineOfChange, Func<string, bool> lineMatcher)
        {
            if (lineOfChange == 0)
                return String.Empty;

            string currentLine = updatedContents[lineOfChange].Trim();
            if (lineMatcher(currentLine))
            {
                foreach (var s in currentLine.Split('('))
                {
                    string[] definition = s.Split(' ');
                    return definition.Last();
                }
            }
            
            return FindName(updatedContents, lineOfChange - 1, lineMatcher);
        }

        private bool IsLineNamespaceDefinition(string currentLine)
        {
            return currentLine.StartsWith("namespace");
        }

        private bool IsLineClassDefinition(string currentLine)
        {
            return currentLine.Contains(" class ");
        }

        private bool IsLineMethodDefinition(string currentLine)
        {
            return currentLine.StartsWith("public ") || currentLine.StartsWith("private ") || currentLine.StartsWith("protected ") || currentLine.StartsWith("internal ");
        }
    }
}