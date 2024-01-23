#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using JeremyAnsel.Media.Dds;
using SharpDX.Direct3D11;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Resource.Dds;
using Device = SharpDX.Direct3D11.Device;

namespace T3.Core.Resource
{
    public sealed partial class ResourceManager
    {
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();
        public static Device Device => _instance._device;

        public static ResourceManager Instance() => _instance;
        private static ResourceManager _instance;

        static ResourceManager()
        {
        }

        public static void Init(Device device)
        {
            _instance ??= new ResourceManager();
            _instance.InitializeDevice(device);
        }

        private static void CreateTexture2d(string filename, ref Texture2D? texture)
        {
            try
            {
                ImagingFactory factory = new ImagingFactory();
                var bitmapDecoder = new BitmapDecoder(factory, filename, DecodeOptions.CacheOnDemand);
                var formatConverter = new FormatConverter(factory);
                var bitmapFrameDecode = bitmapDecoder.GetFrame(0);
                formatConverter.Initialize(bitmapFrameDecode, PixelFormat.Format32bppRGBA, BitmapDitherType.None, null, 0.0, BitmapPaletteType.Custom);

                texture?.Dispose();
                texture = CreateTexture2DFromBitmap(Device, formatConverter);
                string name = Path.GetFileName(filename);
                texture.DebugName = name;
                bitmapFrameDecode.Dispose();
                bitmapDecoder.Dispose();
                formatConverter.Dispose();
                factory.Dispose();
                Log.Info($"Created texture '{name}' from '{filename}'");
            }
            catch (Exception e)
            {
                Log.Info($"Info: couldn't access file '{filename}': {e.Message}.");
            }
        }

        public void CreateUnorderedAccessView(uint textureId, string name, ref UnorderedAccessView unorderedAccessView)
        {
            if (!ResourcesById.TryGetValue(textureId, out var resource))
            {
                Log.Error($"Trying to look up texture resource with id {textureId} but did not found it.");
                return;
            }

            switch (resource)
            {
                case Texture2dResource texture2dResource:
                    unorderedAccessView?.Dispose();
                    unorderedAccessView = new UnorderedAccessView(Device, texture2dResource.Texture) { DebugName = name };
                    Log.Info($"Created unordered resource view '{name}' for texture '{texture2dResource.Name}'.");
                    break;
                case Texture3dResource texture3dResource:
                    unorderedAccessView?.Dispose();
                    unorderedAccessView = new UnorderedAccessView(Device, texture3dResource.Texture) { DebugName = name };
                    Log.Info($"Created unordered resource view '{name}' for texture '{texture3dResource.Name}'.");
                    break;
                default:
                    Log.Error("Trying to generate unordered resource view from a resource that's not a texture resource");
                    break;
            }
        }

        /* TODO, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags, ResourceOptionFlags miscFlags, int loadFlags*/
        public (uint textureId, uint srvResourceId) CreateTextureFromFile(string relativePath, ResourceFileWatcher? watcher, Action? fileChangeAction)
        {
            if (!TryGetResourcePath(watcher, relativePath, out var path, out var relevantFileWatcher))
            {
                // todo - search other packages? common?
                Log.Warning($"Couldn't find texture '{relativePath} (Resolved to '{path}'.");
                return (NullResource, NullResource);
            }

            if (relevantFileWatcher != null && relevantFileWatcher.HooksForResourceFilePaths.TryGetValue(relativePath, out var existingHook))
            {
                uint textureId = existingHook.ResourceIds.First();
                existingHook.FileChangeAction += fileChangeAction;
                uint srvId = (from srvResourceEntry in ShaderResourceViews
                              where srvResourceEntry.TextureId == textureId
                              select srvResourceEntry.Id).Single();
                return (textureId, srvId);
            }

            Texture2D? texture = null;
            ShaderResourceView? srv = null;
            if (path!.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                var ddsFile = DdsFile.FromFile(path);
                DdsDirectX.CreateTexture(ddsFile, Device, Device.ImmediateContext, out var resource, out srv);
                texture = (Texture2D)resource;
            }
            else
            {
                CreateTexture2d(path, ref texture);
            }

            var fileName = Path.GetFileName(path);
            var textureResourceEntry = new Texture2dResource(GetNextResourceId(), fileName, texture);
            ResourcesById.TryAdd(textureResourceEntry.Id, textureResourceEntry);

            uint srvResourceId;
            if (srv == null)
            {
                srvResourceId = CreateShaderResourceView(textureResourceEntry.Id, fileName);
            }
            else
            {
                var textureViewResourceEntry = new ShaderResourceViewResource(GetNextResourceId(), fileName, srv, textureResourceEntry.Id);
                ResourcesById.TryAdd(textureViewResourceEntry.Id, textureViewResourceEntry);
                ShaderResourceViews.Add(textureViewResourceEntry);
                srvResourceId = textureViewResourceEntry.Id;
            }

            if (relevantFileWatcher != null)
            {
                var hook = new ResourceFileHook(relativePath, new[] { textureResourceEntry.Id, srvResourceId });
                hook.FileChangeAction += fileChangeAction;
                relevantFileWatcher.HooksForResourceFilePaths.TryAdd(relativePath, hook);
            }

            return (textureResourceEntry.Id, srvResourceId);
        }

