using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace T3.Core
{
    public abstract class Resource
    {
        protected Resource(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; }
        public readonly string Name;
        public bool UpToDate { get; set; }
    }

    public abstract class ShaderResource : Resource
    {
        protected ShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob)
            : base(id, name)
        {
            EntryPoint = entryPoint;
            Blob = blob;
        }

        public string EntryPoint { get; }
        protected ShaderBytecode _blob;
        public ShaderBytecode Blob { get => _blob; private init => _blob = value; }
    }

    public class VertexShaderResource : ShaderResource
    {
        public VertexShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, VertexShader vertexShader)
            : base(id, name, entryPoint, blob)
        {
            VertexShader = vertexShader;
        }

        public void UpdateFromFile(string path)
        {
            if (UpToDate)
                return;

            ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "vs_5_0", ref VertexShader, ref _blob);
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

        public virtual void UpdateFromFile(string path)
        {
            if (UpToDate)
                return;

            ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "ps_5_0", ref PixelShader, ref _blob);
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

        public void UpdateFromFile(string path)
        {
            if (UpToDate)
                return;

            ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "cs_5_0", ref ComputeShader, ref _blob);
            UpToDate = true;
        }

        // public void UpdateFromSourceString(string source)
        // {
        //     ResourceManager.Instance().CompileShaderFromSource(source, EntryPoint, Name, "cs_5_0", ref ComputeShader, ref _blob);
        //     UpToDate = true;
        // }
    }

    public class GeometryShaderResource : ShaderResource
    {
        public GeometryShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, GeometryShader geometryShader) :
            base(id, name, entryPoint, blob)
        {
            GeometryShader = geometryShader;
        }

        public GeometryShader GeometryShader;

        public virtual void UpdateFromFile(string path)
        {
            if (UpToDate)
                return;

            ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "gs_5_0", ref GeometryShader, ref _blob);
            UpToDate = true;
        }
    }

    public class Texture2dResource : Resource
    {
        public Texture2dResource(uint id, string name, Texture2D texture)
            : base(id, name)
        {
            Texture = texture;
        }

        public Texture2D Texture;
    }

    public class Texture3dResource : Resource
    {
        public Texture3dResource(uint id, string name, Texture3D texture)
            : base(id, name)
        {
            Texture = texture;
        }

        public Texture3D Texture;
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
}