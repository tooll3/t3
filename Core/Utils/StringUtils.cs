using System;

namespace T3.Core.Utils;

public static class StringUtils
{
    public static bool Equals(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase)
    {
        var aLength = a.Length;
        if (aLength != b.Length)
            return false;

        if (ignoreCase)
        {
            for (var i = 0; i < aLength; i++)
            {
                if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i]))
                    return false;
            }
        }
        else
        {
            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits the provided path by the directory separator character.
    /// Warning: not for use with windows-style absolute paths (e.g. C:\foo\bar or C:/foo/bar),
    /// though unix-style absolute paths will work (e.g. /foo/bar)
    /// </summary>
    /// <param name="path">The path to split</param>
    /// <param name="ranges">Ranges that can be used to create spans or substrings from the original path</param>
    /// <returns></returns>
    public static int SplitByDirectory(ReadOnlySpan<char> path, Span<Range> ranges)
    {
        int count = 0;
        int start = 0;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '\\' || path[i] == '/')
            {
                if (i > start)
                {
                    ranges[count++] = new Range(start, i);
                }

                start = i + 1;
            }
        }

        if (path.Length > start)
        {
            ranges[count++] = new Range(start, path.Length);
        }

        return count;
    }
}