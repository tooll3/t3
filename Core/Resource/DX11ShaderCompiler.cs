using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using T3.Core.Logging;

namespace T3.Core.Resource;

public class DX11ShaderCompiler : ShaderCompiler
{
    public Device Device { get; set; }

    protected override bool CompileShaderFromSource<TShader>(string shaderSource, IReadOnlyList<string> directories, string entryPoint, string name, out ShaderBytecode blob,
                                                             out string errorMessage)
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

            compilationResult = ShaderBytecode.Compile(shaderSource, entryPoint, profile, flags, EffectFlags.None, null, new IncludeHandler(directories));

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
            resultMessage = $"Failed to compile shader '{name}'.\n{resultMessage}";
            errorMessage = resultMessage;
            blob = null;
        }

        return success;
    }

    protected override void CreateShaderInstance<TShader>(string name, in ShaderBytecode blob, out TShader shader)
    {
        // As shader type is generic we've to use Activator and PropertyInfo to create/set the shader object
        var shaderType = typeof(TShader);

        // todo - optimize and cache constructors similar to AssemblyInformation.cs and Symbol.OutputDefinition
        shader = (TShader)Activator.CreateInstance(shaderType, Device, blob.Data, null);

        var debugNameInfo = shaderType.GetProperty("DebugName");
        debugNameInfo?.SetValue(shader, name);
    }

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
        private readonly IReadOnlyList<string> _directories;
        
        public IncludeHandler(IReadOnlyList<string> directories)
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
            if (ResourceManager.TryResolvePath(fileName, out var path, _directories))
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