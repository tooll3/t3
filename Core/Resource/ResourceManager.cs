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

        public static bool TryResolvePath(string relativePath, IReadOnlyCollection<string> directories, out string absolutePath,
                                          bool checkSharedResources = true, string? backCompat = null)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                return false;
            }
            
            bool changedForCompatibility;
            bool isAbsolute;
            
            if (backCompat == null)
            {
                backCompat = RelativePathBackwardsCompatibility(relativePath, out isAbsolute, out changedForCompatibility);
            }
            else
            {
                isAbsolute = Path.IsPathRooted(relativePath);
                changedForCompatibility = !string.IsNullOrWhiteSpace(backCompat) && backCompat != relativePath;
            }

            if (isAbsolute)
            {
                absolutePath = relativePath;
                return Exists(absolutePath);
            }

            foreach (var directory in directories)
            {
                absolutePath = Path.Combine(directory, relativePath);
                if (Exists(absolutePath))
                    return true;
                
                absolutePath = Path.Combine(directory, backCompat);
                if (Exists(absolutePath))
                    return true;
            }

            if (checkSharedResources)
            {
                if (CheckSharedResources(relativePath, out absolutePath, out _))
                {
                    return true;
                }

                if (changedForCompatibility && CheckSharedResources(backCompat, out absolutePath, out _))
                {
                    return true;
                }
            }
            
            absolutePath = string.Empty;
            return false;
        }
        
        private static bool Exists(string absolutePath) => File.Exists(absolutePath) || Directory.Exists(absolutePath);

        public static string CleanRelativePath(string relativePath) => RelativePathBackwardsCompatibility(relativePath, out _, out _);
        

        private static string RelativePathBackwardsCompatibility(string relativePath, out bool isAbsolute, out bool changedForCompatibility)
        {
            changedForCompatibility = false;
            
            if (Path.IsPathRooted(relativePath))
            {
                Log.Warning($"Path '{relativePath}' is not relative. This is deprecated and should be relative to the project Resources folder as " +
                            $"live updates will not occur. Please update your project settings.");
                isAbsolute = true;
                return relativePath;
            }

            const string resourcesSubfolder = "resources";
            isAbsolute = false;
            if (relativePath.StartsWith(resourcesSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath[(resourcesSubfolder.Length + 1)..];
                changedForCompatibility = true;
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

                changedForCompatibility = true;
                return pathWithoutUser[slashIndex..].ToString();
            }

            const string commonSubfolder = "common";
            if (relativePath.StartsWith(commonSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                changedForCompatibility = true;
                return relativePath[(commonSubfolder.Length + 1)..];
            }

            const string libSubfolder = "lib";
            if (relativePath.StartsWith(libSubfolder, StringComparison.OrdinalIgnoreCase))
            {
                changedForCompatibility = true;
                return relativePath[(libSubfolder.Length + 1)..];
            }

            return relativePath;
        }

        internal static bool TryResolvePath(string relativePath, Instance? instance, out string absolutePath, out ResourceFileWatcher? relevantFileWatcher)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                relevantFileWatcher = null;
                return false;
            }
            
            var backCompat = RelativePathBackwardsCompatibility(relativePath, out var isAbsolute, out var changedForCompatibility);
            if (isAbsolute)
            {
                absolutePath = relativePath;
                relevantFileWatcher = null;
                return Exists(absolutePath);
            }


            if (instance != null)
            {
                foreach (var package in instance.AvailableResourcePackages)
                {
                    var resourcesFolder = package.ResourcesFolder;
                    var path = Path.Combine(resourcesFolder, relativePath);

                    bool found = Exists(path);

                    if (!found && changedForCompatibility)
                    {
                        path = Path.Combine(resourcesFolder, backCompat);
                        found = Exists(path);
                    }

                    if (found)
                    {
                        absolutePath = path;
                        relevantFileWatcher = package.ResourceFileWatcher;
                        return true;
                    }
                }
            }

            if (CheckSharedResources(relativePath, out absolutePath, out relevantFileWatcher))
            {
                return true;
            }
            
            if (changedForCompatibility && CheckSharedResources(backCompat, out absolutePath, out relevantFileWatcher))
            {
                return true;
            }
            
            absolutePath = string.Empty;
            relevantFileWatcher = null;
            return false;
        }

        private static bool CheckSharedResources(string relativePath, out string absolutePath, out ResourceFileWatcher? relevantFileWatcher)
        {
            foreach (var fileWatcher in SharedResourceFileWatchers)
            {
                absolutePath = Path.Combine(fileWatcher.WatchedFolder, relativePath);
                if (Exists(absolutePath))
                {
                    relevantFileWatcher = fileWatcher;
                    return true;
                }
            }
            
            absolutePath = string.Empty;
            relevantFileWatcher = null;
            return false;
        }


        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private uint _resourceIdCounter = 1;
        internal static readonly List<ResourceFileWatcher> SharedResourceFileWatchers = new(4);
    }
}