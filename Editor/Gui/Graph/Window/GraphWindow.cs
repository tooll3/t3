#nullable enable
using ImGuiNET;
using T3.Core.Animation;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Legacy.Interaction.Connections;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Window;

/// <summary>
/// A window that renders a node graph 
/// </summary>
internal sealed partial class GraphWindow : Windows.Window
{

    protected override string WindowDisplayTitle { get; }
    private bool _focusOnNextFrame;

    private void FocusRequestedHandler()
    {
        TakeFocus();
        _focusOnNextFrame = true;
    }

    protected override void DrawContent()
    {
        if (FitViewToSelectionHandling.FitViewToSelectionRequested)
            GraphCanvas.FocusViewToSelection();
            
        Components.OpenedProject.RefreshRootInstance();
        
        if (Components.Composition == null)
            return;

        ImageBackgroundFading.HandleImageBackgroundFading(Components.GraphImageBackground, out var backgroundImageOpacity);

        Components.GraphImageBackground.Draw(backgroundImageOpacity);

        ImGui.SetCursorPos(Vector2.Zero);

        var graphHiddenWhileInteractiveWithBackground = Components.GraphImageBackground.IsActive && TransformGizmoHandling.IsDragging;
        if (graphHiddenWhileInteractiveWithBackground)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var windowContentHeight = (int)ImGui.GetWindowHeight();

        if (UserSettings.Config.ShowTimeline)
        {
            Components.TimeLineCanvas.Folding.DrawSplit(out windowContentHeight);
        }

        ImGui.BeginChild("##graph", new Vector2(0, windowContentHeight), false,
                         ImGuiWindowFlags.NoScrollbar
                         | ImGuiWindowFlags.NoMove
                         | ImGuiWindowFlags.NoScrollWithMouse
                         | ImGuiWindowFlags.NoDecoration
                         | ImGuiWindowFlags.NoTitleBar
                         | ImGuiWindowFlags.NoBackground
                         | ImGuiWindowFlags.ChildWindow);
        {
            if (_focusOnNextFrame)
            {
                ImGui.SetWindowFocus();
                _focusOnNextFrame = false;
            }

            ImGui.SetScrollX(0);

            drawList.ChannelsSplit(2);
            
            // Draw Foreground content first
            drawList.ChannelsSetCurrent(1);
            {
                if (UserSettings.Config.ShowTitleAndDescription)
                    GraphTitleAndBreadCrumbs.Draw(Components);

                UiElements.DrawProjectControlToolbar(Components);
            }

            // Draw content
            drawList.ChannelsSetCurrent(0);
            {
                ImageBackgroundFading.HandleGraphFading(Components.GraphImageBackground, drawList, out var graphOpacity);

                var isGraphHidden = graphOpacity <= 0;
                if (!isGraphHidden)
                {
                    GraphCanvas.BeginDraw(Components.GraphImageBackground.IsActive, 
                                          Components.GraphImageBackground.HasInteractionFocus);

                    /*
                     * This is a work around to delay setting the composition until ImGui has
                     * finally updated its window size and applied its layout so we can use
                     * Graph window size to properly fit the content into view.
                     *
                     * The side effect of this hack is that CompositionOp stays undefined for
                     * multiple frames with requires many checks in GraphWindow's Draw().
                     */
                    if (!_initializedAfterLayoutReady && ImGui.GetFrameCount() > 1)
                    {
                        GraphCanvas.ApplyComposition(ICanvas.Transition.JumpIn, Components.OpenedProject.RootInstance.SymbolChildId);
                        GraphCanvas.FocusViewToSelection();
                        _initializedAfterLayoutReady = true;
                    }

                    GraphBookmarkNavigation.HandleForCanvas(Components);

                    ImGui.BeginGroup();
                    ImGui.SetScrollY(0);
                    CustomComponents.DrawWindowFocusFrame();
                    if (ImGui.IsWindowFocused())
                        TakeFocus();
                    
                    GraphCanvas.DrawGraph(drawList, graphOpacity);
                    
                    ImGui.EndGroup();

                    ParameterPopUp.DrawParameterPopUp(Components);
                }
            }
            drawList.ChannelsMerge();

            _editDescriptionDialog.Draw(Components.Composition.Symbol);
        }
        ImGui.EndChild();

        Components.OpenedProject.RefreshRootInstance(); // Why is this called again?
        
        if (UserSettings.Config.ShowTimeline)
        {
            const int splitterWidth = 3;
            var availableRestHeight = ImGui.GetContentRegionAvail().Y;
            if (availableRestHeight <= splitterWidth)
            {
                //Log.Warning($"skipping rending of timeline because layout is inconsistent: only {availableRestHeight}px left.");
            }
            else
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + splitterWidth - 1); // leave gap for splitter
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0);

                ImGui.BeginChild("##timeline", Vector2.Zero, false,
                                 ImGuiWindowFlags.NoResize
                                 | ImGuiWindowFlags.NoBackground
                                );
                {
                    Components.TimeLineCanvas.Draw(Components.CompositionOp, Playback.Current);
                }
                ImGui.EndChild();
                ImGui.PopStyleVar(1);
            }
        }

        if (UserSettings.Config.ShowMiniMap)
            UiElements.DrawMiniMap(Components.Composition, GraphCanvas);

        Components.CheckDisposal();
    }

    protected override void Close()
    {
        ConnectionMaker.RemoveWindow(GraphCanvas);
        GraphWindow.GraphWindowInstances.Remove(this);
        if (GraphWindow.Focused == this)
            GraphWindow.Focused = GraphWindow.GraphWindowInstances.FirstOrDefault();
            
        OnWindowDestroyed?.Invoke(this, Components.OpenedProject.Package);
    }

    protected override void AddAnotherInstance()
    {
        GraphWindow.TryOpenPackage(Components.OpenedProject.Package, false, Components.CompositionOp);
    }

    private bool _initializedAfterLayoutReady;
    public IGraphCanvas GraphCanvas => Components.GraphCanvas;
    private static readonly EditSymbolDescriptionDialog _editDescriptionDialog = new();
}