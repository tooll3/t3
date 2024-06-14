#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;

namespace T3.Core.Resource
{
    /// <summary>
    /// File handler and GPU resource generator. Should probably be split into multiple classes, but for now it is a
    /// multi-file partial class.
    /// Todo: rename to `Resources`? for ease of use
    /// </summary>
    public sealed partial class ResourceManager
    {
        public const string ResourcesSubfolder = "Resources";
        public const char PathSeparator = '/';

        public static ResourceManager Instance() => _instance;
        private static readonly ResourceManager _instance = new();

        static ResourceManager()
        {
        }
        
        public static bool TryResolvePath(string relativePath, IResourceConsumer? consumer, out string absolutePath, out IResourcePackage? resourceContainer, bool isFolder = false)
        {
            var packages = consumer?.AvailableResourcePackages.ToArray();
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
        public enum PathMode {Absolute, Relative, Aliased, Raw}
    }
}