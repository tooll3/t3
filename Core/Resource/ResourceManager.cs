using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using T3.Core.Logging;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

// ReSharper disable SuggestVarOrType_SimpleTypes <- Cynic doesn't like it
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable    <- keeping the file handlers as members is clearer

namespace T3.Core
{
    public abstract class Resource
    {
        public Resource(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; }
        public string Name;
        public bool UpToDate { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class FileResource
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

            var source = string.Empty;
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

            var referencedAssembliesNames = ResourceManager.Instance().OperatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                var asm = appDomainAssemblies.SingleOrDefault(assembly => assembly.GetName().Name == asmName.Name);
                if (asm != null)
                {
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                }
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("Operators",
                                                       new[] { syntaxTree },
                                                       referencedAssemblies.ToArray(),
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

        protected string EntryPoint { get; }
        protected ShaderBytecode Blob; // { get; internal set; }
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
            if (UpToDate)
                return;

            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "vs_5_0", ref VertexShader, ref Blob);
            UpToDate = true;
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
            if (UpToDate)
                return;

            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "ps_5_0", ref PixelShader, ref Blob);
            UpToDate = true;
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
            if (UpToDate)
                return;

            ResourceManager.Instance().CompileShader(path, EntryPoint, Name, "cs_5_0", ref ComputeShader, ref Blob);
            UpToDate = true;
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

        public readonly ShaderResourceView ShaderResourceView;
        public readonly uint TextureId;
    }

    public class ResourceManager
    {
        public uint TestId = NULL_RESOURCE;
        public Assembly OperatorsAssembly { get; set; }

        public static ResourceManager Instance()
        {
            return _instance;
        }

        public T GetResource<T>(uint resourceId) where T : Resource
        {
            return (T)Resources[resourceId];
        }

        public VertexShader GetVertexShader(uint resourceId)
        {
            return GetResource<VertexShaderResource>(resourceId).VertexShader;
        }

        public PixelShader GetPixelShader(uint resourceId)
        {
            return GetResource<PixelShaderResource>(resourceId).PixelShader;
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
            Device = device;
            _hlslFileWatcher = new FileSystemWatcher(ResourcesFolder, "*.hlsl");
            _hlslFileWatcher.IncludeSubdirectories = true;
            _hlslFileWatcher.Changed += OnChanged;
            _hlslFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime; // creation time needed for visual studio (2017)
            _hlslFileWatcher.EnableRaisingEvents = true;

            _pngFileWatcher = new FileSystemWatcher(ResourcesFolder, "*.png");
            _pngFileWatcher.IncludeSubdirectories = true;
            _pngFileWatcher.Changed += OnChanged;
            _pngFileWatcher.Created += OnChanged;
            _pngFileWatcher.EnableRaisingEvents = true;

            _jpgFileWatcher = new FileSystemWatcher(ResourcesFolder, "*.jpg");
            _jpgFileWatcher.IncludeSubdirectories = true;
            _jpgFileWatcher.Changed += OnChanged;
            _jpgFileWatcher.Created += OnChanged;
            _jpgFileWatcher.EnableRaisingEvents = true;

            _ddsFileWatcher = new FileSystemWatcher(ResourcesFolder, "*.dds");
            _ddsFileWatcher.IncludeSubdirectories = true;
            _ddsFileWatcher.Changed += OnChanged;
            _ddsFileWatcher.Created += OnChanged;
            _ddsFileWatcher.EnableRaisingEvents = true;

            _tiffFileWatcher = new FileSystemWatcher(ResourcesFolder, "*.tiff");
            _tiffFileWatcher.IncludeSubdirectories = true;
            _tiffFileWatcher.Changed += OnChanged;
            _tiffFileWatcher.Created += OnChanged;
            _tiffFileWatcher.EnableRaisingEvents = true;

            _csFileWatcher = new FileSystemWatcher(@"Operators\Types", "*.cs");
            _csFileWatcher.IncludeSubdirectories = true;
            _csFileWatcher.Changed += OnChanged;
            _csFileWatcher.Renamed += OnRenamed;
            _csFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;
            _csFileWatcher.EnableRaisingEvents = true;
        }

