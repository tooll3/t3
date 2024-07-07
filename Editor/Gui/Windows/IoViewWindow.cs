using T3.Core.DataTypes.DataSet;
using T3.Editor.Gui.OutputUi;

// ReSharper disable PossibleMultipleEnumeration

namespace T3.Editor.Gui.Windows;

public class IoViewWindow : Window
{
    public IoViewWindow()
    {
        Config.Title = "IO Events";
    }

    protected override void DrawContent()
    {
        _canvas.Draw(DataRecording.ActiveRecordingSet);
    }

    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    private readonly DataSetViewCanvas _canvas = new();
}