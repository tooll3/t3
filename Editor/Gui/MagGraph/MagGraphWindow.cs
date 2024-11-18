using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.MagGraph.Ui;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.MagGraph;

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
            _rootInstance = GraphWindow.Composition.GetFor(focusedCompositionOp!);
            _structure = new Structure(() => _rootInstance.Instance);
            var navigationHistory = new NavigationHistory(_structure);
            var nodeSelection = new NodeSelection(navigationHistory, _structure);
            _magGraphCanvas = new MagGraphCanvas(this, nodeSelection);
        }
        
        _magGraphCanvas?.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return [];
    }

    internal Instance CompositionOp;

    private MagGraphCanvas _magGraphCanvas;
    private GraphWindow.Composition _rootInstance;
    private Structure _structure;
}