using System.Linq;
using System.Text.RegularExpressions;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace T3.Core.Resource;

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

public abstract class ShaderResource : AbstractResource
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

    public static string ExtractMeaningfulShaderErrorMessage(string message)
    {
        var t = new Regex(@"(.*?)\((.*)\):(.*)");

        var shaderErrorMatch = t.Match(message);
        if (!shaderErrorMatch.Success)
            return message;

        var shaderName = shaderErrorMatch.Groups[1].Value;
        var lineNumber = shaderErrorMatch.Groups[2].Value;
        var errorMessage = shaderErrorMatch.Groups[3].Value;

        errorMessage = Enumerable.First<string>(errorMessage.Split('\n'));
        return $"Line {lineNumber}: {errorMessage}\n\n{shaderName}";
    }

    /// <summary>
    /// Matches errors like....
    ///
    /// Failed to compile shader 'ComputeWobble': C:\Users\pixtur\coding\t3\Resources\compute-ColorGrade.hlsl(32,12-56): warning X3206: implicit truncation of vector type
    /// </summary>
    private static readonly Regex ShaderErrorPattern = new(@".*?\((.*)\):(.*)");
}

public class PixelShaderResource : ShaderResource
{
    public PixelShaderResource(uint id, string name, string entryPoint, ShaderBytecode blob, PixelShader pixelShader)
        : base(id, name, entryPoint, blob)
    {
        PixelShader = pixelShader;
    }

    public PixelShader PixelShader;

    public virtual bool UpdateFromFile(string path)
    {
        if (UpToDate)
            return true;

        var success = ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "ps_5_0", ref PixelShader, ref _blob);
        UpToDate = true;
        return success;
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

    public bool UpdateFromFile(string path)
    {
        if (UpToDate)
            return true;

        var success =ResourceManager.CompileShaderFromFile(path, EntryPoint, Name, "cs_5_0", ref ComputeShader, ref _blob);
        UpToDate = true;
        return success;
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

public class Texture2dResource : AbstractResource
{
    public Texture2dResource(uint id, string name, Texture2D texture)
        : base(id, name)
    {
        Texture = texture;
    }

    public Texture2D Texture;
}

public class Texture3dResource : AbstractResource
{
    public Texture3dResource(uint id, string name, Texture3D texture)
        : base(id, name)
    {
        Texture = texture;
    }

    public Texture3D Texture;
}

public class ShaderResourceViewResource : AbstractResource
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