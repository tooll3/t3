#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using T3.Core.Logging;

namespace T3.Core.Resource;

public sealed partial class FileResource
{
    public bool TryLoadBytes(ref byte[]? buffer, out Range range, [NotNullWhen(false)] out string? reason)
    {
        var path = AbsolutePath;
        if (!TryInitLoad(ref buffer, out reason, path))
        {
            range = default;
            return false;
        }
        
        try
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var count = fileStream.Read(buffer, 0, buffer.Length);
            range = new Range(0, count);
            return true;
        }
        catch (Exception e)
        {
            reason = e.ToString();
            range = default;
            return false;
        }
    }
    
    public async Task<AsyncLoadResult> TryLoadBytesAsync()
    {
        byte[]? buffer = null;
        
        if (!TryInitLoad(ref buffer, out var reason, AbsolutePath))
        {
            return new AsyncLoadResult(false, null, reason);
        }
        
        int readCount;
        try
        {
            await using var fileStream = new FileStream(AbsolutePath, FileMode.Open, FileAccess.Read);
            readCount = await fileStream.ReadAsync(buffer);
        }
        catch (Exception e)
        {
            return new AsyncLoadResult(false, buffer, e.ToString());
        }
        
        if (readCount != buffer.Length)
        {
            Log.Warning($"Read fewer bytes than expected from file {AbsolutePath}");
        }
        
        return new AsyncLoadResult(true, buffer, null);
    }
    
    private static bool TryInitLoad([NotNullWhen(true)] ref byte[]? buffer, [NotNullWhen(false)] out string? reason, string path)
    {
        long sizeBytes;
        
        try
        {
            var fileInfo = new FileInfo(path);
            
            if (!fileInfo.Exists)
            {
                reason = $"File not found: \"{path}\"";
                return false;
            }
            
            sizeBytes = fileInfo.Length;
        }
        catch (Exception e)
        {
            reason = e.ToString();
            return false;
        }
        
        if (buffer == null || buffer.Length < sizeBytes)
        {
            buffer = new byte[sizeBytes];
        }
        
        reason = null;
        return true;
    }
    
    public bool TryOpenFileStream([NotNullWhen(true)] out FileStream? fileStream, [NotNullWhen(false)] out string? reason, FileAccess access,
                                  FileMode mode = FileMode.Open)
    {
        try
        {
            fileStream = new FileStream(AbsolutePath, mode, access);
            reason = null;
            return true;
        }
        catch (Exception e)
        {
            reason = e.ToString();
            fileStream = null;
            return false;
        }
    }
    
    /// <summary>
    /// Async load result
    /// </summary>
    /// <param name="Success"></param>
    /// <param name="Data">Likely null if success is false, otherwise true</param>
    /// <param name="Reason">Reason for failure - null if succeeded</param>
    public readonly record struct AsyncLoadResult(bool Success, byte[]? Data, string? Reason);
}