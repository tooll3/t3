using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.MagGraph.Ui;

// UX notes:
// It's strange when snapping two operators into two inputs of another operator, that one can end up overlapping the other
// 
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
        var focusedCompositionOp = GraphWindow.Focused?.CompositionOp;
        if (focusedCompositionOp == null)
            return;

        if (CompositionOp != focusedCompositionOp)
        {
            CompositionOp = focusedCompositionOp;
            
            // should the previous canvas be "disposed"?
            // we can manage the lifetime of our compositions this way
            _magGraphCanvas = new MagGraphCanvas(this, GraphWindow.Focused.GraphCanvas.NodeSelection);
        }
        
        _magGraphCanvas?.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return [];
    }

    internal Instance CompositionOp;
    private MagGraphCanvas _magGraphCanvas;
}