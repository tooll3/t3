#nullable enable
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.UiModel;

internal interface IGraphCanvas : IScalableCanvas
{
    public SymbolBrowser SymbolBrowser { get; }
    bool Destroyed { get; }

    void ApplyComposition(ICanvas.Transition transition, Guid compositionOpSymbolChildId);
    void FocusViewToSelection();
    void OpenAndFocusInstance(IReadOnlyList<Guid> path);
    public CanvasScope GetTargetScope();
}