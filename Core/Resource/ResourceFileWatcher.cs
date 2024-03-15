using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SharpDX.Direct3D11;
using T3.Core.Logging;

namespace T3.Core.Resource
{
    public class ResourceFileWatcher
    {
        
        public ResourceFileWatcher(string watchedFolder)
        {
            Directory.CreateDirectory(watchedFolder);
            
            FileSystemEventHandler handler = (_, eventArgs) => FileChangedHandler(eventArgs, HooksForResourceFilePaths);
            RenamedEventHandler renamedHandler = (_, eventArgs) => FileChangedHandler(eventArgs, HooksForResourceFilePaths);
            
            var hlslWatcher = AddWatcher(watchedFolder, "*.hlsl", _fileWatchers, handler);
            hlslWatcher.Deleted += handler;
            hlslWatcher.Renamed += renamedHandler;
            hlslWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime; // Creation time needed for visual studio (2017)

            AddWatcher(watchedFolder, "*.png", _fileWatchers, handler);
            AddWatcher(watchedFolder, "*.jpg", _fileWatchers, handler);
            AddWatcher(watchedFolder, "*.dds", _fileWatchers, handler);
            AddWatcher(watchedFolder, "*.tiff", _fileWatchers, handler);
        }

        // todo - optimize this by making pairing it with the correct watcher that already exists
        public static void AddFileHook(string filepath, Action action)
        {
            if (string.IsNullOrWhiteSpace(filepath))
                return;

            string pattern;
            try
            {
                pattern = "*" + Path.GetExtension(filepath);
            }
            catch
            {
                Log.Warning($"Can't get filepath from source file: {filepath}");
                return;
            }

            if (!ExternalFileWatchers.ContainsKey(pattern))
            {
                FileSystemEventHandler handler = (_, eventArgs) => FileChangedHandler(eventArgs, StaticHooksForResourceFilePaths);
                AddWatcher(Path.GetDirectoryName(filepath), pattern, ExternalFileWatchers, handler);
            }

            if (StaticHooksForResourceFilePaths.TryGetValue(filepath, out var hook))
            {
                hook.FileChangeAction -= action;
                hook.FileChangeAction += action;
            }
            else
            {
                if (!File.Exists(filepath))
                {
                    Log.Warning($"Can't access filepath: {filepath}");
                    return;
                }

                var newHook = new ResourceFileHook(filepath, Array.Empty<uint>())
                                  {
                                      FileChangeAction = action
                                  };
                StaticHooksForResourceFilePaths.TryAdd(filepath, newHook);
            }
        }

        private static FileSystemWatcher AddWatcher(string folder, string filePattern, Dictionary<string, FileSystemWatcher> collection, FileSystemEventHandler handler)
        {
            Directory.CreateDirectory(folder);
            var newWatcher = new FileSystemWatcher(folder, filePattern)
                                 {
                                     IncludeSubdirectories = true,
                                     EnableRaisingEvents = true
                                 };
            newWatcher.Changed += handler;
            newWatcher.Created += handler;
            collection.Add(filePattern, newWatcher);
            return newWatcher;
        }

        private static void FileChangedHandler(FileSystemEventArgs fileSystemEventArgs, ConcurrentDictionary<string, ResourceFileHook> hooks)
        {
            if (!hooks.TryGetValue(fileSystemEventArgs.FullPath, out var fileHook))
            {
                return;
            }

            var lastWriteTime = File.GetLastWriteTime(fileSystemEventArgs.FullPath);
            if (lastWriteTime == fileHook.LastWriteReferenceTime)
                return;

            // hack: in order to prevent editors like vs-code still having the file locked after writing to it, this gives these editors 
            //       some time to release the lock. With a locked file Shader.ReadFromFile(...) function will throw an exception, because
            //       it cannot read the file. 

            Thread.Sleep(32);
            var ids = string.Join(",", fileHook.ResourceIds);
            Log.Info($"Updating '{fileSystemEventArgs.FullPath}' ({ids} {fileSystemEventArgs.ChangeType})");

            fileHook.FileChangeAction?.Invoke();

            fileHook.LastWriteReferenceTime = lastWriteTime;

            // else discard the (duplicated) OnChanged event
        }

        public void Dispose()
        {
            
        }

        internal readonly ConcurrentDictionary<string, ResourceFileHook> HooksForResourceFilePaths = new();
        private static readonly ConcurrentDictionary<string, ResourceFileHook> StaticHooksForResourceFilePaths = new(); 

        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers = new(5);
        private static readonly Dictionary<string, FileSystemWatcher> ExternalFileWatchers = new(5);
    }

    /// <summary>
    /// Used by some <see cref="AbstractResource"/>s to link to a file.
    /// Note that multiple resources likes <see cref="VertexShader"/> and <see cref="PixelShader"/> can
    /// depend on the same source file. 
    /// </summary>
    public class ResourceFileHook
    {
        public ResourceFileHook(string path, IEnumerable<uint> ids)
        {
            Path = path;
            ResourceIds.AddRange(ids);
            LastWriteReferenceTime = File.GetLastWriteTime(path);
        }

        public readonly string Path;
        public readonly List<uint> ResourceIds = new();
        public DateTime LastWriteReferenceTime;
        public Action FileChangeAction;
    }
}