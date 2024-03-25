#nullable enable
using System;
using SharpDX.D3DCompiler;

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

    protected abstract bool CompileShaderFromSource<TShader>(ShaderCompilationArgs args, out ShaderBytecode blob, out string errorMessage)
        where TShader : class, IDisposable;

    protected abstract void CreateShaderInstance<TShader>(string name, in ShaderBytecode blob, out TShader shader)
        where TShader : class, IDisposable;
}