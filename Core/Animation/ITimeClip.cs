using T3.Gui.Windows.TimeLine;

namespace T3.Core.Animation
{
    public interface ITimeClip
    {
        TimeRange TimeRange { get; set; }
        TimeRange SourceRange { get; set; }
    }
}