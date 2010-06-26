using System;

namespace Watcher
{
    class Program
    {
        static void Main()
        {
            string path = @"D:\SourceControl\MonoCecil101\example\UnitTesting1";

            CodeChangingWatcher watcher = new CodeChangingWatcher();
            watcher.Watch(path);

            Console.Read();
        }
    }
}