namespace Watcher
{
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