        public void SetupConstBuffer<T>(T bufferData, ref Buffer buffer) where T : struct
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
                    buffer = new Buffer(Device, data, bufferDesc);
                }
                else
                {
                    Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer, 0);
                }
            }
        }

        public void SetupBuffer(BufferDescription bufferDesc, ref Buffer buffer)
        {
            if (buffer == null)
            {
                buffer = new Buffer(Device, bufferDesc);
            }
        }

        public void SetupIndirectBuffer(int sizeInBytes, ref Buffer buffer)
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

        public void CreateBufferUav<T>(Buffer buffer, Format format, ref UnorderedAccessView uav)
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

        public void SetupStructuredBuffer<T>(T[] bufferData, ref Buffer buffer) where T : struct
        {
            int stride = Marshal.SizeOf(typeof(T));
            int sizeInBytes = stride * bufferData.Length;
            SetupStructuredBuffer(bufferData, sizeInBytes, stride, ref buffer);
        }

        public void SetupStructuredBuffer<T>(T[] bufferData, int sizeInBytes, int stride, ref Buffer buffer) where T : struct
        {
            using (var data = new DataStream(sizeInBytes, true, true))
            {
                data.WriteRange(bufferData);
                data.Position = 0;

                if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
                {
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
                    Device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer, 0);
                }
            }
        }

        public void SetupStructuredBuffer(int sizeInBytes, int stride, ref Buffer buffer)
        {
            if (buffer == null || buffer.Description.SizeInBytes != sizeInBytes)
            {
                var bufferDesc = new BufferDescription
                                 {
                                     Usage = ResourceUsage.Default,
                                     BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                     SizeInBytes = sizeInBytes,
                                     OptionFlags = ResourceOptionFlags.BufferStructured,
                                     StructureByteStride = stride
                                 };
                buffer = new Buffer(Device, bufferDesc);
            }
        }

        public void CreateStructuredBufferSrv(Buffer buffer, ref ShaderResourceView srv)
        {
            if (buffer == null)
                return;

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

        public void CreateStructuredBufferUav(Buffer buffer, UnorderedAccessViewBufferFlags bufferFlags, ref UnorderedAccessView uav)
        {
            if (buffer == null)
                return;

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

        class IncludeHandler : SharpDX.D3DCompiler.Include
        {
            private StreamReader _streamReader;

            public void Dispose()
            {
                _streamReader?.Dispose();
            }

            public IDisposable Shadow { get; set; }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                _streamReader = new StreamReader(Path.Combine(ResourcesFolder, fileName));
                return _streamReader.BaseStream;
            }

            public void Close(Stream stream)
            {
                _streamReader.Close();
            }
        }

        internal void CompileShader<TShader>(string srcFile, string entryPoint, string name, string profile, ref TShader shader, ref ShaderBytecode blob)
            where TShader : class, IDisposable
        {
            CompilationResult compilationResult = null;
            try
            {
                compilationResult =
                    ShaderBytecode.CompileFromFile(srcFile, entryPoint, profile, ShaderFlags.Debug, EffectFlags.None, null, new IncludeHandler());
            }
            catch (Exception ce)
            {
                Log.Error($"Failed to compile shader '{name}': {ce.Message}\nUsing previous resource state.");
                return;
            }

            blob?.Dispose();
            blob = compilationResult.Bytecode;

            shader?.Dispose();
            // as shader type is generic we've to use Activator and PropertyInfo to create/set the shader object
            Type shaderType = typeof(TShader);
            shader = (TShader)Activator.CreateInstance(shaderType, Device, blob.Data, null);
            PropertyInfo debugNameInfo = shaderType.GetProperty("DebugName");
            debugNameInfo?.SetValue(shader, name);

            Log.Info($"Successfully compiled shader '{name}' with profile '{profile}' from '{srcFile}'");
        }

        public uint CreateVertexShaderFromFile(string srcFile, string entryPoint, string name, Action fileChangedAction)
        {
            if (string.IsNullOrEmpty(srcFile) || string.IsNullOrEmpty(entryPoint))
                return NULL_RESOURCE;

            bool foundFileEntryForPath = _fileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is VertexShaderResource)
                    {
                        fileResource.FileChangeAction -= fileChangedAction;
                        fileResource.FileChangeAction += fileChangedAction;
                        return id;
                    }
                }
            }

            VertexShader vertexShader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "vs_5_0", ref vertexShader, ref blob);
            if (vertexShader == null)
            {
                Log.Info($"Failed to create vertex shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new VertexShaderResource(GetNextResourceId(), name, entryPoint, blob, vertexShader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            _vertexShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                _fileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            fileResource.FileChangeAction -= fileChangedAction;
            fileResource.FileChangeAction += fileChangedAction;

            return resourceEntry.Id;
        }

        public uint CreatePixelShaderFromFile(string srcFile, string entryPoint, string name, Action fileChangedAction)
        {
            if (string.IsNullOrEmpty(srcFile) || string.IsNullOrEmpty(entryPoint))
                return NULL_RESOURCE;

            bool foundFileEntryForPath = _fileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is PixelShaderResource)
                    {
                        fileResource.FileChangeAction -= fileChangedAction;
                        fileResource.FileChangeAction += fileChangedAction;
                        return id;
                    }
                }
            }

            PixelShader shader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "ps_5_0", ref shader, ref blob);
            if (shader == null)
            {
                Log.Info($"Failed to create pixel shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new PixelShaderResource(GetNextResourceId(), name, entryPoint, blob, shader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            _pixelShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                _fileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            fileResource.FileChangeAction -= fileChangedAction;
            fileResource.FileChangeAction += fileChangedAction;

            return resourceEntry.Id;
        }

        public uint CreateComputeShaderFromFile(string srcFile, string entryPoint, string name, Action fileChangedAction)
        {
            if (string.IsNullOrEmpty(srcFile) || string.IsNullOrEmpty(entryPoint))
                return NULL_RESOURCE;

            bool foundFileEntryForPath = _fileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is ComputeShaderResource)
                    {
                        fileResource.FileChangeAction -= fileChangedAction;
                        fileResource.FileChangeAction += fileChangedAction;
                        return id;
                    }
                }
            }

            ComputeShader shader = null;
            ShaderBytecode blob = null;
            CompileShader(srcFile, entryPoint, name, "cs_5_0", ref shader, ref blob);
            if (shader == null)
            {
                Log.Info($"Failed to create compute shader '{name}'.");
                return NULL_RESOURCE;
            }

            var resourceEntry = new ComputeShaderResource(GetNextResourceId(), name, entryPoint, blob, shader);
            Resources.Add(resourceEntry.Id, resourceEntry);
            _computeShaders.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                _fileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            fileResource.FileChangeAction -= fileChangedAction;
            fileResource.FileChangeAction += fileChangedAction;

            return resourceEntry.Id;
        }

        public void UpdateVertexShaderFromFile(string path, uint id, ref VertexShader vertexShader)
        {
            Resources.TryGetValue(id, out var resource);
            if (resource is VertexShaderResource vsResource)
            {
                vsResource.Update(path);
                vertexShader = vsResource.VertexShader;
            }
        }

        public void UpdatePixelShaderFromFile(string path, uint id, ref PixelShader vertexShader)
        {
            Resources.TryGetValue(id, out var resource);
            if (resource is PixelShaderResource vsResource)
            {
                vsResource.Update(path);
                vertexShader = vsResource.PixelShader;
            }
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
            bool foundFileEntryForPath = _fileResources.TryGetValue(srcFile, out var fileResource);
            if (foundFileEntryForPath)
            {
                foreach (var id in fileResource.ResourceIds)
                {
                    if (Resources[id] is PixelShaderResource)
                    {
                        return id;
                    }
                }
            }

            var resourceEntry = new OperatorResource(GetNextResourceId(), name, null);
            Resources.Add(resourceEntry.Id, resourceEntry);
            _operators.Add(resourceEntry);
            if (fileResource == null)
            {
                fileResource = new FileResource(srcFile, new[] { resourceEntry.Id });
                _fileResources.Add(srcFile, fileResource);
            }
            else
            {
                // file resource already exists, so just add the id of the new type resource
                fileResource.ResourceIds.Add(resourceEntry.Id);
            }

            return resourceEntry.Id;
        }

        public void RemoveOperatorEntry(uint resourceId)
        {
            if (Resources.TryGetValue(resourceId, out var entry))
            {
                _operators.Remove(entry as OperatorResource);
                Resources.Remove(resourceId);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            // Log.Info($"change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
            if (!_fileResources.TryGetValue(fileSystemEventArgs.FullPath, out var fileResource))
            {
                Log.Warning("Invalid FileResource?");
                return;
            }

            // Log.Info($"valid change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
            DateTime lastWriteTime = File.GetLastWriteTime(fileSystemEventArgs.FullPath);
            if (lastWriteTime != fileResource.LastWriteReferenceTime)
            {
                // Log.Info($"very valid change for '{fileSystemEventArgs.Name}' due to '{fileSystemEventArgs.ChangeType}'.");
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
                        resource.UpToDate = false;
                    }
                    else
                    {
                        Log.Info($"Trying to update a non existing file resource '{fileResource.Path}'.");
                    }
                }

                fileResource.FileChangeAction?.Invoke();

                fileResource.LastWriteReferenceTime = lastWriteTime;
            }

            // else discard the (duplicated) OnChanged event
        }

        private void OnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            //Log.Info($"renamed file from '{renamedEventArgs.OldFullPath}' to '{renamedEventArgs.FullPath}'");

            var extension = Path.GetExtension(renamedEventArgs.FullPath);
            if (extension != ".cs")
            {
                Log.Info($"Ignoring file rename to invalid extension '{extension}' in '{renamedEventArgs.FullPath}'.");
                return;
            }

            if (_fileResources.TryGetValue(renamedEventArgs.OldFullPath, out var fileResource))
            {
                Log.Info($"renamed file resource from '{renamedEventArgs.OldFullPath}' to '{renamedEventArgs.FullPath}'");
                fileResource.Path = renamedEventArgs.FullPath;
                _fileResources.Remove(renamedEventArgs.OldFullPath);
                _fileResources.Add(renamedEventArgs.FullPath, fileResource);
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
                texture = CreateTexture2DFromBitmap(Device, formatConverter);
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
                    shaderResourceView = new ShaderResourceView(Device, textureResource.Texture) { DebugName = name };
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
            _shaderResourceViews.Add(textureViewResourceEntry);
            return textureViewResourceEntry.Id;
        }

        public (uint, uint)
            CreateTextureFromFile(string filename,
                                  Action fileChangeAction) /* TODO, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags, ResourceOptionFlags miscFlags, int loadFlags*/
        {
            if (!File.Exists(filename))
            {
                Log.Warning($"Couldn't find texture '{filename}'.");
                return (NULL_RESOURCE, NULL_RESOURCE);
            }

            if (_fileResources.TryGetValue(filename, out var existingFileResource))
            {
                uint textureId = existingFileResource.ResourceIds.First();
                existingFileResource.FileChangeAction += fileChangeAction;
                uint srvId = (from srvResourceEntry in _shaderResourceViews
                              where srvResourceEntry.TextureId == textureId
                              select srvResourceEntry.Id).Single();
                return (textureId, srvId);
            }

            Texture2D texture = null;
            CreateTexture(filename, ref texture);
            string name = Path.GetFileName(filename);
            var textureResourceEntry = new TextureResource(GetNextResourceId(), name, texture);
            Resources.Add(textureResourceEntry.Id, textureResourceEntry);
            _textures.Add(textureResourceEntry);

            uint shaderResourceViewId = CreateShaderResourceView(textureResourceEntry.Id, name);

            var fileResource = new FileResource(filename, new[] { textureResourceEntry.Id, shaderResourceViewId });
            fileResource.FileChangeAction += fileChangeAction;
            _fileResources.Add(filename, fileResource);

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
                    texture = new Texture2D(Device, description);
                }

                // new texture, create resource entry
                textureResource = new TextureResource(GetNextResourceId(), name, texture);
                Resources.Add(textureResource.Id, textureResource);
                _textures.Add(textureResource);
            }
            else
            {
                textureResource.Texture?.Dispose();
                textureResource.Texture = new Texture2D(Device, description);
                texture = textureResource.Texture;
            }

            id = textureResource.Id;

            return true;
        }

        private readonly Stopwatch _operatorUpdateStopwatch = new Stopwatch();
        private readonly List<Symbol> _modifiedSymbols = new List<Symbol>();

        public List<Symbol> UpdateChangedOperatorTypes()
        {
            _modifiedSymbols.Clear();
            for (int i = 0; i < _operators.Count; i++)
            {
                if (!_operators[i].Updated)
                    continue;

                var opResource = _operators[i]; // must be after if to prevent huge amount of allocations
                Type type = opResource.OperatorAssembly.ExportedTypes.FirstOrDefault();
                if (type == null)
                {
                    Log.Error("Error updateable operator had not exported type");
                    continue;
                }

                var symbolEntry = SymbolRegistry.Entries.FirstOrDefault(e => e.Value.Id.ToString() == opResource.Name);
                if (symbolEntry.Value != null)
                {
                    _operatorUpdateStopwatch.Restart();
                    symbolEntry.Value.SetInstanceType(type);
                    opResource.Updated = false;
                    _operatorUpdateStopwatch.Stop();
                    Log.Info($"type updating took: {(double)_operatorUpdateStopwatch.ElapsedTicks / Stopwatch.Frequency}s");
                    _modifiedSymbols.Add(symbolEntry.Value);
                }
                else
                {
                    Log.Info($"Error replacing symbol type '{opResource.Name}");
                }
            }

            return _modifiedSymbols;
        }

        public readonly Dictionary<uint, Resource> Resources = new Dictionary<uint, Resource>();

        /// <summary>Maps full filepath to FileResource</summary>
        private readonly Dictionary<string, FileResource> _fileResources = new Dictionary<string, FileResource>();

        private readonly List<VertexShaderResource> _vertexShaders = new List<VertexShaderResource>();
        private readonly List<PixelShaderResource> _pixelShaders = new List<PixelShaderResource>();
        private readonly List<ComputeShaderResource> _computeShaders = new List<ComputeShaderResource>();
        private readonly List<TextureResource> _textures = new List<TextureResource>();
        private readonly List<ShaderResourceViewResource> _shaderResourceViews = new List<ShaderResourceViewResource>();
        private readonly List<OperatorResource> _operators = new List<OperatorResource>(100);
        private readonly FileSystemWatcher _hlslFileWatcher;
        private readonly FileSystemWatcher _pngFileWatcher;
        private readonly FileSystemWatcher _jpgFileWatcher;
        private readonly FileSystemWatcher _ddsFileWatcher;
        private readonly FileSystemWatcher _tiffFileWatcher;
        private readonly FileSystemWatcher _csFileWatcher;

        public readonly Device Device;
        public const string ResourcesFolder = @"Resources";
    }
}