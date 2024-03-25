using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX.D3DCompiler;
using T3.Core.Logging;
using T3.Core.SystemUi;

namespace T3.Core.Resource;

public abstract partial class ShaderCompiler
{
    public static void DeleteShaderCache()
    {
        int failures = 0;
        int total = 0;
        long totalFileSize = 0;

        lock (ShaderCacheLock)
        {
            Directory.EnumerateFiles(ShaderCacheRootPath)
                     .AsParallel()
                     .ForAll(file =>
                             {
                                 Interlocked.Increment(ref total);
                                 try
                                 {
                                     var fileInfo = new FileInfo(file);
                                     var fileSize = fileInfo.Length;
                                     fileInfo.Delete();
                                     Interlocked.Add(ref totalFileSize, fileSize);
                                 }
                                 catch (Exception e)
                                 {
                                     Log.Error($"Failed to delete shader cache file '{file}': {e.Message}");
                                     Interlocked.Increment(ref failures);
                                 }
                             });
        }

        var deletionsInKb = totalFileSize / 1024d;
        var deletionsString = deletionsInKb < 1024d
                                  ? $"{deletionsInKb:0.0} KB"
                                  : $"{deletionsInKb / 1024d:0.0} MB";
        
        var isError = failures > 0;

        string message;
        string title;
        if (isError)
        {
            message = $"Failed to delete {failures} out of {total} shader cache files.";
            title = "Shader Cache Deletion Failed";
        }
        else
        {
            message = string.Empty;
            title = "Shader Cache Deleted Successfully";
        }

        var finalMessage = $"Deleted {deletionsString} of shader cache from \"{ShaderCacheRootPath}\".\n{message}\n" +
                           $"Restart the application to refresh the removed cache, as all shaders still reside in memory.";
        CoreUi.Instance.ShowMessageBox(finalMessage, title);
    }

    private static void CacheSuccessfulCompilation(ShaderBytecode oldBytecode, long hash, ShaderBytecode newBytecode)
    {
        CacheShaderInMemory(oldBytecode, hash, newBytecode);
        SaveBytecodeToDisk(hash, newBytecode);
    }

    private static void SaveBytecodeToDisk(long hash, ShaderBytecode bytecode)
    {
        var path = GetPathForShaderCache(hash);
        File.WriteAllBytes(path, bytecode.Data);
    }

    private static bool TryLoadBytecodeFromDisk(long hash, [NotNullWhen(true)] out ShaderBytecode bytecode)
    {
        var path = GetPathForShaderCache(hash);
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            bytecode = null;
            return false;
        }

        var data = File.ReadAllBytes(path);
        bytecode = new ShaderBytecode(data);
        return true;
    }

    private static void CacheShaderInMemory(ShaderBytecode oldBytecode, long hash, ShaderBytecode newBytecode)
    {
        if (oldBytecode != null)
        {
            if (ShaderBytecodeHashes.Remove(oldBytecode, out var oldHash))
            {
                ShaderBytecodeCache.Remove(oldHash);
            }
        }

        ShaderBytecodeCache[hash] = newBytecode;
        ShaderBytecodeHashes[newBytecode] = hash;
    }

    private static string GetPathForShaderCache(long hashCode)
    {
        // ReSharper disable once StringLiteralTypo
        return Path.Combine(ShaderCacheRootPath, $"{hashCode}.shadercache");
    }

    private static readonly Dictionary<ShaderBytecode, long> ShaderBytecodeHashes = new();
    private static readonly Dictionary<long, ShaderBytecode> ShaderBytecodeCache = new();
    
    static ShaderCompiler()
    {
        Directory.CreateDirectory(ShaderCacheRootPath);
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct LongFromTwoInts(int a, int b)
    {
        [FieldOffset(0)]
        public readonly long Value = 0;
        
        [FieldOffset(0)]
        public readonly int A = a;
        
        [FieldOffset(4)]
        public readonly int B = b;
    }
    
    private static readonly object ShaderCacheLock = new();
    private static readonly string ShaderCacheRootPath = Path.Combine(UserData.UserData.TempFolder, "ShaderCache");
}