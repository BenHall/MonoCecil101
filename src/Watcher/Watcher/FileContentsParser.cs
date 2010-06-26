using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Watcher
{
    class FileContentsParser
    {
        static List<FileContents> files;

        public FileContentsParser(string path)
        {
            files = GetAllFiles(path, "*.cs");
        }

        private List<FileContents> GetAllFiles(string path, string searchPattern)
        {
            List<FileContents> contents = new List<FileContents>();
            foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
            {
                contents.Add(new FileContents { Path = file, Contents = GetFileContents(file) });
            }

            return contents;
        }

        public IEnumerable<ChangedMethod> GetChangedMethods(FileSystemEventArgs e)
        {
            string[] updatedContents = GetUpdatedContents(e);
            FileContents originalContents = GetOriginalContents(e);

            IEnumerable<ChangedMethod> findChangedLines = FindChangedLines(updatedContents, originalContents);
            originalContents.Contents = updatedContents;

            return findChangedLines;
        }

        private IEnumerable<ChangedMethod> FindChangedLines(string[] updatedContents, FileContents originalContents)
        {
            List<ChangedMethod> list = new List<ChangedMethod>();
            for (int index = 0; index < updatedContents.Length; index++)
            {
                var updatedContent = updatedContents[index];
                var originalContent = originalContents.Contents[index];

                if (updatedContent != originalContent)
                {
                    var changedContents = GetNamespaceClassMethodOfChangedLine(index, updatedContents);
                    if (changedContents != null)
                        list.Add(changedContents);
                }
            }

            return list;
        }

        private FileContents GetOriginalContents(FileSystemEventArgs e)
        {
            return files.SingleOrDefault(x => x.Path == e.FullPath);
        }

        private string[] GetUpdatedContents(FileSystemEventArgs e)
        {
            string[] updatedContents = GetFileContents(e.FullPath);
            return updatedContents;
        }

        private string[] GetFileContents(string path)
        {
            try
            {
                StreamReader reader = new StreamReader(path);
                string[] updatedContents = Regex.Split(reader.ReadToEnd(), "\r\n");
                reader.Close();
                return updatedContents;
            }
            catch (Exception)
            {
                return GetFileContents(path);
            }
        }

        private ChangedMethod GetNamespaceClassMethodOfChangedLine(int index, string[] updatedContents)
        {
            ChangedMethod method = new ChangedMethod();
            method.MethodName = FindPart(updatedContents, index, IsLineMethodDefinition);
            method.ClassName = FindPart(updatedContents, index, IsLineClassDefinition);
            method.NamespaceName = FindPart(updatedContents, index, IsLineNamespaceDefinition);

            if(method.FoundAllParts())
                return method;

            return null;
        }

        private string FindPart(string[] updatedContents, int lineOfChange, Func<string, bool> lineMatcher)
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
            
            return FindPart(updatedContents, lineOfChange - 1, lineMatcher);
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

    internal class ChangedMethod
    {
        public override string ToString()
        {
            return String.Format("{0}.{1}.{2}", NamespaceName, ClassName, MethodName);
        }

        public string MethodName { get; set; }

        public string ClassName { get; set; }

        public string NamespaceName { get; set; }

        public bool FoundAllParts()
        {
            return !(string.IsNullOrEmpty(MethodName) || string.IsNullOrEmpty(ClassName) || string.IsNullOrEmpty(NamespaceName));
        }
    }
}