using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using T3.Core.Logging;

namespace T3.Core.Resource
{
    public static class ResourceFileWatcher
    {
        public static void Setup()
        {
            var hlslWatcher = AddWatcher(ResourceManager.ResourcesFolder, "*.hlsl");
            hlslWatcher.Deleted += FileChangedHandler;
            hlslWatcher.Renamed += FileChangedHandler;
            hlslWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime; // Creation time needed for visual studio (2017)

            AddWatcher(ResourceManager.ResourcesFolder, "*.png");
            AddWatcher(ResourceManager.ResourcesFolder, "*.jpg");
            AddWatcher(ResourceManager.ResourcesFolder, "*.dds");
            AddWatcher(ResourceManager.ResourcesFolder, "*.tiff");

            _csFileWatcher = AddWatcher(Core.Model.SymbolData.OperatorTypesFolder,"*.cs");
            _csFileWatcher.Renamed += CsFileRenamedHandler;
            _csFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;

        }
        
        public static void AddFileHook(string filepath, Action action)
        {
            if (string.IsNullOrEmpty(filepath))
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
            
            if (!_fileWatchers.ContainsKey(pattern))
            {
                AddWatcher(ResourceManager.ResourcesFolder, pattern);
            }

            if (HooksForResourceFilepaths.TryGetValue(filepath, out var hook))
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
                HooksForResourceFilepaths.Add(filepath,newHook);
            }
        }
        
        private static FileSystemWatcher AddWatcher(string folder, string filePattern)
        {
            var newWatcher = new FileSystemWatcher(folder, filePattern)
                                 {
                                     IncludeSubdirectories = true,
                                     EnableRaisingEvents = true
                                 };
            newWatcher.Changed += FileChangedHandler;
            newWatcher.Created += FileChangedHandler;
            _fileWatchers.Add(filePattern, newWatcher);
            return newWatcher;
        }


        private static Dictionary<string, FileSystemWatcher> _fileWatchers = new();

        public static void DisableOperatorFileWatcher()
        {
            _csFileWatcher.EnableRaisingEvents = false;
        }

        public static void EnableOperatorFileWatcher()
        {
            _csFileWatcher.EnableRaisingEvents = true;
        }

        private static void FileChangedHandler(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            // Log.Info($"change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
            if (!HooksForResourceFilepaths.TryGetValue(fileSystemEventArgs.FullPath, out var fileHook))
            {
                //Log.Warning("Invalid FileResource?");
                return;
            }

            // Log.Info($"valid change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
            DateTime lastWriteTime = File.GetLastWriteTime(fileSystemEventArgs.FullPath);
            if (lastWriteTime == fileHook.LastWriteReferenceTime)
                return;

            // Log.Info($"very valid change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
            // hack: in order to prevent editors like vs-code still having the file locked after writing to it, this gives these editors 
            //       some time to release the lock. With a locked file Shader.ReadFromFile(...) function will throw an exception, because
            //       it cannot read the file. 
            
            Thread.Sleep(32);
            var ids = string.Join(",", fileHook.ResourceIds);
            Log.Info($"Updating '{fileSystemEventArgs.FullPath}' ({ids} {fileSystemEventArgs.ChangeType})");
            foreach (var id in fileHook.ResourceIds)
            {
                // Update all resources that depend from this file
                if (ResourceManager.ResourcesById.TryGetValue(id, out var resource))
                {
                    var updateable = resource as IUpdateable;
                    updateable?.Update(fileHook.Path);
                }
                else
                {
                    Log.Info($"Trying to update a non existing file resource '{fileHook.Path}'.");
                }
            }

            fileHook.FileChangeAction?.Invoke();

            fileHook.LastWriteReferenceTime = lastWriteTime;

            // else discard the (duplicated) OnChanged event
        }

        private static void CsFileRenamedHandler(object sender, RenamedEventArgs renamedEventArgs)
        {
            ResourceManager.RenameOperatorResource(renamedEventArgs.OldFullPath, renamedEventArgs.FullPath);
        }
        
        private static FileSystemWatcher _csFileWatcher;
        public static readonly Dictionary<string, ResourceFileHook> HooksForResourceFilepaths = new();
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

        public string Path;
        public readonly List<uint> ResourceIds = new();
        public DateTime LastWriteReferenceTime;
        public Action FileChangeAction;
    }
}