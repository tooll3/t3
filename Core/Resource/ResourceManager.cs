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
using T3.Core.Utils;

namespace T3.Core.Resource
{
    /// <summary>
    /// File handler and GPU resource generator. Should probably be split into multiple classes, but for now it is a
    /// multi-file partial class.
    /// </summary>
    public sealed partial class ResourceManager
    {
        public const string ResourcesSubfolder = "Resources";
        public const char PathSeparator = '/';
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();
        public static IEnumerable<string> SharedResourceFolders => SharedResourcePackages.Select(x => x.ResourcesFolder);

        public static ResourceManager Instance() => _instance;
        private static readonly ResourceManager _instance = new();

        static ResourceManager()
        {
        }

        public static bool TryResolvePath(string relativePath, IEnumerable<IResourcePackage>? resourceContainers, out string absolutePath, out IResourcePackage? resourceContainer, bool isFolder = false)
        {
            var packages = resourceContainers?.ToArray();
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                absolutePath = string.Empty;
                resourceContainer = null;
                return false;
            }
            
            if (relativePath.StartsWith('/'))
            {
                return HandleAlias(relativePath, packages, out absolutePath, out resourceContainer, isFolder);
            }

            RelativePathBackwardsCompatibility(relativePath, out var isAbsolute, out var backCompatRanges);
            if (isAbsolute)
            {
                absolutePath = relativePath;
                resourceContainer = null;
                return Exists(absolutePath, isFolder);
            }

            IReadOnlyList<string>? backCompatPaths = null;

            if (packages != null)
            {
                if (TestPath(relativePath, packages, out absolutePath, out resourceContainer, isFolder))
                    return true;

                backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

                foreach (var backCompatPath in backCompatPaths)
                {
                    if (TestPath(backCompatPath, packages, out absolutePath, out resourceContainer, isFolder))
                        return true;
                }
            }
            
            var sharedResourcePackages = relativePath.EndsWith(".hlsl") ? ShaderPackages : SharedResourcePackages;

            if (TestPath(relativePath, sharedResourcePackages, out absolutePath, out resourceContainer, isFolder))
            {
                return true;
            }

            backCompatPaths ??= PopulateBackCompatPaths(relativePath, backCompatRanges);

            foreach (var backCompatPath in backCompatPaths)
            {
                if (TestPath(backCompatPath, sharedResourcePackages, out absolutePath, out resourceContainer, isFolder))
                    return true;
            }

            absolutePath = string.Empty;
            resourceContainer = null;
            return false;
        }

        private static bool Exists(string absolutePath, bool isFolder) =>  isFolder ? Directory.Exists(absolutePath) : File.Exists(absolutePath);

