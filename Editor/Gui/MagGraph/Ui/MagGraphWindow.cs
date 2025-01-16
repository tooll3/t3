using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Legacy;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed class MagGraphWindow : Window
{
    public MagGraphWindow()
    {
        Config.Title = "MagGraph";
        MenuTitle = "Magnetic Graph View";
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        
    }

    
    protected override void DrawContent()
    {
        var focused = GraphWindow.Focused?.Components;
        if (focused == null)
            return;
        
        var legacyFocusedCompositionOp = focused.CompositionOp;
        if (legacyFocusedCompositionOp == null)
            return;

        if (_windowCompositionOp != legacyFocusedCompositionOp)
        {
            _windowCompositionOp = legacyFocusedCompositionOp;
            var nodeSelection = focused.NodeSelection;
            _graphImageBackground = new GraphImageBackground(nodeSelection, focused.Structure);
            _magGraphCanvas = new MagGraphCanvas(this,  _windowCompositionOp, nodeSelection, _graphImageBackground);
        }
        
        _graphImageBackground?.Draw(1);
        _magGraphCanvas?.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return [];
    }

    private Instance _windowCompositionOp;
    private MagGraphCanvas _magGraphCanvas;
    private GraphImageBackground _graphImageBackground;
}