using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.MagGraph.Ui;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.MagGraph;

internal sealed class MagGraphWindow : Window
{
    //public MagGraphWindow(EditorSymbolPackage package, int instanceNumber, Instance rootInstance)
    public MagGraphWindow()
    {
        Config.Title = "MagGraph";
        MenuTitle = "MagGraph";
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        //GraphWindow.Composition.GetFor();
        
        if (GraphWindow.Focused == null)
        {
            Log.Warning("Can't show magnetic graph window without focused graph window");
            return;
        }

        //var rootInstance = GraphWindow.Focused.CompositionOp;
        

    }

    //private Instance _compositionOp;
    
    protected override void DrawContent()
    {
        var focusedCompositionOp = GraphWindow.Focused?.CompositionOp;
        if (focusedCompositionOp == null)
            return;

        if (CompositionOp != focusedCompositionOp)
        {
            CompositionOp = focusedCompositionOp;
            RootInstance = GraphWindow.Composition.GetFor(focusedCompositionOp!);
            Structure = new Structure(() => RootInstance.Instance);
            var navigationHistory = new NavigationHistory(Structure);
            var nodeSelection = new NodeSelection(navigationHistory, Structure);
            _magGraphCanvas = new MagGraphCanvas(this, nodeSelection);
        }
        
        _magGraphCanvas?.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    //private GraphWindow.Composition _composition;
    //internal Instance CompositionOp => _composition?.Instance;
    internal Instance CompositionOp;
    
    private MagGraphCanvas _magGraphCanvas;
    internal GraphWindow.Composition RootInstance { get; private set; }
    public Structure Structure;
}