        private static bool TestPath(string relative, IEnumerable<IResourcePackage> resourceContainers, out string absolutePath,
                             out IResourcePackage? resourceContainer, bool isFolder)
        {
            foreach (var package in resourceContainers)
            {
                var resourcesFolder = package.ResourcesFolder;
                var path = Path.Combine(resourcesFolder, relative);

                if (Exists(path, isFolder))
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

        private static bool HandleAlias(string relative, IEnumerable<IResourcePackage>? resourceContainers, out string absolutePath, out IResourcePackage? resourceContainer, bool isFolder)
        {
            var relativePathAliased = relative.AsSpan(1);
            var aliasEnd = relativePathAliased.IndexOf('/');
            if (aliasEnd == -1)
                aliasEnd = relativePathAliased.IndexOf('\\');

            if (aliasEnd == -1)
            {
                absolutePath = string.Empty;
                resourceContainer = null;
                return false;
            }

            var alias = relativePathAliased[..aliasEnd];
            var relativePathWithoutAlias = relativePathAliased[(aliasEnd + 1)..].ToString();

            if (resourceContainers != null)
            {
                foreach (var container in resourceContainers)
                {
                    if (TestAlias(container, alias, relativePathWithoutAlias, out absolutePath, isFolder))
                    {
                        resourceContainer = container;
                        return true;
                    }
                }
            }
            
            var sharedResourcePackages = relativePathAliased.EndsWith(".hlsl") ? ShaderPackages : SharedResourcePackages;
            foreach (var container in sharedResourcePackages)
            {
                if(TestAlias(container, alias, relativePathWithoutAlias, out absolutePath, isFolder))
                {
                    resourceContainer = container;
                    return true;
                }
            }
            
            absolutePath = string.Empty;
            resourceContainer = null;
            return false;

            static bool TestAlias(IResourcePackage container, ReadOnlySpan<char> alias, string relativePathWithoutAlias, out string absolutePath, bool isFolder)
            {
                var containerAlias = container.Alias;
                if (containerAlias == null)
                {
                    absolutePath = string.Empty;
                    return false;
                }

                if (StringUtils.Equals(containerAlias, alias, true))
                {
                    var path = Path.Combine(container.ResourcesFolder, relativePathWithoutAlias);
                    if (Exists(path, isFolder))
                    {
                        absolutePath = path;
                        return true;
                    }
                }

                absolutePath = string.Empty;
                return false;
            }
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
        public enum PathMode {Absolute, Relative, Aliased}

        public static IEnumerable<string> EnumerateResources(string[] fileExtensionFilter, bool isFolder, IEnumerable<IResourcePackage> packages, PathMode pathMode = PathMode.Relative)
        {
            var filterAcceptsShaders = !isFolder;
            
            // if there's an "all" wildcard, all other filters are irrelevant
            if(fileExtensionFilter.Length == 0 || fileExtensionFilter.Length > 0 && (fileExtensionFilter.Contains("*.*") || fileExtensionFilter.Contains("*")))
            {
                fileExtensionFilter = isFolder ? ["*"] : ["*.*"];
            }

            filterAcceptsShaders = filterAcceptsShaders && fileExtensionFilter.Any(x => x.EndsWith(".hlsl") || x.EndsWith('*'));
            
            var allFiles = packages
                          .Concat(SharedResourcePackages)
                          .Distinct()
                          .SelectMany(x => AllEntriesOf(x, isFolder, pathMode, fileExtensionFilter));

            // handle always-shared shaders
            return !isFolder && filterAcceptsShaders 
                       ? allFiles.Concat(SharedShaderPackages.Except(SharedResourcePackages).SelectMany(x => AllEntriesOf(x, false, pathMode, ".hlsl"))) 
                       : allFiles;
            
            static IEnumerable<string> AllEntriesOf(IResourcePackage package, bool useFolder, PathMode pathMode, params string[] filters)
            {
                Func<string, string, SearchOption, IEnumerable<string>> searchFunc = useFolder
                                                                                     ? Directory.EnumerateDirectories
                                                                                     : Directory.EnumerateFiles;

                var items = filters.SelectMany(searchPattern => searchFunc(package.ResourcesFolder, searchPattern, SearchOption.AllDirectories))
                                   .Select(x => x.Replace('\\', '/'));

                return pathMode switch
                           {
                               PathMode.Absolute => items,
                               PathMode.Relative => items.Select(x => x[(package.ResourcesFolder.Length + 1)..]),
                               PathMode.Aliased  => items.Select(x => $"/{package.Alias}/{x[(package.ResourcesFolder.Length + 1)..]}"),
                               _                 => throw new ArgumentOutOfRangeException(nameof(pathMode), pathMode, null)
                           };
            }
        }
    }

    public static class ResourceExtensions
    {
        public static IEnumerable<IResourcePackage> PackagesInCommon(this IEnumerable<Instance> instances)
        {
            return instances.Select(x => x.AvailableResourcePackages)
                            .Aggregate<IEnumerable<IResourcePackage>>((a, b) => a.Intersect(b));
        }
    }
}