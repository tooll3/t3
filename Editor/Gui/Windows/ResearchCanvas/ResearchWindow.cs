using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.UiModel;

using T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

internal sealed class ResearchWindow : Window
{
    public ResearchWindow(EditorSymbolPackage package, int instanceNumber, Instance rootInstance)
    {
        Config.Title = "Research";
        MenuTitle = "Research";
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        
        RootInstance = GraphWindow.Composition.GetFor(rootInstance!);
        Structure = new Structure(() => RootInstance.Instance);
        var navigationHistory = new NavigationHistory(Structure);
        var nodeSelection = new NodeSelection(navigationHistory, Structure);
        _snapGraphCanvas = new SnapGraphCanvas(this, nodeSelection);
    }

    protected override void DrawContent()
    {
        _snapGraphCanvas.Draw();
    }
    
    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    private GraphWindow.Composition _composition;
    internal Instance CompositionOp => _composition.Instance;

    
    private readonly SnapGraphCanvas _snapGraphCanvas;
    internal GraphWindow.Composition RootInstance { get; private set; }
    public readonly Structure Structure;
}