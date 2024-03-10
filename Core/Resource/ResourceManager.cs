#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Resource
{
    /// <summary>
    /// File handler and GPU resource generator. Should probably be split into multiple classes, but for now it is a
    /// multi-file partial class.
    /// Todo: reduce/remove dependency on ResourceFileWatcher to resolve paths - this should happen the other way around
    /// </summary>
    public sealed partial class ResourceManager
    {
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();
        public static IEnumerable<string> SharedResourceFolders => SharedResourceFileWatchers.Select(x => x.WatchedFolder);

        public static ResourceManager Instance() => _instance;
        private static readonly ResourceManager _instance = new();

        static ResourceManager()
        {
        }

        private static bool TryGetResourcePath(Instance? instance, string relativePath, out string path,
                                               out ResourceFileWatcher? relevantFileWatcher)
        {
            // keep backwards compatibility
            if (Path.IsPathRooted(relativePath))
            {
                Log.Warning($"Absolute paths for resources are deprecated, live updating will not occur. " +
                            $"Please use paths relative to your project or a shared resource folder instead: {relativePath}");
                path = relativePath;
                relevantFileWatcher = null;
                return File.Exists(relativePath);
            }

            // then just resolve correctly
            if (TryResolvePath(relativePath, instance, out var absolutePath, out var watcher))
            {
                if (File.Exists(absolutePath))
                {
                    path = absolutePath;
                    relevantFileWatcher = watcher;
                    return true;
                }
            }

            // prioritize project-local resources
            var relativePathBackwardsCompatibility = RelativePathBackwardsCompatibility(relativePath, false);
            if (TryResolvePath(relativePathBackwardsCompatibility, instance, out var absolutePathCompat, out var watcherCompat))
            {
                if (File.Exists(absolutePathCompat))
                {
                    path = absolutePathCompat;
                    relevantFileWatcher = watcherCompat;
                    return true;
                }
            }

            path = relativePath;
            relevantFileWatcher = null;
            return false;
        }

        private static string RelativePathBackwardsCompatibility(string relativePath, bool checkRoot = true)
        {
            if (checkRoot && Path.IsPathRooted(relativePath))
            {
                Log.Warning($"Path '{relativePath}' is not relative. This is deprecated and should be relative to the project Resources folder as " +
                            $"live updates will not occur. Please update your project settings.");
                return relativePath;
            }

            const string resourcesSubfolder = "resources";
            if (relativePath.StartsWith(resourcesSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath[(resourcesSubfolder.Length + 1)..];
            }

            const string userSubfolder = "user";
            if (relativePath.StartsWith(userSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                // remove the user subfolder
                var pathWithoutUser = relativePath.AsSpan()[(userSubfolder.Length + 1)..];

                // try to remove the user's name
                var backslashIndex = pathWithoutUser.IndexOf('/');
                var forwardSlashIndex = pathWithoutUser.IndexOf('\\');

                if (backslashIndex == -1)
                    backslashIndex = int.MaxValue;

                if (forwardSlashIndex == -1)
                    forwardSlashIndex = int.MaxValue;

                var slashIndex = Math.Min(backslashIndex, forwardSlashIndex);

                if (slashIndex == int.MaxValue)
                    slashIndex = 0;
                else
                    slashIndex += 1;

                return pathWithoutUser[slashIndex..].ToString();
            }

            const string commonSubfolder = "common";
            if (relativePath.StartsWith(commonSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                return relativePath[(commonSubfolder.Length + 1)..];
            }

            const string libSubfolder = "lib";
            if (relativePath.StartsWith(libSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                return relativePath[(libSubfolder.Length + 1)..];
            }

            return relativePath;
        }

        private static bool TryResolvePath(string relativeFileName, Instance? instance, out string absolutePath, out ResourceFileWatcher? relevantFileWatcher)
        {
            var parent = instance;
            while (parent != null)
            {
                if (TryResolvePath(relativeFileName, parent, out absolutePath, false))
                {
                    relevantFileWatcher = parent.ResourceFileWatcher;
                    return true;
                }

                parent = parent.Parent;
            }
            
            return CheckSharedResources(relativeFileName, out absolutePath, out relevantFileWatcher);
        }

        public static bool TryResolvePath(string relativeFileName, Instance instance, out string absolutePath, bool checkSharedResources = true)
        {
            relativeFileName = RelativePathBackwardsCompatibility(relativeFileName);

            if (TryResolvePath(relativeFileName, out absolutePath, instance.ResourceFolders, checkSharedResources))
            {
                return true;
            }


            if(checkSharedResources)
                return CheckSharedResources(relativeFileName, out absolutePath, out _);
            
            absolutePath = relativeFileName;
            return false;
        }

        public static bool TryResolvePath(string relativeFileName, out string absolutePath, IEnumerable<string> directories, bool checkSharedResources = true)
        {
            relativeFileName = RelativePathBackwardsCompatibility(relativeFileName);

            foreach (var directory in directories)
            {
                absolutePath = Path.Combine(directory, relativeFileName);
                if (File.Exists(absolutePath))
                    return true;
            }
            
            if(checkSharedResources)
                return CheckSharedResources(relativeFileName, out absolutePath, out _);
            
            absolutePath = relativeFileName;
            return false;
        }

        public static bool TryResolvePath(string relativeFileName, out string absolutePath, string? directory)
        {
            relativeFileName = RelativePathBackwardsCompatibility(relativeFileName);

            if (directory != null)
            {
                absolutePath = Path.Combine(directory, relativeFileName);
                if (File.Exists(absolutePath))
                    return true;
            }

            return CheckSharedResources(relativeFileName, out absolutePath, out _);
        }

        private static bool CheckSharedResources(string relativeFileName, out string path, out ResourceFileWatcher? relevantFileWatcher)
        {
            foreach (var fileWatcher in SharedResourceFileWatchers)
            {
                path = Path.Combine(fileWatcher.WatchedFolder, relativeFileName);
                if (File.Exists(path))
                {
                    relevantFileWatcher = fileWatcher;
                    return true;
                }
            }
            
            path = string.Empty;
            relevantFileWatcher = null;
            return false;
        }

        public static bool TryResolveDirectory(string relativeDirectory, Instance parent, out string absoluteDirectory)
        {
            relativeDirectory = RelativePathBackwardsCompatibility(relativeDirectory);
            
            foreach(var parentResourceFolder in parent.ResourceFolders)
            {
                var directory = Path.Combine(parentResourceFolder, relativeDirectory);
                if(Directory.Exists(directory))
                {
                    absoluteDirectory = directory;
                    return true;
                }
            }
            
            absoluteDirectory = string.Empty;
            
            foreach(var fileWatcher in SharedResourceFileWatchers)
            {
                var sharedResourceFolder = fileWatcher.WatchedFolder;
                var directory = Path.Combine(sharedResourceFolder, relativeDirectory);
                if(Directory.Exists(directory))
                {
                    absoluteDirectory = directory;
                    return true;
                }
            }
            
            return false;
        }

        public static string CleanRelativePath(string relativePath) => RelativePathBackwardsCompatibility(relativePath, true);
        

        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private uint _resourceIdCounter = 1;
        internal static readonly List<ResourceFileWatcher> SharedResourceFileWatchers = new(4);
    }
}