using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.WIC;

namespace T3.Core
{
    public class Resource
    {
        public Resource(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name;
    }

    class FileResource
    {
        public FileResource(string path, IEnumerable<Guid> ids)
        {
            Path = path;
            ResourceIds.AddRange(ids);
            LastWriteReferenceTime = File.GetLastWriteTime(path);
        }

        public string Path;
        public List<Guid> ResourceIds = new List<Guid>();
        public DateTime LastWriteReferenceTime;
    }

    public interface IUpdateable
    {
        void Update(string path);
    }

    public abstract class ShaderResource : Resource, IUpdateable
    {
        protected ShaderResource(Guid id, string name, string entryPoint, ShaderBytecode blob)
            : base(id, name)
        {
            EntryPoint = entryPoint;
            Blob = blob;
        }

        public abstract void Update(string path);

        public string EntryPoint { get; }
        public ShaderBytecode Blob;// { get; internal set; }
    }

    public class VertexShaderResource : ShaderResource
    {
        public VertexShaderResource(Guid id, string name, string entryPoint, ShaderBytecode blob, VertexShader vertexShader)
            : base(id, name, entryPoint, blob)
        {
            VertexShader = vertexShader;
        }

        public override void Update(string path)
        {
            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "vs_5_0", ref VertexShader, ref Blob);
        }

        public VertexShader VertexShader;
    }

    public class PixelShaderResource : ShaderResource
    {
        public PixelShaderResource(Guid id, string name, string entryPoint, ShaderBytecode blob, PixelShader pixelShader)
            : base(id, name, entryPoint, blob)
        {
            PixelShader = pixelShader;
        }

