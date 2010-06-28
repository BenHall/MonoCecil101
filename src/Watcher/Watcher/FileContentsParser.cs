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
                contents.Add(new FileContents { Path = file, Contents = ReadFileContents(file) });
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
            for (int index = 0; index < originalContents.Contents.Length; index++)
            {
                if (updatedContents.Length == index)
                    break;

                var updatedContent = updatedContents[index];
                var originalContent = originalContents.Contents[index];

                if (updatedContent != originalContent)
                {
                    var changedContents = GetNamespaceClassMethodOfChangedLine(index, updatedContents);
                    if (changedContents != null)
                    {
                        changedContents.Status = ChangedContentStatus.Updated;
                        list.Add(changedContents);
                    }
                }
            }

            if (updatedContents.Length > originalContents.Contents.Length)
                list.AddRange(FindNewMethods(updatedContents, originalContents.Contents.Length));

            if (originalContents.Contents.Length > updatedContents.Length)
                list.AddRange(FindDeletedMethods(updatedContents, originalContents));

            return list;
        }

        private List<ChangedMethod> FindDeletedMethods(string[] updatedContents, FileContents originalContents)
        {
            List<ChangedMethod> originalMethods = new List<ChangedMethod>();
            for (int i = 0; i < originalContents.Contents.Length; i++)
            {
                string originalLine = originalContents.Contents[i].Trim();

                if (IsLineMethodDefinition(originalLine))
                {
                    ChangedMethod changedLine = GetNamespaceClassMethodOfChangedLine(i, originalContents.Contents);
                    changedLine.Status = ChangedContentStatus.Delete;
                    originalMethods.Add(changedLine);
                }
            }

            for (int i = 0; i < updatedContents.Length; i++)
            {
                string updatedLine = updatedContents[i].Trim();

                if (IsLineMethodDefinition(updatedLine))
                {
                    var definitionPart = GetNamespaceClassMethodOfChangedLine(i, updatedContents);

                    if (originalMethods.Contains(definitionPart))
                        originalMethods.Remove(definitionPart);
                }
            }

            return originalMethods;
        }

        private IEnumerable<ChangedMethod> FindNewMethods(string[] updatedContents, int startOfNewContent)
        {
            int newLines = updatedContents.Length - startOfNewContent;
            List<ChangedMethod> list = new List<ChangedMethod>();

            for (int i = 0; i < newLines; i++)
            {
                var changedContents = GetNamespaceClassMethodOfChangedLine(i, updatedContents);
                if (changedContents != null)
                {
                    changedContents.Status = ChangedContentStatus.New;
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
            string[] updatedContents = ReadFileContents(e.FullPath);
            return updatedContents;
        }

        private string[] ReadFileContents(string path)
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
                return ReadFileContents(path);
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

        private string FindPart(string[] contents, int lineOfChange, Func<string, bool> lineMatcher)
        {
            if (lineOfChange == 0)
                return String.Empty;

            string currentLine = contents[lineOfChange].Trim();
            if (lineMatcher(currentLine))
                return GetDefinitionPart(currentLine);

            return FindPart(contents, lineOfChange - 1, lineMatcher);
        }

        private string GetDefinitionPart(string currentLine)
        {
            foreach (var s in currentLine.Split('('))
            {
                string[] definition = s.Split(' ');
                return definition.Last();
            }

            return string.Empty;
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
            return (currentLine.StartsWith("public ") || currentLine.StartsWith("private ") || currentLine.StartsWith("protected ") || currentLine.StartsWith("internal "))
                    && !IsLineClassDefinition(currentLine);
        }
    }

    public class ChangedMethod
    {
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public string NamespaceName { get; set; }
        public ChangedContentStatus Status { get; set; }

        public bool FoundAllParts()
        {
            return !(string.IsNullOrEmpty(MethodName) || string.IsNullOrEmpty(ClassName) || string.IsNullOrEmpty(NamespaceName));
        }

        public override string ToString()
        {
            return String.Format("{0}.{1}.{2}", NamespaceName, ClassName, MethodName);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ToString().Equals(obj.ToString());
        }
    }

    public enum ChangedContentStatus
    {
        New,
        Delete,
        Updated
    }
}