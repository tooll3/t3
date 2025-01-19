using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel.ProjectHandling;

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
        // var graphComponents = ProjectManager.Components;
        //
        // var legacyFocusedCompositionOp = graphComponents?.CompositionOp;
        // if (legacyFocusedCompositionOp == null)
        //     return;
        //
        // if (_windowCompositionOp != legacyFocusedCompositionOp)
        // {
        //     _windowCompositionOp = legacyFocusedCompositionOp;
        //     var nodeSelection = graphComponents.NodeSelection;
        //     _graphImageBackground = new GraphImageBackground(nodeSelection, graphComponents.Structure);
        //     _magGraphCanvas = new MagGraphCanvas(_windowCompositionOp, nodeSelection, _graphImageBackground);
        // }
        //
        // _graphImageBackground?.Draw(1);
        // _magGraphCanvas?.Draw();
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }

    // private Instance _windowCompositionOp;
    // private MagGraphCanvas _magGraphCanvas;
    // private GraphImageBackground _graphImageBackground;
}