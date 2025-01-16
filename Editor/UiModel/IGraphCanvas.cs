#nullable enable
using ImGuiNET;
using T3.Editor.Gui.Graph.GraphUiModel;
using T3.Editor.Gui.Graph.Legacy.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.UiModel;

internal interface IGraphCanvas : IScalableCanvas
{
    bool Destroyed { get; set;  }
    SelectableNodeMovement SelectableNodeMovement { get; }

    void ApplyComposition(ICanvas.Transition transition, Guid compositionOpSymbolChildId);
    void FocusViewToSelection();
    void OpenAndFocusInstance(IReadOnlyList<Guid> path);
    public CanvasScope GetTargetScope();
    void BeginDraw(bool backgroundActive, bool bgHasInteractionFocus);
    void DrawGraph(ImDrawListPtr drawList, float graphOpacity);
    
    /// <summary>
    /// Should be active during actions like dragging a connection.
    /// </summary>
    bool HasActiveInteraction { get; }
    
    public GraphComponents Components { set; }
}