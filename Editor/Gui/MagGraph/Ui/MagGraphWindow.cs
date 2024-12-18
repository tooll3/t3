using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
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
        var info = GraphWindow.Focused?.Components;
        if (info == null)
            return;
        
        var focusedCompositionOp = info.CompositionOp;
        if (focusedCompositionOp == null)
            return;

        if (CompositionOp != focusedCompositionOp)
        {
            CompositionOp = focusedCompositionOp;
            var nodeSelection = info.NodeSelection;
            _graphImageBackground = new GraphImageBackground(nodeSelection, info.Structure);
            _magGraphCanvas = new MagGraphCanvas(this, nodeSelection, _graphImageBackground);
        }
        
        _graphImageBackground?.Draw(1);
        _magGraphCanvas?.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return [];
    }

    internal Instance CompositionOp;
    private MagGraphCanvas _magGraphCanvas;
    private GraphImageBackground _graphImageBackground;
}