#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using T3.Core.Logging;

namespace T3.Core.Resource
{
    public sealed partial class ResourceManager
    {
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();

        public static ResourceManager Instance() => _instance;
        private static readonly ResourceManager _instance = new();

        static ResourceManager()
        {
        }

        #region Shaders
        private static bool TryGetResourcePath(ResourceFileWatcher? watcher, string relativePath, out string path,
                                               out ResourceFileWatcher? relevantFileWatcher)
        {
            // keep backwards compatibility
            if (Path.IsPathRooted(relativePath))
            {
                Log.Warning($"Absolute paths for resources are deprecated, live updating will not occur. " +
                            $"Please use paths relative to your project or a shared resource folder instead: {relativePath}");
                path = relativePath;
                relevantFileWatcher = null;
                return true;
            }

            relativePath = RelativePathBackwardsCompatibility(relativePath, false);

            // prioritize project-local resources
            if (watcher != null)
            {
                var firstPath = Path.Combine(watcher.WatchedFolder, relativePath);
                if (File.Exists(firstPath))
                {
                    path = firstPath;
                    relevantFileWatcher = watcher;
                    return true;
                }
            }

            bool found = false;
            path = relativePath;
            relevantFileWatcher = null;

            foreach (var sharedWatcher in SharedResourceFileWatchers)
            {
                var sharedPath = Path.Combine(sharedWatcher.WatchedFolder, relativePath);
                if (!File.Exists(sharedPath))
                    continue;

                path = sharedPath;
                relevantFileWatcher = sharedWatcher;
                found = true;
                break;
            }

            return found;
        }

        private static string RelativePathBackwardsCompatibility(string relativePath, bool checkRoot = true)
        {
            if (checkRoot && Path.IsPathRooted(relativePath))
            {
                Log.Warning($"AudioClip path '{relativePath}' is not relative. This is deprecated and should be relative to the project Resources folder. Please update your project settings.");
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

            const string libSubfolder = "lib";
            if (relativePath.StartsWith(libSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath[(libSubfolder.Length + 1)..];
            }

            return relativePath;
        }

        public static bool TryResolvePath(string relativeFileName, out string absolutePath, IEnumerable<string> directories)
        {
            relativeFileName = RelativePathBackwardsCompatibility(relativeFileName);

            foreach (var directory in directories)
            {
                absolutePath = Path.Combine(directory, relativeFileName);
                if (File.Exists(absolutePath))
                    return true; // warning - this will not warn if the file can be found in multiple directories.
            }

            return CheckSharedResources(relativeFileName, out absolutePath);
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

            return CheckSharedResources(relativeFileName, out absolutePath);
        }

        private static bool CheckSharedResources(string relativeFileName, out string path)
        {
            foreach (var folder in SharedResourceFolders)
            {
                path = Path.Combine(folder, relativeFileName);
                if (File.Exists(path))
                    return true;
            }

            path = string.Empty;
            return false;
        }

        public static bool TryResolveDirectory(string relativeDirectory, out string absoluteDirectory, IEnumerable<string> parentResourceFolders)
        {
            relativeDirectory = RelativePathBackwardsCompatibility(relativeDirectory);
            
            foreach(var parentResourceFolder in parentResourceFolders)
            {
                var directory = Path.Combine(parentResourceFolder, relativeDirectory);
                if(Directory.Exists(directory))
                {
                    absoluteDirectory = directory;
                    return true;
                }
            }
            
            absoluteDirectory = string.Empty;
            
            foreach(var sharedResourceFolder in SharedResourceFolders)
            {
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
        
        #endregion

        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private uint _resourceIdCounter = 1;
        internal static readonly List<ResourceFileWatcher> SharedResourceFileWatchers = new(4);
        public static IEnumerable<string> SharedResourceFolders => SharedResourceFileWatchers.Select(x => x.WatchedFolder);
    }
}