using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using JeremyAnsel.Media.Dds;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Resource.Dds;
using Device = SharpDX.Direct3D11.Device;

namespace T3.Core.Resource
{
    public interface IUpdateable
    {
        void Update(string path);
    }

    public sealed partial class ResourceManager
    {
        public static readonly ConcurrentDictionary<uint, AbstractResource> ResourcesById = new();
        public static Device Device => _instance._device;
        public static readonly string CommonResourcesFolder;
        
        public static ResourceManager Instance() => _instance;
        private static ResourceManager _instance;

        static ResourceManager()
        {
            CommonResourcesFolder = Path.Combine(RuntimeAssemblies.CoreDirectory, "Resources");
        }

        public static void Init(Device device)
        {
            _instance ??= new ResourceManager();
            _instance.InitializeDevice(device);
        }

        private static void CreateTexture2d(string filename, ref Texture2D texture)
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
        public (uint textureId, uint srvResourceId) CreateTextureFromFile(string filename, Action fileChangeAction)
        {
            if (!File.Exists(filename))
            {
                Log.Warning($"Couldn't find texture '{filename}'.");
                return (NullResource, NullResource);
            }

            if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(filename, out var existingFileResource))
            {
                uint textureId = existingFileResource.ResourceIds.First();
                existingFileResource.FileChangeAction += fileChangeAction;
                uint srvId = (from srvResourceEntry in ShaderResourceViews
                              where srvResourceEntry.TextureId == textureId
                              select srvResourceEntry.Id).Single();
                return (textureId, srvId);
            }

            Texture2D texture = null;
            ShaderResourceView srv = null;
            if (filename.ToLower().EndsWith(".dds"))
            {
                var ddsFile = DdsFile.FromFile(filename);
                DdsDirectX.CreateTexture(ddsFile, Device, Device.ImmediateContext, out var resource, out srv);
                texture = (Texture2D)resource;
            }
            else
            {
                CreateTexture2d(filename, ref texture);
            }

            var fileName = Path.GetFileName(filename);
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

            var fileResource = new ResourceFileHook(filename, new[] { textureResourceEntry.Id, srvResourceId });
            fileResource.FileChangeAction += fileChangeAction;
            ResourceFileWatcher.HooksForResourceFilepaths.TryAdd(filename, fileResource);

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
        public bool TryCreateShaderResourceFromSource<TShader>(out ShaderResource<TShader> resource, string shaderSource, out string errorMessage,
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

        public bool TryCreateShaderResource<TShader>(out ShaderResource<TShader> resource, string fileName, out string errorMessage,
                                                     string name = "", string entryPoint = "main", Action fileChangedAction = null)
            where TShader : class, IDisposable
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                resource = null;
                errorMessage = "Empty file name";
                return false;
            }

            var path = ConstructShaderPath(fileName);
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                resource = null;
                errorMessage = $"File '{path}' doesn't exist";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
                name = fileInfo.Name;

            if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(path, out var fileHook))
            {
                foreach (var id in fileHook.ResourceIds)
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
            var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromFile(srcFile: path,
                                                                                   entryPoint: entryPoint,
                                                                                   name: name,
                                                                                   resourceId: resourceId,
                                                                                   resource: out resource,
                                                                                   errorMessage: out errorMessage);

            if (!compiled)
            {
                Log.Error($"Failed to compile shader '{fileName}'");
                return false;
            }

            ResourcesById.TryAdd(resource.Id, resource);

            // Warning: this may not play nice in a multi-threaded environment
            if (fileHook == null)
            {
                fileHook = new ResourceFileHook(path, new[] { resourceId });
                ResourceFileWatcher.HooksForResourceFilepaths.TryAdd(path, fileHook);
            }

            fileHook.FileChangeAction -= fileChangedAction;
            fileHook.FileChangeAction += fileChangedAction;

            return true;

            static string ConstructShaderPath(string fileName)
            {
                if (Path.IsPathRooted(fileName))
                    return fileName;

                var fileInfo = new FileInfo(fileName!);

                // get the most parent directory and check if it's already the resources folder
                var parentDir = fileInfo.Directory;
                while (parentDir != null && parentDir.Name != CommonResourcesFolder)
                {
                    parentDir = parentDir.Parent;
                }

                if (parentDir == null)
                {
                    // not in resources folder, so prepend it
                    return Path.Combine(CommonResourcesFolder, fileName);
                }

                // already in resources folder
                return fileName;
            }
        }
        #endregion

        private uint GetNextResourceId() => Interlocked.Increment(ref _resourceIdCounter);

        private const uint NullResource = 0;
        private uint _resourceIdCounter = 1;
    }
}