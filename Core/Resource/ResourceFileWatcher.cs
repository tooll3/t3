#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using T3.Core.Logging;

namespace T3.Core.Resource
{
    public class ResourceFileWatcher
    {
        public ResourceFileWatcher(string watchedFolder)
        {
            Directory.CreateDirectory(watchedFolder);
            _watchedDirectory = watchedFolder;
        }

        public void AddFileHook(string filepath, FileWatcherAction action)
        {
            if (string.IsNullOrWhiteSpace(filepath))
                return;

            ArgumentNullException.ThrowIfNull(action);

            if (!filepath.StartsWith(_watchedDirectory))
            {
                Log.Error($"Cannot watch file outside of watched directory: \"{filepath}\" is not in \"{_watchedDirectory}\"");
                return;
            }
            
            if (!_fileChangeActions.TryGetValue(filepath, out var existingActions))
            {
                existingActions = new List<FileWatcherAction>();
                _fileChangeActions.Add(filepath, existingActions);
            }
            
            existingActions.Add(action);
            
            if (_fsWatcher == null)
            {
                _fsWatcher = new(_watchedDirectory)
                                 {
                                     IncludeSubdirectories = true,
                                     EnableRaisingEvents = true
                                 };
                
                _fsWatcher.Changed += OnFileChanged;
                _fsWatcher.Created += OnFileCreated;
                _fsWatcher.Deleted += OnFileDeleted;
                _fsWatcher.Error += OnError;
                _fsWatcher.Renamed += OnFileRenamed;
            }
        }
        
        public void RemoveFileHook(string absolutePath, FileWatcherAction onResourceChanged)
        {
            if (_fileChangeActions.TryGetValue(absolutePath, out var actions))
            {
                actions.Remove(onResourceChanged);
                if (actions.Count == 0)
                {
                    _fileChangeActions.Remove(absolutePath);
                    
                    if (_fileChangeActions.Count == 0)
                    {
                        DisposeFileWatcher(ref _fsWatcher);
                    }
                }
            }
        }
        
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!_fileChangeActions.Remove(e.OldFullPath, out var actions)) 
                return;
            
            var newPath = e.FullPath;
            if (_fileChangeActions.TryGetValue(newPath, out var previousActions))
            {
                previousActions.AddRange(actions);
                return;
            }
            
            _fileChangeActions.Add(newPath, actions); 
        }
        
        private void OnError(object sender, ErrorEventArgs e)
        {
            Log.Error($"File watcher error: {e.GetException()}");
            _fsWatcher?.Dispose();
            _fsWatcher = new FileSystemWatcher(_watchedDirectory)
                             {
                                 IncludeSubdirectories = true,
                                 EnableRaisingEvents = true
                             };
        }
        
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            OnFileChanged(sender, e);
            Log.Warning($"File deleted: {e.FullPath}");
        }
        
        private void Execute(List<FileWatcherAction> actions, WatcherChangeTypes type, string absolutePath)
        {
            // hack: in order to prevent editors like vs-code still having the file locked after writing to it, this gives these editors 
            //       some time to release the lock. With a locked file Shader.ReadFromFile(...) function will throw an exception, because
            //       it cannot read the file. 

            Thread.Sleep(32);
            
            foreach (var action in actions)
            {
                try
                {
                    action(type, absolutePath);
                }
                catch (Exception exception)
                {
                    Log.Error($"Error in file change action: {exception}");
                }
            }
        }
        
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Log.Info($"File created: {e.FullPath}");
            FileCreated?.Invoke(this, e.FullPath);
        }
        
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!_fileChangeActions.TryGetValue(e.FullPath, out var actions))
                return;
            
            Execute(actions, e.ChangeType, e.FullPath);
        }

        public void Dispose()
        {
            DisposeFileWatcher(ref _fsWatcher);
            
            _fileChangeActions.Clear();
        }
        
        private readonly string _watchedDirectory;
        private FileSystemWatcher? _fsWatcher = null;
        
        private readonly Dictionary<string, List<FileWatcherAction>> _fileChangeActions = new();
        public event EventHandler<string>? FileCreated;
        
        private static void DisposeFileWatcher(ref FileSystemWatcher? watcher)
        {
            if (watcher == null)
                return;
            
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;
        }
    }
    
    public delegate void FileWatcherAction(WatcherChangeTypes changeTypes, string absolutePath);
    
    public static class WatcherChangeTypesExtensions
    {
        public static bool WasDeleted(this WatcherChangeTypes changeTypes)
        {
            return (changeTypes & WatcherChangeTypes.Deleted) == WatcherChangeTypes.Deleted;
        }
        
        public static bool WasMoved(this WatcherChangeTypes changeTypes)
        {
            return (changeTypes & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed;
        }
        
        public static bool WasCreated(this WatcherChangeTypes changeTypes)
        {
            return (changeTypes & WatcherChangeTypes.Created) == WatcherChangeTypes.Created;
        }
        
        public static bool WasChanged(this WatcherChangeTypes changeTypes)
        {
            return (changeTypes & WatcherChangeTypes.Changed) == WatcherChangeTypes.Changed;
        }
    }
}