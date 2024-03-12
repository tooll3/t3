#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;

namespace T3.Core.Resource;

public partial class ResourceManager
{
    private static bool RelativePathBackwardsCompatibility(string relativePath, out bool isAbsolute, out List<Range>? backCompatPaths)
    {
        backCompatPaths = null;
        if (Path.IsPathRooted(relativePath))
        {
            Log.Warning($"Path '{relativePath}' is not relative. This is deprecated and should be relative to the project Resources folder as " +
                        $"live updates will not occur. Please update your project settings.");
            isAbsolute = true;
            return false;
        }

        isAbsolute = false;

        var pathSpan = relativePath.AsSpan();

        var subfolderRangeArray = _rangeArrayPool.Get();
        var wholeSubfolderSpan = subfolderRangeArray.AsSpan();
        var subfolderCount = StringUtils.SplitByDirectory(pathSpan, wholeSubfolderSpan);

        if (subfolderCount <= 0)
        {
            throw new Exception("This should not happen, even if there are no subfolders.");
        }

        var subfolderRanges = subfolderRangeArray.AsSpan(0, subfolderCount);

        List<Range>? backCompatRanges = null;

        // test for shaders that reference using lib/etc
        int index = 0;
        if (AddNextRangeIfMatch("lib", pathSpan, subfolderRanges))
        {
            backCompatPaths = backCompatRanges;
            return true;
        }
        
        // remove "resources" prefix
        if (AddNextRangeIfMatch("resources", pathSpan, subfolderRanges))
        {
            index++;
        }
        else // if there is no "resources" prefix, we do not need to apply any back-compat rules
        {
            return false;
        }

        // test again for "Resources/lib" references
        if (AddNextRangeIfMatch("lib", pathSpan, subfolderRanges))
            index++;

        // "t3/Resources/common" has been flattened into "t3/Resources"
        if(AddNextRangeIfMatch("common", pathSpan, subfolderRanges))
            index++;

        if (AddNextRangeIfMatch("user", pathSpan, subfolderRanges))
        {
            // "user" subfolders expected an additional subfolder after them
            // e.g. "t3/Resources/user/pixtur/shaders"
            // so we should remove this as it is not needed and may be incorrect
            backCompatRanges!.RemoveAt(backCompatRanges.Count - 1);

            // remove username subfolder
            index++;
            AddNextRangeIfMatch("*", pathSpan, subfolderRanges);
        }
        
        backCompatPaths = backCompatRanges;
        ReturnPooledArray();
        return true;

        void ReturnPooledArray() => _rangeArrayPool.Return(subfolderRangeArray, false);

        static bool TryCreateRange(int startIndex, int endIndex, ReadOnlySpan<char> path,out Range range)
        {
            if (startIndex < endIndex && endIndex <= path.Length)
            {
                range = new Range(startIndex, endIndex);
                return true;
            }

            range = default;
            return false;
        }

        bool AddNextRangeIfMatch(string testString, ReadOnlySpan<char> pathSpan, Span<Range> subfolderRanges)
        {
            if(index >= subfolderRanges.Length)
                return false;
            
            var range = subfolderRanges[index];
            var subfolder = pathSpan[range];
            var nextStartIndex = range.End.Value + 1;
        
            if (testString == "*" || StringUtils.Equals(subfolder, testString, true))
            {
                if (TryCreateRange(nextStartIndex, pathSpan.Length, pathSpan, out var newRange))
                {
                    backCompatRanges ??= new List<Range>();
                    backCompatRanges.Add(newRange);
                    return true;
                }
            }

            return false;
        }
    }

    static IReadOnlyList<string> PopulateBackCompatPaths(string original, List<Range>? backCompatRanges)
    {
        if (backCompatRanges is null or { Count: 0 })
            return Array.Empty<string>();

        var list = new List<string>(backCompatRanges!.Count);
        foreach (var range in backCompatRanges)
        {
            list.Add(original[range]);
        }

        return list;
    }

    private static void LogFailedResourceLocation(string searchPath, Instance? instance, IEnumerable<string> usedResourceFolders)
    {
        string instanceFolders;
        if (instance != null)
        {
            instanceFolders = $"{instance.Symbol.Name}: Instance ID {instance.SymbolChildId}, Symbol ID {instance.Symbol.Id}\n" +
                              string.Join("\n", instance.AvailableResourceFolders.Concat(usedResourceFolders).Distinct());
        }
        else
        {
            instanceFolders = string.Join("\n", usedResourceFolders);
        }

        var searchPathDebug = $"\"{searchPath}\"";
        if (RelativePathBackwardsCompatibility(searchPath, out _, out var ranges))
        {
            var paths = PopulateBackCompatPaths(searchPath, ranges);
            if (paths.Count > 0)
            {
                var backCompatPathList = string.Join('\n', paths);
                searchPathDebug += $" (backcompat: {backCompatPathList})\n";
            }
        }

        Log.Debug($"Failed to locate resource {searchPathDebug} in available resource folders:\n" + instanceFolders);
    }

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static Utils.ObjectPooling.ArrayPool<Range> _rangeArrayPool = new(true, 30);
}