        public static void UpdateTextureFromFile(uint textureId, string path, ref Texture2D texture)
        {
            ResourcesById.TryGetValue(textureId, out var resource);
            if (resource is Texture2dResource textureResource)
            {
                CreateTexture2d(path, ref textureResource.Texture);
                texture = textureResource.Texture;
            }
        }

        // returns true if the texture changed

        #region Shaders
        public bool TryCreateShaderResourceFromSource<TShader>(out ShaderResource<TShader> resource, string shaderSource, IReadOnlyList<string> directories,
                                                               out string errorMessage,
                                                               string name = "", string entryPoint = "main")
            where TShader : class, IDisposable
        {
            var resourceId = GetNextResourceId();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"{typeof(TShader).Name}_{resourceId}";
            }

            var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromSource<TShader>(shaderSource: shaderSource,
                                                                                              name: name,
                                                                                              directory: directories,
                                                                                              entryPoint: entryPoint,
                                                                                              resourceId: resourceId,
                                                                                              resource: out var newResource,
                                                                                              errorMessage: out errorMessage);

            if (compiled)
            {
                ResourcesById.TryAdd(newResource.Id, newResource);
            }
            else
            {
                Log.Error($"Failed to compile shader '{name}'");
            }

            resource = newResource;
            return compiled;
        }

        public bool TryCreateShaderResource<TShader>(out ShaderResource<TShader>? resource, ResourceFileWatcher? watcher, string relativePath,
                                                     out string errorMessage, IEnumerable<string>? resourceFolders,
                                                     string name = "", string entryPoint = "main", Action? fileChangedAction = null)
            where TShader : class, IDisposable
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                resource = null;
                errorMessage = "Empty file name";
                return false;
            }

            if (!TryGetResourcePath(watcher, relativePath, out var path, out var relevantFileWatcher))
            {
                resource = null;
                errorMessage = $"Path not found: '{relativePath}' (Resolved to '{path}').";
                return false;
            }

            var fileInfo = new FileInfo(path!);
            if (string.IsNullOrWhiteSpace(name))
                name = fileInfo.Name;

            ResourceFileHook? fileHook = null;
            var hookExists = relevantFileWatcher != null && relevantFileWatcher.HooksForResourceFilePaths.TryGetValue(relativePath, out fileHook);
            if (hookExists)
            {
                foreach (var id in fileHook!.ResourceIds)
                {
                    var resourceById = ResourcesById[id];
                    if (resourceById is not ShaderResource<TShader> shaderResource || shaderResource.EntryPoint != entryPoint)
                        continue;

                    if (fileChangedAction != null)
                    {
                        fileHook.FileChangeAction -= fileChangedAction;
                        fileHook.FileChangeAction += fileChangedAction;
                    }

                    resource = shaderResource;
                    errorMessage = string.Empty;
                    return true;
                }
            }

            // need to create
            var resourceId = GetNextResourceId();
            List<string> compilationReferences = new();
            
            if(resourceFolders != null)
                compilationReferences.AddRange(resourceFolders);
            else if(relevantFileWatcher != null)
                compilationReferences.Add(relevantFileWatcher.WatchedFolder);
            
            
            
            var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromFile(srcFile: path,
                                                                                   entryPoint: entryPoint,
                                                                                   name: name,
                                                                                   resourceId: resourceId,
                                                                                   resource: out resource,
                                                                                   errorMessage: out errorMessage,
                                                                                   resourceDirs: compilationReferences);

            if (!compiled)
            {
                Log.Error($"Failed to compile shader '{path}'");
                return false;
            }

            ResourcesById.TryAdd(resource.Id, resource);
            if (relevantFileWatcher == null)
                return true;

            if (fileHook == null)
            {
                fileHook = new ResourceFileHook(path, new[] { resourceId });
                relevantFileWatcher.HooksForResourceFilePaths.TryAdd(relativePath, fileHook);
            }

            if (fileChangedAction != null)
            {
                fileHook.FileChangeAction -= fileChangedAction;
                fileHook.FileChangeAction += fileChangedAction;
            }

            return true;
        }

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
                    return true;
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

        public static string CleanRelativePath(string relativePath)
        {
            return RelativePathBackwardsCompatibility(relativePath, true);
        }
        #endregion

        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private const uint NullResource = 0;
        private uint _resourceIdCounter = 1;

        internal static readonly List<ResourceFileWatcher> SharedResourceFileWatchers = new(4);
        public static IEnumerable<string> SharedResourceFolders => SharedResourceFileWatchers.Select(x => x.WatchedFolder);

    }
}