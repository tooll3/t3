using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Core
{
    public class Resource
    {
        public Resource(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; }
        public string Name;
    }

    class FileResource
    {
        public FileResource(string path, IEnumerable<uint> ids)
        {
            Path = path;
            ResourceIds.AddRange(ids);
            LastWriteReferenceTime = File.GetLastWriteTime(path);
        }

        public string Path;
        public List<uint> ResourceIds = new List<uint>();
        public DateTime LastWriteReferenceTime;
        public Action FileChangeAction;
    }

    public interface IUpdateable
    {
        void Update(string path);
    }

    public class OperatorResource : Resource, IUpdateable
    {
        public Assembly OperatorAssembly { get; private set; }
        public bool Updated { get; set; } = false;

        public OperatorResource(uint id, string name, Assembly operatorAssembly)
            : base(id, name)
        {
            OperatorAssembly = operatorAssembly;
        }

        public void Update(string path)
        {
            Log.Info($"Operator source '{path}' changed.");
            Log.Info($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");

            string source = string.Empty;
            try
            {
                source = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Log.Error($"Error opening file '{path}");
                Log.Error(e.Message);
                return;
            }

            if (string.IsNullOrEmpty(source))
            {
                Log.Info("Source was empty, skip compilation.");
                return;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("assemblyName",
                                                       new[] { syntaxTree },
                                                       new[]
                                                       {
                                                           MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                                                           MetadataReference.CreateFromFile(typeof(Resource).Assembly.Location)
                                                       },
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                Log.Info($"compilation results of '{path}':");
                if (!emitResult.Success)
                {
                    foreach (var entry in emitResult.Diagnostics)
                    {
                        Log.Info(entry.GetMessage());
                    }
                }
                else
                {
                    Log.Info("successful");
                    var newAssembly = Assembly.Load(dllStream.GetBuffer());
                    if (newAssembly.ExportedTypes.Any())
                    {
                        OperatorAssembly = newAssembly;
                        Updated = true;
                    }
                    else
                    {
                        Log.Error("New compiled Assembly had no exported type.");
                    }
                }
            }
        }
    }

    public abstract class ShaderResource : Resource
    {
        protected ShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob)
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
        public VertexShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, VertexShader vertexShader)
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
        public PixelShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, PixelShader pixelShader)
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

    public class ComputeShaderResource : ShaderResource
    {
        public ComputeShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, ComputeShader computeShader) :
            base(id, name, entryPoint, blob)
        {
            ComputeShader = computeShader;
        }

        public ComputeShader ComputeShader;

        public override void Update(string path)
        {
            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "cs_5_0", ref ComputeShader, ref Blob);
        }
    }

    public class TextureResource : Resource
    {
        public TextureResource(uint id, string name, Texture2D texture)
            : base(id, name)
        {
            Texture = texture;
        }

        public Texture2D Texture;
    }

    public class ShaderResourceViewResource : Resource
    {
        public ShaderResourceViewResource(uint id, string name, ShaderResourceView srv, uint textureId)
            : base(id, name)
        {
            ShaderResourceView = srv;
            TextureId = textureId;
        }

        public ShaderResourceView ShaderResourceView;
        public uint TextureId;
    }

    public class ResourceManager
    {
        public uint TestId = NULL_RESOURCE;

        public static ResourceManager Instance()
        {
            return _instance;
        }

        public T GetResource<T>(uint resourceId) where T : Resource
        {
            return (T)Resources[resourceId];
        }

        public ComputeShader GetComputeShader(uint resourceId)
        {
            return GetResource<ComputeShaderResource>(resourceId).ComputeShader;
        }
        
        public const uint NULL_RESOURCE = 0;
        private uint _resourceIdCounter = 1;

        private uint GetNextResourceId()
        {
            return _resourceIdCounter++;
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

            _csFileWatcher = new FileSystemWatcher(@"..\Operators\Types", "*.cs");
            _csFileWatcher.Changed += OnChanged;
            _csFileWatcher.NotifyFilter = NotifyFilters.LastWrite;// | NotifyFilters.CreationTime;
            _csFileWatcher.EnableRaisingEvents = true;
        }

        public void SetupConstBufferForCS<T>(T bufferData, ref Buffer buffer, int slot) where T : struct
        {
            using (var data = new DataStream(Marshal.SizeOf(typeof(T)), true, true))
            {
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
                    buffer = new Buffer(_device, data, bufferDesc);
                }
                else
                {
                    _device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer, 0);
                }
                _device.ImmediateContext.ComputeShader.SetConstantBuffer(slot, buffer);
            }
        }

        internal void CompileShader<TShader>(string srcFile, string entryPoint, string name, string profile, ref TShader shader, ref ShaderBytecode blob)
            where TShader : class, IDisposable
        {
            CompilationResult compilationResult = null;
            try
            {
                compilationResult = ShaderBytecode.CompileFromFile(srcFile, entryPoint, profile, ShaderFlags.Debug, EffectFlags.None);
            }
            catch (Exception ce)
            {
                Log.Info($"Failed to compile shader '{name}': {ce.Message}\nUsing previous resource state.");
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

            Log.Info($"Successfully compiled shader '{name}' from '{srcFile}'");
        }

        public uint CreateVertexShader(string srcFile, string entryPoint, string name)
        {
            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is VertexShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Log.Warning($"Trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            VertexShader vertexShader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "vs_5_0", ref vertexShader, ref blob);
            if (vertexShader == null)
            {
                Log.Info("Failed to create vertex shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new VertexShaderResource(GetNextResourceId(), name, entryPoint, blob, vertexShader);
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

        public uint CreatePixelShader(string srcFile, string entryPoint, string name)
        {
            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is PixelShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Log.Warning($"Trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            PixelShader shader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "ps_5_0", ref shader, ref blob);
            if (shader == null)
            {
                Log.Info("Failed to create pixel shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new PixelShaderResource(GetNextResourceId(), name, entryPoint, blob, shader);
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

        public uint CreateComputeShaderFromFile(string srcFile, string entryPoint, string name, Action fileChangedAction)
        {
            if (string.IsNullOrEmpty(srcFile) || string.IsNullOrEmpty(entryPoint))
                return NULL_RESOURCE;

            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is ComputeShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Log.Warning($"Trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            ComputeShader shader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "cs_5_0", ref shader, ref blob);
            if (shader == null)
            {
                Log.Info("Failed to create pixel shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new ComputeShaderResource(GetNextResourceId(), name, entryPoint, blob, shader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            ComputeShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                fileResource.FileChangeAction = fileChangedAction;
                FileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            return resourceEntry.Id;
        }

        public void UpdateComputeShaderFromFile(string path, uint id, ref ComputeShader computeShader)
        {
            Resources.TryGetValue(id, out var resource);
            if (resource is ComputeShaderResource csResource)
            {
                csResource.Update(path);
                computeShader = csResource.ComputeShader;
            }
        }

        public uint CreateOperatorEntry(string srcFile, string name)
        {
            // todo: code below is redundant with all file resources -> refactor
            bool foundFileEntryForPath = FileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is PixelShaderResource)
                    {
                        // if file resource already exists then it must be a different type
                        Log.Warning($"Trying to create an already existing file resource ('{srcFile}'");
                        return id;
                    }
                }
            }

            var resourceEntry = new OperatorResource(GetNextResourceId(), name, null);
            Resources.Add(resourceEntry.Id, resourceEntry);
            Operators.Add(resourceEntry);
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
                     // hack: in order to prevent editors like vs-code still having the file locked after writing to it, this gives these editors 
                     //       some time to release the lock. With a locked file Shader.ReadFromFile(...) function will throw an exception, because
                     //       it cannot read the file. 
                    Thread.Sleep(15);
                    Log.Info($"File '{fileSystemEventArgs.FullPath}' changed due to {fileSystemEventArgs.ChangeType}");
                    foreach (var id in fileResource.ResourceIds)
                    {
                        // update all resources that depend from this file
                        if (Resources.TryGetValue(id, out var resource))
                        {
                            var updateable = resource as IUpdateable;
                            updateable?.Update(fileResource.Path);
                            fileResource.FileChangeAction?.Invoke();
                        }
                        else
                        {
                            Log.Info($"Trying to update a non existing file resource '{fileResource.Path}'.");
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
            int stride = bitmapSource.Size.Width*4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height*stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                int mipLevels = (int)Math.Log(Math.Max(bitmapSource.Size.Width, bitmapSource.Size.Height), 2.0) + 1;
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
                Log.Info($"Created texture '{name}' from '{filename}'");
            }
            catch (Exception)
            {
                Log.Info($"Info: couldn't access file '{filename}' as it was locked.");
            }
        }

        public void CreateShaderResourceView(uint textureId, string name, ref ShaderResourceView shaderResourceView)
        {
            if (Resources.TryGetValue(textureId, out var resource))
            {
                if (resource is TextureResource textureResource)
                {
                    shaderResourceView?.Dispose();
                    shaderResourceView = new ShaderResourceView(_device, textureResource.Texture) { DebugName = name };
                    Log.Info($"Created shader resource view '{name}' for texture '{textureResource.Name}'.");
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

        public uint CreateShaderResourceView(uint textureId, string name)
        {
            ShaderResourceView textureView = null;
            CreateShaderResourceView(textureId, name, ref textureView);
            var textureViewResourceEntry = new ShaderResourceViewResource(GetNextResourceId(), name, textureView, textureId);
            Resources.Add(textureViewResourceEntry.Id, textureViewResourceEntry);
            ShaderResourceViews.Add(textureViewResourceEntry);
            return textureViewResourceEntry.Id;
        }

        public (uint, uint) CreateTextureFromFile(string filename, Action fileChangeAction) /* TODO, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags, ResourceOptionFlags miscFlags, int loadFlags*/
        {
            if (FileResources.TryGetValue(filename, out var existingFileResource))
            {
                Log.Error($"Trying to create an already existing file resource ('{filename}'");
                return (existingFileResource.ResourceIds.First(), NULL_RESOURCE);
            }

            Texture2D texture = null;
            CreateTexture(filename, ref texture);
            string name = Path.GetFileName(filename);
            var textureResourceEntry = new TextureResource(GetNextResourceId(), name, texture);
            Resources.Add(textureResourceEntry.Id, textureResourceEntry);
            Textures.Add(textureResourceEntry);

            uint shaderResourceViewId = CreateShaderResourceView(textureResourceEntry.Id, name);

            var fileResource = new FileResource(filename, new[] { textureResourceEntry.Id, shaderResourceViewId });
            fileResource.FileChangeAction = fileChangeAction;
            FileResources.Add(filename, fileResource);

            return (textureResourceEntry.Id, shaderResourceViewId);
        }

        public void UpdateTextureFromFile(uint textureId, string path, ref Texture2D texture)
        {
            Resources.TryGetValue(textureId, out var resource);
            if (resource is TextureResource textureResource)
            {
                CreateTexture(path, ref textureResource.Texture);
                texture = textureResource.Texture;
            }
        }

        // returns true if the texture changed
        public bool CreateTexture(Texture2DDescription description, string name, ref uint id, ref Texture2D texture)
        {
            if (texture != null && texture.Description.Equals(description))
            {
                return false; // no change
            }

            Resources.TryGetValue(id, out var resource);
            TextureResource textureResource = resource as TextureResource;

            if (textureResource == null)
            {
                // no entry so far, if texture is also null then create a new one
                if (texture == null)
                {
                    texture = new Texture2D(_device, description);
                }

                // new texture, create resource entry
                textureResource = new TextureResource(GetNextResourceId(), name, texture);
                Resources.Add(textureResource.Id, textureResource);
                Textures.Add(textureResource);
            }
            else
            {
                textureResource.Texture = texture;
            }

            id = textureResource.Id;

            return true;
        }

        private readonly Stopwatch _operatorUpdateStopwatch = new Stopwatch();
        public List<Symbol> UpdateChangedOperatorTypes()
        {
            var modifiedSymbols = new List<Symbol>();
            foreach (var opResource in Operators)
            {
                if (opResource.Updated)
                {
                    Type type = opResource.OperatorAssembly.ExportedTypes.FirstOrDefault();
                    if (type == null)
                    {
                        Log.Error("Error updateable operator had not exported type");
                        continue;
                    }
                    var symbolEntry = SymbolRegistry.Entries.FirstOrDefault(e => e.Value.Name == opResource.Name);// todo: name -> no good idea, use id
                    if (symbolEntry.Value != null)
                    {
                        _operatorUpdateStopwatch.Restart();
                        symbolEntry.Value.SetInstanceType(type);
                        opResource.Updated = false;
                        _operatorUpdateStopwatch.Stop();
                        Log.Info($"type updating took: {(double)_operatorUpdateStopwatch.ElapsedTicks / Stopwatch.Frequency}s");
                        modifiedSymbols.Add(symbolEntry.Value);
                    }
                    else
                    {
                        Log.Info($"Error replacing symbol type '{opResource.Name}");
                    }
                }
            }

            return modifiedSymbols;
        }

        public Dictionary<uint, Resource> Resources = new Dictionary<uint, Resource>();
        internal Dictionary<string, FileResource> FileResources = new Dictionary<string, FileResource>();
        internal List<VertexShaderResource> VertexShaders = new List<VertexShaderResource>();
        internal List<PixelShaderResource> PixelShaders = new List<PixelShaderResource>();
        internal List<ComputeShaderResource> ComputeShaders = new List<ComputeShaderResource>();
        internal List<TextureResource> Textures = new List<TextureResource>();
        internal List<ShaderResourceViewResource> ShaderResourceViews = new List<ShaderResourceViewResource>();
        public List<OperatorResource> Operators = new List<OperatorResource>(100);
        public readonly Device _device;
        private readonly FileSystemWatcher _hlslFileWatcher;
        private readonly FileSystemWatcher _textureFileWatcher;
        private readonly FileSystemWatcher _csFileWatcher;
    }
}
