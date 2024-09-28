#nullable enable
using Svg;

namespace Lib.Utils;

public static class SvgLoader
{
    public static bool TryLoad(FileResource file, SvgDocument? currentValue, out SvgDocument? newValue, out string? failurereason)
    {
        try
        {
            newValue = SvgDocument.Open<SvgDocument>(file.AbsolutePath, null);
            failurereason = null;
            return true;
        }
        catch (Exception e)
        {
            newValue = null;
            failurereason = $"Failed to load svg file:" + e.Message;
            return false;
        }
    }
}