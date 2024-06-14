#nullable enable
using System;
using SharpDX.D3DCompiler;
using T3.Core.DataTypes;
using T3.Core.Logging;

namespace T3.Core.Resource;

public abstract partial class ShaderCompiler
{
    private static ShaderCompiler? _instance;

    public static ShaderCompiler Instance
    {
        get => _instance!;
        set
        {
            if (_instance != null)
            {
                throw new InvalidOperationException($"Can't set {nameof(ShaderCompiler)}.{nameof(Instance)} twice.");
            }

            _instance = value;
        }
    }

    protected abstract bool CompileShaderFromSource<TShader>(ShaderCompilationArgs args, out byte[] blob, out string errorMessage)
        where TShader : class;

    protected abstract void CreateShaderInstance<TShader>(string name, in byte[] blob, out TShader shader)
        where TShader : class;
}