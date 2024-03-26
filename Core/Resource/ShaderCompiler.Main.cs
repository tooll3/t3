using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.D3DCompiler;
using T3.Core.Logging;

namespace T3.Core.Resource;

public abstract partial class ShaderCompiler
{
    internal static bool TryCompileShaderFromSource<TShader>(ShaderCompilationArgs args, string name, ref TShader shader, ref ShaderBytecode blob, bool useCache,
                                                             bool forceRecompile, out string reason)
        where TShader : class, IDisposable
    {
        if (string.IsNullOrWhiteSpace(args.EntryPoint))
            args.EntryPoint = "main";

        var hashCombination = new ULongFromTwoInts(args.SourceCode.GetHashCode(), args.EntryPoint.GetHashCode());
        var hash = hashCombination.Value;
        bool success = false;
        ShaderBytecode compiledBlob;

        if (useCache)
        {
            lock (ShaderCacheLock)
            {
                if (!forceRecompile && TryLoadCached(hash, out compiledBlob, out reason))
                {
                    success = true;
                    reason = "loaded from cache";
                }
                else if (Instance.CompileShaderFromSource<TShader>(args, out compiledBlob, out reason))
                {
                    success = true;
                    CacheSuccessfulCompilation(blob, hash, compiledBlob);
                    reason = "compiled successfully";
                }
            }
        }
        else
        {
            if (Instance.CompileShaderFromSource<TShader>(args, out compiledBlob, out reason))
            {
                reason = "compiled successfully";
                success = true;
            }
        }

        if (success)
        {
            blob = compiledBlob;

            if (shader is IDisposable oldDisposableShader)
                oldDisposableShader.Dispose();

            Instance.CreateShaderInstance(name, compiledBlob!, out shader);
            Log.Debug($"{name} - {args.EntryPoint} {reason}");
            return true;
        }

        reason = $"Failed to compile shader '{name}'.\n{reason}";
        return false;

        static bool TryLoadCached(ulong hash, out ShaderBytecode compiledBlob, out string reason)
        {
            if (ShaderBytecodeCache.TryGetValue(hash, out compiledBlob))
            {
                reason = string.Empty;
                reason = "Shader already compiled.";
                return true;
            }

            if (TryLoadBytecodeFromDisk(hash, out compiledBlob))
            {
                reason = string.Empty;
                CacheShaderInMemory(compiledBlob, hash, compiledBlob);
                reason = $"Loaded cached shader from disk ({hash}).";
                return true;
            }

            reason = "No cached shader found.";
            return false;
        }
    }

    /// <summary>
    /// A simple method to prepare <see cref="ShaderCompilationArgs"/> before calling the actual shader compilation.
    /// </summary>
    internal static bool TryPrepareSourceFile(string srcFile, string entryPoint, IEnumerable<IResourcePackage> resourceDirs,
                                              out string errorMessage, out ShaderCompilationArgs args)
    {
        var file = new FileInfo(srcFile);
        if (!file.Exists)
        {
            errorMessage = $"Shader file '{srcFile}' not found.";
            Log.Error(errorMessage);
            args = default;
            return false;
        }

        List<IResourcePackage> directories =
            [
                new ShaderResourcePackage(file.DirectoryName!)
            ];

        if (resourceDirs != null)
            directories.AddRange(resourceDirs);

        try
        {
            var sourceCodeText = File.ReadAllText(srcFile);
            errorMessage = string.Empty;
            args = new ShaderCompilationArgs(sourceCodeText, entryPoint, directories);
            return true;
        }
        catch (Exception e)
        {
            errorMessage = $"Failed to read shader file '{srcFile}'.\n{e.Message}";
            Log.Error(errorMessage);
            args = default;
            return false;
        }
    }

    public record struct ShaderCompilationArgs(string SourceCode, string EntryPoint, IReadOnlyList<IResourcePackage> IncludeDirectories);

    public sealed class ShaderResourcePackage(string resourcesFolder) : IResourcePackage
    {
        public string ResourcesFolder { get; } = resourcesFolder;
        public ResourceFileWatcher FileWatcher => null;
        public bool IsReadOnly => true;
    }
}