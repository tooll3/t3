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
        public const string ResourcesSubfolder = "Resources";
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();
        public static IEnumerable<string> SharedResourceFolders => SharedResourcePackages.Select(x => x.ResourcesFolder);

        public static ResourceManager Instance() => _instance;
        private static readonly ResourceManager _instance = new();

        static ResourceManager()
        {
        }

        public static bool TryResolvePath(string relativePath, IReadOnlyList<IResourcePackage>? resourceContainers, out string absolutePath, out IResourcePackage? resourceContainer)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                resourceContainer = null;
                return false;
            }

            RelativePathBackwardsCompatibility(relativePath, out var isAbsolute, out var backCompatRanges);
            if (isAbsolute)
            {
                absolutePath = relativePath;
                resourceContainer = null;
                return Exists(absolutePath);
            }

            IReadOnlyList<string>? backCompatPaths = null;

            if (resourceContainers != null)
            {
                if (TestPath(relativePath, resourceContainers, out absolutePath, out resourceContainer))
                    return true;

                backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

                foreach (var backCompatPath in backCompatPaths)
                {
                    if (TestPath(backCompatPath, resourceContainers, out absolutePath, out resourceContainer))
                        return true;
                }
            }
            
            var sharedResourcePackages = relativePath.EndsWith(".hlsl") ? ShaderPackages : SharedResourcePackages;

            if (TestPath(relativePath, sharedResourcePackages, out absolutePath, out resourceContainer))
            {
                return true;
            }

            backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

            foreach (var backCompatPath in backCompatPaths)
            {
                if (TestPath(backCompatPath, sharedResourcePackages, out absolutePath, out resourceContainer))
                    return true;
            }

            #if DEBUG
            LogFailedResourceLocation(relativePath, instance, SharedResourceFolders);
            #endif

            absolutePath = string.Empty;
            resourceContainer = null;
            return false;
        }

        private static bool Exists(string absolutePath) => File.Exists(absolutePath) || Directory.Exists(absolutePath);

        static bool TestPath(string relative, IReadOnlyList<IResourcePackage> resourceContainers, out string absolutePath,
                             out IResourcePackage? resourceContainer)
        {
            foreach (var package in resourceContainers)
            {
                var resourcesFolder = package.ResourcesFolder;
                var path = Path.Combine(resourcesFolder, relative);

                if (Exists(path))
                {
                    absolutePath = path;
                    resourceContainer = package;
                    return true;
                }
            }

            absolutePath = string.Empty;
            resourceContainer = null;
            return false;
        }

        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private uint _resourceIdCounter = 1;

        internal static void AddSharedResourceFolder(IResourcePackage resourcePackage, bool allowSharedNonCodeFiles)
        {
            if(allowSharedNonCodeFiles)
                SharedResourcePackages.Add(resourcePackage);
            
            ShaderPackages.Add(resourcePackage);
        }
        
        internal static void RemoveSharedResourceFolder(IResourcePackage resourcePackage)
        {
            ShaderPackages.Remove(resourcePackage);
            SharedResourcePackages.Remove(resourcePackage);
        }
        
        private static readonly List<IResourcePackage> SharedResourcePackages = new(4);
        public static IReadOnlyList<IResourcePackage> SharedShaderPackages => ShaderPackages;
        private static readonly List<IResourcePackage> ShaderPackages = new(4);
    }
}