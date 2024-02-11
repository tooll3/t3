using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.D3DCompiler;
using T3.Core.Logging;

namespace T3.Core.Resource;

public abstract class ShaderCompiler
{
    private static ShaderCompiler _instance;

    public static ShaderCompiler Instance
    {
        get => _instance;
        set
        {
            if (_instance != null)
            {
                throw new InvalidOperationException($"Can't set {nameof(ShaderCompiler)}.{nameof(Instance)} twice.");
            }

            _instance = value;
        }
    }

    protected abstract bool CompileShaderFromSource<TShader>(string shaderSource, string entryPoint, string name, out ShaderBytecode blob,
                                                             out string errorMessage)
        where TShader : class, IDisposable;

    protected abstract void CreateShaderInstance<TShader>(string name, in ShaderBytecode blob, out TShader shader)
        where TShader : class, IDisposable;

    public bool TryCompileShaderFromSource<TShader>(string shaderSource, string entryPoint, string name, ref TShader shader, ref ShaderBytecode blob,
                                                    out string errorMessage)
        where TShader : class, IDisposable
    {
        if (string.IsNullOrWhiteSpace(entryPoint))
            entryPoint = "main";

        int hash = HashCode.Combine(shaderSource.GetHashCode(), entryPoint.GetHashCode());
        bool success;
        ShaderBytecode latestBlob;

        lock (_shaderBytecodeCacheLock)
        {
            if (_shaderBytecodeCache.TryGetValue(hash, out latestBlob))
            {
                success = true;
                errorMessage = string.Empty;
                Log.Debug($"Shader '{name}'({hash}) already compiled.");
            }
            else
            {
                success = CompileShaderFromSource<TShader>(shaderSource, entryPoint, name, out latestBlob, out errorMessage);

                if (success)
                {
                    Log.Debug($"Compiled shader '{name}'({hash}).");
                    if (blob != null)
                    {
                        if (_shaderBytecodeHashes.Remove(blob, out var oldHash))
                        {
                            _shaderBytecodeCache.Remove(oldHash);
                        }

                        blob.Dispose();
                        Log.Debug($"Disposing old shader '{name}'({oldHash}).");
                    }

                    _shaderBytecodeCache[hash] = latestBlob;
                    _shaderBytecodeHashes[latestBlob] = hash;
                }
                else
                {
                    Log.Error($"Failed to compile shader '{name}'.\n{errorMessage}");
                }
            }
        }

        if (!success)
            return false;

        blob = latestBlob;

        if (shader is IDisposable oldDisposableShader)
            oldDisposableShader.Dispose();

        CreateShaderInstance(name, blob, out shader);
        return true;
    }

    public bool TryCompileShaderFromFile<TShader>(string srcFile, string entryPoint, string name, ref TShader shader, ref ShaderBytecode blob,
                                                  out string errorMessage)
        where TShader : class, IDisposable
    {
        var file = new FileInfo(srcFile);
        if (!file.Exists)
        {
            errorMessage = $"Shader file '{srcFile}' not found.";
            Log.Error(errorMessage);
            return false;
        }

        string fileText;
        try
        {
            fileText = File.ReadAllText(srcFile);
        }
        catch (Exception e)
        {
            errorMessage = $"Failed to read shader file '{srcFile}'.\n{e.Message}";
            Log.Error(errorMessage);
            return false;
        }
        return TryCompileShaderFromSource(fileText, entryPoint, name, ref shader, ref blob, out errorMessage);
    }

    public bool TryCreateShaderResourceFromSource<TShader>(string shaderSource, string name, string entryPoint, uint resourceId,
                                                           out ShaderResource<TShader> resource, out string errorMessage)
        where TShader : class, IDisposable
    {
        TShader shader = null;
        ShaderBytecode blob = null;
        var success = TryCompileShaderFromSource(shaderSource, entryPoint, name, ref shader, ref blob, out errorMessage);
        if (!success)
        {
            Log.Info($"Failed to create {nameof(TShader)} '{name}'.");
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

    public bool TryCreateShaderResourceFromFile<TShader>(string srcFile, string name, string entryPoint, uint resourceId, out ShaderResource<TShader> resource,
                                                         out string errorMessage)
        where TShader : class, IDisposable
    {
        TShader shader = null;
        ShaderBytecode blob = null;
        var success = TryCompileShaderFromFile(srcFile, entryPoint, name, ref shader, ref blob, out errorMessage);
        if (!success)
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

    private readonly Dictionary<ShaderBytecode, int> _shaderBytecodeHashes = new();
    private readonly Dictionary<int, ShaderBytecode> _shaderBytecodeCache = new();
    readonly object _shaderBytecodeCacheLock = new();
}