        public PixelShader PixelShader;
        public override void Update(string path)
        {
            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "ps_5_0", ref PixelShader, ref Blob);
        }
    }

    public class TextureResource : Resource, IUpdateable
    {
        public TextureResource(Guid id, string name, Texture2D texture)
            : base(id, name)
        {
            Texture = texture;
        }

        public void Update(string path)
        {
            ResourceManager.Instance().CreateTexture(path, ref Texture);
        }

        public Texture2D Texture;
    }

    public class ShaderResourceViewResource : Resource, IUpdateable
    {
        public ShaderResourceViewResource(Guid id, string name, ShaderResourceView srv, Guid textureId)
            : base(id, name)
        {
            ShaderResourceView = srv;
            TextureId = textureId;
        }

        public void Update(string path)
        {
            ResourceManager.Instance().CreateShaderResourceView(TextureId, Name, ref ShaderResourceView);
        }

        public ShaderResourceView ShaderResourceView;
        public Guid TextureId;
    }

    public class ResourceManager
    {
        public static ResourceManager Instance()
        {
            return _instance;
        }

        public static void Init(Device device)
        {
            if (_instance == null)
                _instance = new ResourceManager(device);
        }
        private static ResourceManager _instance;

        private ResourceManager(Device device)
        {
            _device = device;
            _hlslFileWatcher = new FileSystemWatcher(@"Resources", "*.hlsl");
            _hlslFileWatcher.Changed += OnChanged;
            _hlslFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime; // creation time needed for visual studio (2017)
            _hlslFileWatcher.EnableRaisingEvents = true;

            _textureFileWatcher = new FileSystemWatcher(@"Resources", "*.jpg");//"*.png|*.jpg|*.dds|*.tiff");
            _textureFileWatcher.Changed += OnChanged;
            _textureFileWatcher.Created += OnChanged;
            _hlslFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            _textureFileWatcher.EnableRaisingEvents = true;
        }

        internal void CompileShader<TShader>(string srcFile, string entryPoint, string name, string profile, ref TShader shader, ref ShaderBytecode blob)
            where TShader : class, IDisposable
        {
            CompilationResult compilationResult = null;
            try
            {
                compilationResult = ShaderBytecode.CompileFromFile(srcFile, entryPoint, profile, ShaderFlags.None, EffectFlags.None);
            }
            catch (Exception ce)
            {
                Console.WriteLine($"Failed to compile shader '{name}': {ce.Message}\nUsing previous resource state.");
                return;
            }

            blob?.Dispose();
            blob = compilationResult.Bytecode;

            shader?.Dispose();
            // as shader type is generic we've to use Activator and PropertyInfo to create/set the shader object
            Type shaderType = typeof(TShader);
            shader = (TShader)Activator.CreateInstance(shaderType, _device, blob.Data, null);
            PropertyInfo debugNameInfo = shaderType.GetProperty("DebugName");
            debugNameInfo?.SetValue(shader, name);

            Console.WriteLine($"Successfully compiled shader '{name}' from '{srcFile}'");
        }

        public Guid CreateVertexShader(string srcFile, string entryPoint, string name)
        {
            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is VertexShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Console.WriteLine($"Warning: trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            VertexShader vertexShader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "vs_5_0", ref vertexShader, ref blob);
            if (vertexShader == null)
            {
                Console.WriteLine("Failed to create vertex shader '{name}'.");
                return Guid.Empty;
            }

            var resourceEntry = new VertexShaderResource(Guid.NewGuid(), name, entryPoint, blob, vertexShader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            VertexShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                FileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            return resourceEntry.Id;
        }

        public Guid CreatePixelShader(string srcFile, string entryPoint, string name)
        {
            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is PixelShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Console.WriteLine($"Warning: trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            PixelShader shader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "ps_5_0", ref shader, ref blob);
            if (shader == null)
            {
                Console.WriteLine("Failed to create pixel shader '{name}'.");
                return Guid.Empty;
            }

            var resourceEntry = new PixelShaderResource(Guid.NewGuid(), name, entryPoint, blob, shader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            PixelShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                FileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            return resourceEntry.Id;
        }

        private void OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (FileResources.TryGetValue(fileSystemEventArgs.FullPath, out var fileResource))
            {
                DateTime lastWriteTime = File.GetLastWriteTime(fileSystemEventArgs.FullPath);
                if (lastWriteTime != fileResource.LastWriteReferenceTime)
                {
                    Console.WriteLine($"File '{fileSystemEventArgs.FullPath}' changed due to {fileSystemEventArgs.ChangeType}");
                    foreach (var id in fileResource.ResourceIds)
                    {
                        // update all resources that depend from this file
                        if (Resources.TryGetValue(id, out var resource))
                        {
                            var updateable = resource as IUpdateable;
                            updateable?.Update(fileResource.Path);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: trying to update a non existing file resource '{fileResource.Path}'.");
                        }
                    }
                    fileResource.LastWriteReferenceTime = lastWriteTime;
                }
                // else discard the (duplicated) OnChanged event
            }
        }

        private static Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new Texture2D(device, new Texture2DDescription()
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    Usage = ResourceUsage.Immutable,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                },
                                     new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }

        public void CreateTexture(string filename, ref Texture2D texture)
        {
            try
            {
                ImagingFactory factory = new ImagingFactory();
                var bitmapDecoder = new BitmapDecoder(factory, filename, DecodeOptions.CacheOnDemand);
                var formatConverter = new FormatConverter(factory);
                var bitmapFrameDecode = bitmapDecoder.GetFrame(0);
                formatConverter.Initialize(bitmapFrameDecode, PixelFormat.Format32bppPRGBA, BitmapDitherType.None, null, 0.0, BitmapPaletteType.Custom);

                texture?.Dispose();
                texture = CreateTexture2DFromBitmap(_device, formatConverter);
                string name = Path.GetFileName(filename);
                texture.DebugName = name;
                bitmapFrameDecode.Dispose();
                bitmapDecoder.Dispose();
                formatConverter.Dispose();
                factory.Dispose();
                Console.WriteLine($"Created texture '{name}' from '{filename}'");
            }
            catch (Exception)
            {
                Console.WriteLine($"Info: couldn't access file '{filename}' as it was locked.");
            }
        }

        public void CreateShaderResourceView(Guid textureId, string name, ref ShaderResourceView shaderResourceView)
        {
            if (Resources.TryGetValue(textureId, out var resource))
            {
                if (resource is TextureResource textureResource)
                {
                    shaderResourceView?.Dispose();
                    shaderResourceView = new ShaderResourceView(_device, textureResource.Texture) { DebugName = name };
                    Console.WriteLine($"Created shader resource view '{name}' for texture '{textureResource.Name}'.");
                }
                else
                {
                    Console.WriteLine("Error: trying to generate shader resource view from a resource that's not a texture resource");
                }
            }
            else
            {
                Console.WriteLine($"Error: trying to look up texture resource with id {textureId} but did not found it.");
            }
        }

        public Guid CreateShaderResourceView(Guid textureId, string name)
        {
            ShaderResourceView textureView = null;
            CreateShaderResourceView(textureId, name, ref textureView);
            var textureViewResourceEntry = new ShaderResourceViewResource(Guid.NewGuid(), name, textureView, textureId);
            Resources.Add(textureViewResourceEntry.Id, textureViewResourceEntry);
            ShaderResourceViews.Add(textureViewResourceEntry);
            return textureViewResourceEntry.Id;
        }

        public (Guid, Guid) CreateTextureFromFile(string filename) /* TODO, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags, ResourceOptionFlags miscFlags, int loadFlags*/
        {
            if (FileResources.TryGetValue(filename, out var existingFileResource))
            {
                Console.WriteLine($"Error: trying to create an already existing file resource ('{filename}'");
                return (existingFileResource.ResourceIds.First(), Guid.Empty);
            }

            Texture2D texture = null;
            CreateTexture(filename, ref texture);
            string name = Path.GetFileName(filename);
            var textureResourceEntry = new TextureResource(Guid.NewGuid(), name, texture);
            Resources.Add(textureResourceEntry.Id, textureResourceEntry);
            Textures.Add(textureResourceEntry);

            Guid shaderResourceViewId = CreateShaderResourceView(textureResourceEntry.Id, name);

            var fileResource = new FileResource(filename, new[] { textureResourceEntry.Id, shaderResourceViewId });
            FileResources.Add(filename, fileResource);

            return (textureResourceEntry.Id, shaderResourceViewId);
        }

        public void CreateTexture(Texture2DDescription description, string name, ref Guid id, ref Texture2D texture)
        {
            if (texture != null && texture.Description.Equals(description))
            {
                return; // no change
            }

            Resources.TryGetValue(id, out var resource);
            TextureResource textureResource = resource as TextureResource;

            if (textureResource == null)
            {
                // new texture, create resource entry
                textureResource = new TextureResource(Guid.NewGuid(), name, texture);
                Resources.Add(textureResource.Id, textureResource);
                Textures.Add(textureResource);
            }
            else
            {
                textureResource.Texture = texture;
            }

            id = textureResource.Id;
        }


        public Dictionary<Guid, Resource> Resources = new Dictionary<Guid, Resource>();
        internal Dictionary<string, FileResource> FileResources = new Dictionary<string, FileResource>();
        internal List<VertexShaderResource> VertexShaders = new List<VertexShaderResource>();
        internal List<PixelShaderResource> PixelShaders = new List<PixelShaderResource>();
        internal List<TextureResource> Textures = new List<TextureResource>();
        internal List<ShaderResourceViewResource> ShaderResourceViews = new List<ShaderResourceViewResource>();
        private readonly Device _device;
        private readonly FileSystemWatcher _hlslFileWatcher;
        private readonly FileSystemWatcher _textureFileWatcher;
    }
}
