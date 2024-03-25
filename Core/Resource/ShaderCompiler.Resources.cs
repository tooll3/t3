using System;
using System.Collections.Generic;
using SharpDX.D3DCompiler;
using T3.Core.Logging;

namespace T3.Core.Resource;

public abstract partial class ShaderCompiler
{
    internal static bool TryCreateShaderResourceFromCode<TShader>(string shaderCode, string name, IReadOnlyList<IResourcePackage> directories,
                                                                  string entryPoint, uint resourceId, out string reason,
                                                                  out ShaderResource<TShader> resource)
        where TShader : class, IDisposable
    {
        TShader shader = null!;
        ShaderBytecode blob = null;
        var success = TryCompileShaderFromSource(args: new ShaderCompilationArgs(shaderCode, entryPoint, directories),
                                                 name: name,
                                                 shader: ref shader,
                                                 blob: ref blob!,
                                                 useCache: false,
                                                 forceRecompile: false,
                                                 reason: out reason);
        if (!success)
        {
            Log.Info($"Failed to create {nameof(TShader)} '{name}'.");
            resource = null!;
            return false;
        }

        resource = new ShaderResource<TShader>()
                       {
                           Id = resourceId,
                           Name = name,
                           Blob = blob,
                           Shader = shader,
                           EntryPoint = entryPoint
                       };

        return true;
    }

    internal static bool TryCreateShaderResourceFromFile<TShader>(string srcFile, string name, string entryPoint, uint resourceId, IReadOnlyList<IResourcePackage> resourceDirs,
                                                                  out ShaderResource<TShader> resource, out string reason, bool forceRecompile)
        where TShader : class, IDisposable
    {
        TShader shader = null!;
        ShaderBytecode blob = null;
        
        if (!TryPrepareSourceFile(srcFile, entryPoint, resourceDirs, out reason, out var args))
        {
            resource = null;
            Log.Error($"{name} -> {entryPoint}: {reason}");
            return false;
        }
        
        if(!TryCompileShaderFromSource(args, name, ref shader, ref blob, true, forceRecompile, out reason))
        {
            resource = null;
            return false;
        }

        resource = new ShaderResource<TShader>()
                       {
                           Id = resourceId,
                           Name = name,
                           Blob = blob,
                           Shader = shader,
                           EntryPoint = entryPoint
                       };
        return true;
    }
}