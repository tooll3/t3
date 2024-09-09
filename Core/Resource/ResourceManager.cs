using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Resource.Dds;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantAssignment
// ReSharper disable SuggestVarOrType_SimpleTypes <- Cynic doesn't like it
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable    <- keeping the file handlers as members is clearer

namespace T3.Core.Resource
{
    public interface IUpdateable
    {
        void Update(string path);
    }

    public class ResourceManager
    {
        public Assembly OperatorsAssembly { get; set; }

        public static ResourceManager Instance()
        {
            return _instance;
        }

        public T GetResource<T>(uint resourceId) where T : AbstractResource
        {
            return (T)ResourcesById[resourceId];
        }

        public OperatorResource GetOperatorFileResource(string path)
        {
            bool foundFileEntryForPath = ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(path, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (ResourcesById[id] is OperatorResource opResource)
                        return opResource;
                }
            }

            return null;
        }

        public static void RenameOperatorResource(string oldPath, string newPath)
        {
            var extension = Path.GetExtension(newPath);
            if (extension != ".cs")
            {
                Log.Info($"Ignoring file rename to invalid extension '{extension}' in '{newPath}'.");
                return;
            }

            if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(oldPath, out var fileResource))
            {
                Log.Info($"renamed file resource from '{oldPath}' to '{newPath}'");
                fileResource.Path = newPath;
                ResourceFileWatcher.HooksForResourceFilepaths.Remove(oldPath);
                ResourceFileWatcher.HooksForResourceFilepaths.Add(newPath, fileResource);
            }
        }

        public const uint NullResource = 0;
        private uint _resourceIdCounter = 1;

        internal uint GetNextResourceId()
        {
            return _resourceIdCounter++;
        }

        public static void Init(Device device)
        {
            _instance ??= new ResourceManager(device);
        }

        private static ResourceManager _instance;

        private ResourceManager(Device device)
        {
            Device = device;

            var samplerDesc = new SamplerStateDescription()
                                  {
                                      Filter = Filter.MinMagMipLinear,
                                      AddressU = TextureAddressMode.Clamp,
                                      AddressV = TextureAddressMode.Clamp,
                                      AddressW = TextureAddressMode.Clamp,
                                      MipLodBias = 0.0f,
                                      MaximumAnisotropy = 1,
                                      ComparisonFunction = Comparison.Never,
                                      BorderColor = new RawColor4(1.0f, 1.0f, 1.0f, 1.0f),
                                      MinimumLod = -Single.MaxValue,
                                      MaximumLod = Single.MaxValue,
                                  };
            DefaultSamplerState = new SamplerState(device, samplerDesc);
            ResourceFileWatcher.Setup();
        }

        public SamplerState DefaultSamplerState { get; }

        public static void SetupConstBuffer<T>(T bufferData, ref Buffer buffer) where T : struct
        {
            using var data = new DataStream(Marshal.SizeOf(typeof(T)), true, true);

            data.Write(bufferData);
            data.Position = 0;

            if (buffer == null)
            {
                var bufferDesc = new BufferDescription
                                     {
                                         Usage = ResourceUsage.Default,
                                         SizeInBytes = Marshal.SizeOf(typeof(T)),
                                         BindFlags = BindFlags.ConstantBuffer
                                     };
                buffer = new Buffer(Device, data, bufferDesc);
            }
            else
            {
                Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer);
            }
        }

        public static void SetupBuffer(BufferDescription bufferDesc, ref Buffer buffer)
        {
            buffer ??= new Buffer(Device, bufferDesc);
        }

        public static void SetupIndirectBuffer(int sizeInBytes, ref Buffer buffer)
        {
            var bufferDesc = new BufferDescription
                                 {
                                     Usage = ResourceUsage.Default,
                                     BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                     SizeInBytes = sizeInBytes,
                                     OptionFlags = ResourceOptionFlags.DrawIndirectArguments,
                                 };
            SetupBuffer(bufferDesc, ref buffer);
        }

        public static void CreateBufferUav<T>(Buffer buffer, Format format, ref UnorderedAccessView uav)
        {
            if (buffer == null)
                return;

            if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) != 0)
            {
                Log.Warning($"Input buffer is structured, skipping UAV creation.");
                return;
            }

            uav?.Dispose();
            var desc = new UnorderedAccessViewDescription
                           {
                               Dimension = UnorderedAccessViewDimension.Buffer,
                               Format = format,
                               Buffer = new UnorderedAccessViewDescription.BufferResource()
                                            {
                                                FirstElement = 0,
                                                ElementCount = buffer.Description.SizeInBytes / Marshal.SizeOf<T>(),
                                                Flags = UnorderedAccessViewBufferFlags.None
                                            }
                           };
            uav = new UnorderedAccessView(Device, buffer, desc);
        }

        public static void SetupStructuredBuffer<T>(T[] bufferData, ref Buffer buffer) where T : struct
        {
            int stride = Marshal.SizeOf(typeof(T));
            int sizeInBytes = stride * bufferData.Length;
            SetupStructuredBuffer(bufferData, sizeInBytes, stride, ref buffer);
        }

        public static void SetupStructuredBuffer<T>(T[] bufferData, int sizeInBytes, int stride, ref Buffer buffer) where T : struct
        {
            using var data = new DataStream(sizeInBytes, true, true);

            data.WriteRange(bufferData);
            data.Position = 0;

            SetupStructuredBuffer(data, sizeInBytes, stride, ref buffer);
        }

        public static void SetupStructuredBuffer(DataStream data, int sizeInBytes, int stride, ref Buffer buffer)
        {
            if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
            {
                buffer?.Dispose();
                var bufferDesc = new BufferDescription
                                     {
                                         Usage = ResourceUsage.Default,
                                         BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                         SizeInBytes = sizeInBytes,
                                         OptionFlags = ResourceOptionFlags.BufferStructured,
                                         StructureByteStride = stride
                                     };
                buffer = new Buffer(Device, data, bufferDesc);
            }
            else
            {
                Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer);
            }
        }

        public static void SetupStructuredBuffer(int sizeInBytes, int stride, ref Buffer buffer)
        {
            try
            {
                if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
                {
                    buffer?.Dispose();
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                             SizeInBytes = sizeInBytes,
                                             OptionFlags = ResourceOptionFlags.BufferStructured,
                                             StructureByteStride = stride
                                         };
                    try
                    {
                        buffer = new Buffer(Device, bufferDesc);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to setup structured buffer (stride:{stride} {sizeInBytes}b):" + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to create Structured buffer " + e.Message);
            }
        }

        public static void CreateStructuredBufferSrv(Buffer buffer, ref ShaderResourceView srv)
        {
            if (buffer == null)
                return;

            try
            {
                if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == 0)
                {
                    // Log.Warning($"{nameof(SrvFromStructuredBuffer)} - input buffer is not structured, skipping SRV creation.");
                    return;
                }

                srv?.Dispose();
                var srvDesc = new ShaderResourceViewDescription()
                                  {
                                      Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                                      Format = Format.Unknown,
                                      BufferEx = new ShaderResourceViewDescription.ExtendedBufferResource
                                                     {
                                                         FirstElement = 0,
                                                         ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride
                                                     }
                                  };
                srv = new ShaderResourceView(Device, buffer, srvDesc);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to create SRV:" + e.Message);
            }
        }

        public static void CreateStructuredBufferUav(Buffer buffer, UnorderedAccessViewBufferFlags bufferFlags, ref UnorderedAccessView uav)
        {
            if (buffer == null)
                return;

            try
            {
                if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == 0)
                {
                    // Log.Warning($"{nameof(SrvFromStructuredBuffer)} - input buffer is not structured, skipping SRV creation.");
                    return;
                }

                uav?.Dispose();
                var uavDesc = new UnorderedAccessViewDescription()
                                  {
                                      Dimension = UnorderedAccessViewDimension.Buffer,
                                      Format = Format.Unknown,
                                      Buffer = new UnorderedAccessViewDescription.BufferResource
                                                   {
                                                       FirstElement = 0,
                                                       ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride,
                                                       Flags = bufferFlags
                                                   }
                                  };
                uav = new UnorderedAccessView(Device, buffer, uavDesc);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to create UAV " + e.Message);
            }
        }

        public uint CreateOperatorEntry(string sourceFilePath, string name, OperatorResource.UpdateDelegate updateHandler)
        {
            // todo: code below is redundant with all file resources -> refactor
            if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(sourceFilePath, out var fileResource))
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (ResourcesById[id] is OperatorResource)
                    {
                        return id;
                    }
                }
            }

            var resourceEntry = new OperatorResource(GetNextResourceId(), name, null, updateHandler);
            ResourcesById.Add(resourceEntry.Id, resourceEntry);

            if (fileResource == null)
            {
                try
                {
                    fileResource = new ResourceFileHook(sourceFilePath, new[] { resourceEntry.Id });
                    ResourceFileWatcher.HooksForResourceFilepaths.Add(sourceFilePath, fileResource);
                }
                catch (Exception e)
                {
                    Log.Error($"Can't set file resource hook to '{sourceFilePath}': {e.Message}");
                }
            }
            else
            {
                // File resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            return resourceEntry.Id;
        }

        public static Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            var stride = bitmapSource.Size.Width * 4;
            using var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true);

            // Copy the content of the WIC to the buffer
            bitmapSource.CopyPixels(stride, buffer);
            int mipLevels = (int)Math.Log(bitmapSource.Size.Width, 2.0) + 1;
            var texDesc = new Texture2DDescription()
                              {
                                  Width = bitmapSource.Size.Width,
                                  Height = bitmapSource.Size.Height,
                                  ArraySize = 1,
                                  BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                                  Usage = ResourceUsage.Default,
                                  CpuAccessFlags = CpuAccessFlags.None,
                                  Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                                  MipLevels = mipLevels,
                                  OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                                  SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                              };
            var dataRectangles = new DataRectangle[mipLevels];
            for (int i = 0; i < mipLevels; i++)
            {
                dataRectangles[i] = new DataRectangle(buffer.DataPointer, stride);
                stride /= 2;
            }

            return new Texture2D(device, texDesc, dataRectangles);
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

        public uint GetIdForTexture(Texture2D texture)
        {
            if (texture == null)
                return NullResource;

            foreach (var (id, resourceEntry) in ResourcesById)
            {
                if (resourceEntry is Texture2dResource textureResource)
                {
                    if (textureResource.Texture == texture)
                        return id;
                }
            }

            return NullResource;
        }

        public uint GetIdForTexture(Texture3D texture)
        {
            if (texture == null)
                return NullResource;

            foreach (var (id, resourceEntry) in ResourcesById)
            {
                if (resourceEntry is Texture3dResource textureResource)
                {
                    if (textureResource.Texture == texture)
                        return id;
                }
            }

            return NullResource;
        }

        public void CreateShaderResourceView(uint textureId, string name, ref ShaderResourceView shaderResourceView)
        {
            if (ResourcesById.TryGetValue(textureId, out var resource))
            {
                if (resource is Texture2dResource texture2dResource)
                {
                    shaderResourceView?.Dispose();
                    shaderResourceView = new ShaderResourceView(Device, texture2dResource.Texture) { DebugName = name };
                    Log.Info($"Created shader resource view '{name}' for texture '{texture2dResource.Name}'.");
                }
                else if (resource is Texture3dResource texture3dResource)
                {
                    shaderResourceView?.Dispose();
                    shaderResourceView = new ShaderResourceView(Device, texture3dResource.Texture) { DebugName = name };
                    Log.Info($"Created shader resource view '{name}' for texture '{texture3dResource.Name}'.");
                }
                else
                {
                    Log.Error("Trying to generate shader resource view from a resource that's not a texture resource");
                }
            }
            else
            {
                Log.Error($"Trying to look up texture resource with id {textureId} but did not found it.");
            }
        }

        public void CreateUnorderedAccessView(uint textureId, string name, ref UnorderedAccessView unorderedAccessView)
        {
            if (ResourcesById.TryGetValue(textureId, out var resource))
            {
                if (resource is Texture2dResource texture2dResource)
                {
                    unorderedAccessView?.Dispose();
                    unorderedAccessView = new UnorderedAccessView(Device, texture2dResource.Texture) { DebugName = name };
                    Log.Info($"Created unordered resource view '{name}' for texture '{texture2dResource.Name}'.");
                }
                else if (resource is Texture3dResource texture3dResource)
                {
                    unorderedAccessView?.Dispose();
                    unorderedAccessView = new UnorderedAccessView(Device, texture3dResource.Texture) { DebugName = name };
                    Log.Info($"Created unordered resource view '{name}' for texture '{texture3dResource.Name}'.");
                }
                else
                {
                    Log.Error("Trying to generate unordered resource view from a resource that's not a texture resource");
                }
            }
            else
            {
                Log.Error($"Trying to look up texture resource with id {textureId} but did not found it.");
            }
        }

        public void CreateRenderTargetView(uint textureId, string name, ref RenderTargetView renderTargetView)
        {
            if (ResourcesById.TryGetValue(textureId, out var resource))
            {
                if (resource is Texture2dResource texture2dResource)
                {
                    renderTargetView?.Dispose();
                    renderTargetView = new RenderTargetView(Device, texture2dResource.Texture) { DebugName = name };
                    Log.Info($"Created render target view '{name}' for texture '{texture2dResource.Name}'.");
                }
                else if (resource is Texture3dResource texture3dResource)
                {
                    renderTargetView?.Dispose();
                    renderTargetView = new RenderTargetView(Device, texture3dResource.Texture) { DebugName = name };
                    Log.Info($"Created render target view '{name}' for texture '{texture3dResource.Name}'.");
                }
                else
                {
                    Log.Error("Trying to generate render target view from a resource that's not a texture resource");
                }
            }
            else
            {
                Log.Error($"Trying to look up texture resource with id {textureId} but did not found it.");
            }
        }

        /**
         * Returns a textureViewResourceEntryId
         */
        private uint CreateShaderResourceView(uint textureId, string name)
        {
            ShaderResourceView textureView = null;
            CreateShaderResourceView(textureId, name, ref textureView);
            var textureViewResourceEntry = new ShaderResourceViewResource(GetNextResourceId(), name, textureView, textureId);
            ResourcesById.Add(textureViewResourceEntry.Id, textureViewResourceEntry);
            _shaderResourceViews.Add(textureViewResourceEntry);
            return textureViewResourceEntry.Id;
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
                uint srvId = (from srvResourceEntry in _shaderResourceViews
                              where srvResourceEntry.TextureId == textureId
                              select srvResourceEntry.Id).Single();
                return (textureId, srvId);
            }

            Texture2D texture = null;
            ShaderResourceView srv = null;
            var srvResourceId = NullResource;
            if (filename.ToLower().EndsWith(".dds"))
            {
                var ddsFile = JeremyAnsel.Media.Dds.DdsFile.FromFile(filename);
                DdsDirectX.CreateTexture(ddsFile, Device, Device.ImmediateContext, out var resource, out srv);
                texture = (Texture2D)resource;
            }
            else
            {
                CreateTexture2d(filename, ref texture);
            }

            var fileName = Path.GetFileName(filename);
            var textureResourceEntry = new Texture2dResource(GetNextResourceId(), fileName, texture);
            ResourcesById.Add(textureResourceEntry.Id, textureResourceEntry);

            if (srv == null)
            {
                srvResourceId = CreateShaderResourceView(textureResourceEntry.Id, fileName);
            }
            else
            {
                var textureViewResourceEntry = new ShaderResourceViewResource(GetNextResourceId(), fileName, srv, textureResourceEntry.Id);
                ResourcesById.Add(textureViewResourceEntry.Id, textureViewResourceEntry);
                _shaderResourceViews.Add(textureViewResourceEntry);
                srvResourceId = textureViewResourceEntry.Id;
            }

            var fileResource = new ResourceFileHook(filename, new[] { textureResourceEntry.Id, srvResourceId });
            fileResource.FileChangeAction += fileChangeAction;
            ResourceFileWatcher.HooksForResourceFilepaths.Add(filename, fileResource);

            return (textureResourceEntry.Id, srvResourceId);
        }

        public void UpdateTextureFromFile(uint textureId, string path, ref Texture2D texture)
        {
            ResourcesById.TryGetValue(textureId, out var resource);
            if (resource is Texture2dResource textureResource)
            {
                CreateTexture2d(path, ref textureResource.Texture);
                texture = textureResource.Texture;
            }
        }

        // returns true if the texture changed
        public bool CreateTexture2d(Texture2DDescription description, string name, ref uint id, ref Texture2D texture)
        {
            if (texture != null)
            {
                bool descriptionMatches;
                try
                {
                    descriptionMatches = texture.Description.Equals(description);
                }
                catch
                {
                    descriptionMatches = false;
                }

                if (descriptionMatches)
                {
                    return false; // no change
                }
            }

            ResourcesById.TryGetValue(id, out var resource);
            Texture2dResource texture2dResource = resource as Texture2dResource;

            if (texture2dResource == null)
            {
                // no entry so far, if texture is also null then create a new one
                texture ??= new Texture2D(Device, description);

                // new texture, create resource entry
                texture2dResource = new Texture2dResource(GetNextResourceId(), name, texture);
                ResourcesById.Add(texture2dResource.Id, texture2dResource);
            }
            else
            {
                texture2dResource.Texture?.Dispose();
                texture2dResource.Texture = new Texture2D(Device, description);
                texture = texture2dResource.Texture;
            }

            id = texture2dResource.Id;

            return true;
        }

        public bool CreateTexture3d(Texture3DDescription description, string name, ref uint id, ref Texture3D texture)
        {
            if (texture != null && texture.Description.Equals(description))
            {
                return false; // no change
            }

            ResourcesById.TryGetValue(id, out var resource);
            Texture3dResource texture3dResource = resource as Texture3dResource;

            if (texture3dResource == null)
            {
                // no entry so far, if texture is also null then create a new one
                texture ??= new Texture3D(Device, description);

                // new texture, create resource entry
                texture3dResource = new Texture3dResource(GetNextResourceId(), name, texture);
                ResourcesById.Add(texture3dResource.Id, texture3dResource);
            }
            else
            {
                texture3dResource.Texture?.Dispose();
                texture3dResource.Texture = new Texture3D(Device, description);
                texture = texture3dResource.Texture;
            }

            id = texture3dResource.Id;

            return true;
        }

        #region Shaders

        public bool TryCreateShaderResourceFromSource<TShader>(out ShaderResource<TShader> resource, string shaderSource, out string errorMessage, string name = "", string entryPoint = "main")
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
                ResourcesById.Add(newResource.Id, newResource);
            }
            else
            {
                Log.Error($"Failed to compile shader '{name}'");
            }
            
            resource = newResource;
            return compiled;
        }

        string ConstructShaderPath(string fileName)
        {
            if (Path.IsPathRooted(fileName))
                return fileName;
            
            var fileInfo = new FileInfo(fileName);
            
            // get the most parent directory and check if it's already the resources folder
            var parentDir = fileInfo.Directory;
            while (parentDir != null && parentDir.Name != ResourcesFolder)
            {
                parentDir = parentDir.Parent;
            }
            
            if (parentDir == null)
            {
                // not in resources folder, so prepend it
                return Path.Combine(ResourcesFolder, fileName);
            }
            
            // already in resources folder
            return fileName;
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
            if(!fileInfo.Exists)
            {
                resource = null;
                errorMessage = $"File '{path}' doesn't exist";
                return false;
            }
            
            if(string.IsNullOrWhiteSpace(name))
                name = fileInfo.Name;

            if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(path, out var fileHook))
            {
                foreach (var id in fileHook.ResourceIds)
                {
                    var resourceById = ResourcesById[id];
                    if (resourceById is ShaderResource<TShader> shaderResource && shaderResource.EntryPoint == entryPoint)
                    {
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

            ResourcesById.Add(resource.Id, resource);

            // Warning: this may not play nice in a multi-threaded environment
            if (fileHook == null)
            {
                fileHook = new ResourceFileHook(path, new[] { resourceId });
                ResourceFileWatcher.HooksForResourceFilepaths.Add(path, fileHook);
            }
            
            fileHook.FileChangeAction -= fileChangedAction;
            fileHook.FileChangeAction += fileChangedAction;

            return true;
        }

        #endregion

        public static readonly Dictionary<uint, AbstractResource> ResourcesById = new();

        private static readonly List<ShaderResourceViewResource> _shaderResourceViews = new();

        public const string ResourcesFolder = @"Resources";

        public static Device Device { get; private set; }
    }
}