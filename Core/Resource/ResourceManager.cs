#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using T3.Core.Logging;
using T3.Core.Model;
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
                                          bool checkSharedResources = true)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                return false;
            }

            RelativePathBackwardsCompatibility(relativePath, out var isAbsolute, out var backCompatRanges);

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
            }

            var backCompatPaths = PopulateBackCompatPaths(relativePath, backCompatRanges);
            foreach (var backCompatPath in backCompatPaths)
            {
                foreach (var directory in directories)
                {
                    absolutePath = Path.Combine(directory, backCompatPath);
                    if (Exists(absolutePath))
                        return true;
                }
            }

            if (checkSharedResources)
            {
                if (CheckSharedResources(relativePath, out absolutePath, out _))
                {
                    return true;
                }

                foreach (var backCompatPath in backCompatPaths)
                {
                    if (CheckSharedResources(backCompatPath, out absolutePath, out _))
                        return true;
                }
            }

            #if DEBUG
            LogFailedResourceLocation(relativePath, null, directories.Concat(SharedResourceFolders));
            #endif

            absolutePath = string.Empty;
            return false;
        }

        internal static bool TryResolvePath(string relativePath, Instance? instance, out string absolutePath, out ResourceFileWatcher? relevantFileWatcher)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                relevantFileWatcher = null;
                return false;
            }

            RelativePathBackwardsCompatibility(relativePath, out var isAbsolute, out var backCompatRanges);
            if (isAbsolute)
            {
                absolutePath = relativePath;
                relevantFileWatcher = null;
                return Exists(absolutePath);
            }

            IReadOnlyList<string>? backCompatPaths = null;

            if (instance != null)
            {
                if (TestPath(relativePath, instance, out absolutePath, out relevantFileWatcher))
                    return true;

                backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

                foreach (var backCompatPath in backCompatPaths)
                {
                    if (TestPath(backCompatPath, instance, out absolutePath, out relevantFileWatcher))
                        return true;
                }
            }

            if (CheckSharedResources(relativePath, out absolutePath, out relevantFileWatcher))
            {
                return true;
            }

            backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

            foreach (var backCompatPath in backCompatPaths)
            {
                if (CheckSharedResources(backCompatPath, out absolutePath, out relevantFileWatcher))
                    return true;
            }

            #if DEBUG
            LogFailedResourceLocation(relativePath, instance, SharedResourceFolders);
            #endif

            absolutePath = string.Empty;
            relevantFileWatcher = null;
            return false;

            static bool TestPath(string relative, Instance instance, out string absolutePath, out ResourceFileWatcher? resourceFileWatcher)
            {
                IReadOnlyList<SymbolPackage> resourcePackages = instance.AvailableResourcePackages;
                foreach (var package in resourcePackages)
                {
                    var resourcesFolder = package.ResourcesFolder;
                    var path = Path.Combine(resourcesFolder, relative);

                    if (Exists(path))
                    {
                        absolutePath = path;
                        resourceFileWatcher = package.ResourceFileWatcher;
                        return true;
                    }
                }

                absolutePath = string.Empty;
                resourceFileWatcher = null;
                return false;
            }
        }

        private static bool Exists(string absolutePath) => File.Exists(absolutePath) || Directory.Exists(absolutePath);


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