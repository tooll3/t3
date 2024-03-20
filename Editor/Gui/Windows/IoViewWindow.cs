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
        _canvas.Draw(T3Ui.MidiDataRecording.DataSet);
    }

    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    private readonly DataSetViewCanvas _canvas = new();
}