using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using T3.Core.Logging;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

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