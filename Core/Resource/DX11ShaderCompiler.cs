using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using T3.Core.Logging;

namespace T3.Core.Resource;

/// <summary>
/// An implementation of the <see cref="ShaderCompiler"/> class that uses the DirectX 11 shader compiler from SharpDX
/// </summary>
public class DX11ShaderCompiler : ShaderCompiler
{
    public Device Device { get; set; }

    protected override bool CompileShaderFromSource<TShader>(ShaderCompilationArgs args, out ShaderBytecode blob, out string errorMessage)
    {
        CompilationResult compilationResult = null;
        string resultMessage;
        bool success;
        var profile = ShaderProfiles[typeof(TShader)];
        try
        {
            ShaderFlags flags = ShaderFlags.None;
            #if DEBUG || FORCE_SHADER_DEBUG
            flags |= ShaderFlags.Debug;
            #endif

            compilationResult = ShaderBytecode.Compile(args.SourceCode, args.EntryPoint, profile, flags, EffectFlags.None, null, new IncludeHandler(args.IncludeDirectories));

            success = compilationResult.ResultCode == Result.Ok;
            resultMessage = compilationResult.Message;
        }
        catch (Exception ce)
        {
            success = false;
            resultMessage = ce.Message;
        }

        if (success)
        {
            blob = compilationResult.Bytecode;
            errorMessage = string.Empty;
        }
        else
        {
            resultMessage = ShaderResource.ExtractMeaningfulShaderErrorMessage(resultMessage);
            errorMessage = resultMessage;
            blob = null;
        }

        return success;
    }
    
    protected override void CreateShaderInstance<TShader>(string name, in ShaderBytecode blob, out TShader shader)
    {
        // As shader type is generic we've to use Activator and PropertyInfo to create/set the shader object
        var shaderType = typeof(TShader);

        shader = (TShader)ShaderConstructors[shaderType].Invoke(Device, blob.Data);
        
        var debugNameInfo = shaderType.GetProperty("DebugName");
        debugNameInfo?.SetValue(shader, name);
    }

    private static readonly IReadOnlyDictionary<Type, Func<Device, byte[], object> > ShaderConstructors = new Dictionary<Type, Func<Device, byte[], object>>()
                                                                                   {
                                                                                       { typeof(VertexShader), (device, data) => new VertexShader(device, data, null) },
                                                                                       { typeof(PixelShader), (device, data) => new PixelShader(device, data, null) },
                                                                                       { typeof(ComputeShader), (device, data) => new ComputeShader(device, data, null) },
                                                                                       { typeof(GeometryShader), (device, data) => new GeometryShader(device, data, null) },
                                                                                   };
    
    private static readonly IReadOnlyDictionary<Type, string> ShaderProfiles = new Dictionary<Type, string>()
                                                                                   {
                                                                                       { typeof(VertexShader), "vs_5_0" },
                                                                                       { typeof(PixelShader), "ps_5_0" },
                                                                                       { typeof(ComputeShader), "cs_5_0" },
                                                                                       { typeof(GeometryShader), "gs_5_0" },
                                                                                   };

    private class IncludeHandler : SharpDX.D3DCompiler.Include
    {
        private StreamReader _streamReader;
        private readonly IReadOnlyList<IResourcePackage> _directories;
        
        public IncludeHandler(IReadOnlyList<IResourcePackage> directories)
        {
            _directories = directories;
        }

        public void Dispose()
        {
            _streamReader?.Dispose();
        }

        public IDisposable Shadow { get; set; }

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            if (ResourceManager.TryResolvePath(fileName, _directories, out var path, out _))
            {
                _streamReader = new StreamReader(path);
                return _streamReader.BaseStream;
            }

            Log.Error($"Could not locate include file '{fileName}'");
            _streamReader = null;
            return null;
        }

        public void Close(Stream stream)
        {
            _streamReader.Close();
        }
    }
}