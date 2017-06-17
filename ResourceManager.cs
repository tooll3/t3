using System;
using System.IO;
using System.Reactive.Linq;
using System.Security.Cryptography;

namespace T3Tests
{
    class FileResource
    {
        string FilePath { get; }
        string Hash { get; }
    }

    class ResourceManager
    {
        private readonly FileSystemWatcher _watcher;

        public ResourceManager()
        {
            _watcher = new FileSystemWatcher(".");
            _watcher.NotifyFilter = NotifyFilters.LastWrite;//NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
           // _watcher.Changed += (sender, args) => Console.WriteLine("File: " + args.FullPath + " " + args.ChangeType); ;
           _watcher.Changed += OnChanged;
           // Observable.FromEventPattern<FileSystemEventArgs>(_watcher, "Changed").Select(e => e.EventArgs).Distinct(e => e.FullPath).Subscribe(OnNext);
            _watcher.EnableRaisingEvents = true;
        }

        private DateTime _lastRead = DateTime.MinValue; 
        private void OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(fileSystemEventArgs.FullPath);
            if (lastWriteTime != _lastRead)
            {
                Console.WriteLine("File: " + fileSystemEventArgs.FullPath + " " + fileSystemEventArgs.ChangeType);
                _lastRead = lastWriteTime;
            }
            // else discard the (duplicated) OnChanged event
        }
    }
}
