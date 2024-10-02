using System;
using System.Linq;
using System.Text.RegularExpressions;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using T3.Core.DataTypes.Vector;

namespace T3.Core.Resource;

public abstract class ShaderResource : AbstractResource
{
    public static string ExtractMeaningfulShaderErrorMessage(string message)
    {
        var shaderErrorMatch = ShaderErrorPattern.Match(message);
        if (!shaderErrorMatch.Success)
            return message;

        var shaderName = shaderErrorMatch.Groups[1].Value;
        var lineNumber = shaderErrorMatch.Groups[2].Value;
        var errorMessage = shaderErrorMatch.Groups[3].Value;

        errorMessage = errorMessage.Split('\n').First();
        return $"Line {lineNumber}: {errorMessage}\n\n{shaderName}";
    }
    
    /// <summary>
    /// Matches errors like....
    ///
    /// Failed to compile shader 'ComputeWobble': C:\Users\pixtur\coding\t3\Resources\compute-ColorGrade.hlsl(32,12-56): warning X3206: implicit truncation of vector type
    /// </summary>
    private static readonly Regex ShaderErrorPattern = new(@"(.*?)\((.*)\):(.*)");
}

public class ShaderResource<T> : ShaderResource where T : class, IDisposable
{
    private T _shader;
    public T Shader { get => _shader; init => _shader = value; }

    private ShaderBytecode _blob;
    public ShaderBytecode Blob {get => _blob; init => _blob = value; }
    
    private string _entryPoint;
    public string EntryPoint { get => _entryPoint; init => _entryPoint = value; }

    public void UpdateDebugName(string newDebugName) => UpdateName(newDebugName);

    public bool TryUpdateFromFile(string path, string entryPoint, out string errorMessage)
    {
        var success = ShaderCompiler.Instance.TryCompileShaderFromFile(path, entryPoint, Name, ref _shader, ref _blob, out errorMessage);
        if(success)
            _entryPoint = entryPoint;
        return success;
    }
    
    public bool TryUpdateFromSource(string source, string entryPoint, out string errorMessage)
    {
        var success = ShaderCompiler.Instance.TryCompileShaderFromSource(source, entryPoint, Name, ref _shader, ref _blob, out errorMessage);
        if(success)
            _entryPoint = entryPoint;
        return success;
    }
}

public static class Extensions
{
    public static bool TryGetThreadGroups(this ShaderResource<ComputeShader> computeShader, out Int3 threadGroups)
    {
        
        threadGroups = default;
        if (computeShader.Blob == null)
            return false;

        var reflection = new ShaderReflection(computeShader.Blob);
        _ = reflection.GetThreadGroupSize(out var x, out var y, out var z);

        threadGroups = new Int3(x, y, z);
        return true;
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