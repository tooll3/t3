#nullable enable
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.GraphUiModel;
using T3.Editor.Gui.Graph.Legacy.Interaction.Connections;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel;
using T3.Editor.UiModel.ProjectSession;
using T3.Editor.UiModel.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Legacy;

/// <summary>
/// A window that renders a node graph 
/// </summary>
internal sealed partial class GraphWindow : Window
{

    protected override string WindowDisplayTitle { get; }
    private bool _focusOnNextFrame;

    private void FocusFromSymbolBrowserHandler()
    {
        TakeFocus();
        _focusOnNextFrame = true;
    }

    protected override void DrawContent()
    {
        if (FitViewToSelectionHandling.FitViewToSelectionRequested)
            GraphCanvas.FocusViewToSelection();
            
        Components.OpenedProject.RefreshRootInstance();

        var fadeBackgroundImage = GraphImageBackground.IsActive 
                                      ? (ImGui.GetMousePos().X + 50).Clamp(0, 100) / 100 
                                      : 1;
        if (GraphImageBackground.IsActive && fadeBackgroundImage == 0)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                GraphImageBackground.ClearBackground();
            }
        }

        GraphImageBackground.Draw(fadeBackgroundImage);

        ImGui.SetCursorPos(Vector2.Zero);

        if (GraphImageBackground.IsActive && TransformGizmoHandling.IsDragging)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var contentHeight = (int)ImGui.GetWindowHeight();

        if (UserSettings.Config.ShowTimeline)
        {
            var currentTimelineHeight = UsingCustomTimelineHeight ? _customTimeLineHeight : ComputedTimelineHeight;
            if (CustomComponents.SplitFromBottom(ref currentTimelineHeight))
            {
                _customTimeLineHeight = (int)currentTimelineHeight;
            }

            contentHeight = (int)ImGui.GetWindowHeight() - (int)currentTimelineHeight -
                            4; // Hack that also depends on when a window-title is being rendered
        }

        ImGui.BeginChild("##graph", new Vector2(0, contentHeight), false,
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
            drawList.ChannelsSetCurrent(1);
            {
                if (UserSettings.Config.ShowTitleAndDescription)
                {
                    TitleAndBreadCrumbs.Draw(Components);
                }

                DrawControlsAtBottom(Components.Composition);
            }

            drawList.ChannelsSetCurrent(0);
            {
                const float activeBorderWidth = 30;
                // Fade and toggle graph on right edge
                var windowPos = Vector2.Zero;
                var windowSize = ImGui.GetIO().DisplaySize;
                var mousePos = ImGui.GetMousePos();
                var showBackgroundOnly = GraphImageBackground.IsActive && mousePos.X > windowSize.X + windowPos.X - activeBorderWidth;

                var backgroundActive = GraphImageBackground.IsActive;

                
                // Todo: clarify/simplify this opacity logic
                var graphOpacity = (backgroundActive && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                    ? (windowSize.X + windowPos.X - mousePos.X - activeBorderWidth).Clamp(0, 100) / 100
                                    : 1;

                if (graphOpacity < 1)
                {
                    var x = windowPos.X + windowSize.X - activeBorderWidth;
                    drawList.AddRectFilled(new Vector2(x, windowPos.Y),
                                           new Vector2(x + 1, windowPos.Y + windowSize.Y),
                                           UiColors.BackgroundFull.Fade((1 - graphOpacity)) * 0.5f);
                    drawList.AddRectFilled(new Vector2(x + 1, windowPos.Y),
                                           new Vector2(x + 2, windowPos.Y + windowSize.Y),
                                           UiColors.ForegroundFull.Fade((1 - graphOpacity)) * 0.5f);
                }
                    

                if (showBackgroundOnly)
                {
                    if ((!ImGui.IsAnyItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
                        GraphImageBackground.HasInteractionFocus = !GraphImageBackground.HasInteractionFocus;
                }
                else
                {
                    GraphCanvas.BeginDraw(backgroundActive, Components.GraphImageBackground.HasInteractionFocus);

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
                    
                    
                    ImGui.BeginGroup(); ImGui.SetScrollY(0);
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
            DrawMiniMap(Components.Composition, GraphCanvas);

        Components.CheckDisposal();
    }



    private static void DrawMiniMap(Composition compositionOp, IGraphCanvas canvas)
    {
        var widgetSize = new Vector2(200, 200);
        var localPos = new Vector2(ImGui.GetWindowWidth() - widgetSize.X, 0);
        ImGui.SetCursorPos(localPos);
        var widgetPos = ImGui.GetCursorScreenPos();

        if (ImGui.BeginChild("##minimap", widgetSize, false,
                             ImGuiWindowFlags.NoScrollbar
                             | ImGuiWindowFlags.NoMove
                             | ImGuiWindowFlags.NoScrollWithMouse
                             | ImGuiWindowFlags.NoDecoration
                             | ImGuiWindowFlags.NoTitleBar
                             | ImGuiWindowFlags.ChildWindow))
        {
            var dl = ImGui.GetWindowDrawList();

            dl.AddRectFilled(widgetPos, widgetPos + widgetSize, UiColors.BackgroundFull.Fade(0.8f));
            dl.AddRect(widgetPos, widgetPos + widgetSize, UiColors.BackgroundFull.Fade(0.9f));

            var symbolUi = compositionOp.SymbolUi;
            {
                var hasChildren = false;
                ImRect bounds = new ImRect();
                foreach (var child in symbolUi.ChildUis.Values)
                {
                    var rect = ImRect.RectWithSize(child.PosOnCanvas, child.Size);

                    if (!hasChildren)
                    {
                        bounds = rect;
                        hasChildren = true;
                    }
                    else
                    {
                        bounds.Add(rect);
                    }
                }

                var maxBoundsSize = MathF.Max(bounds.GetSize().X, bounds.GetSize().Y);
                var opacity = MathUtils.RemapAndClamp(maxBoundsSize, 200, 1000, 0, 1);

                if (hasChildren && opacity > 0)
                {
                    const float padding = 5;

                    var mapMin = widgetPos + Vector2.One * padding;
                    var mapSize = widgetSize - Vector2.One * padding * 2;

                    var boundsMin = bounds.Min;
                    var boundsSize = bounds.GetSize();
                    var boundsAspect = boundsSize.X / boundsSize.Y;

                    var mapAspect = mapSize.X / mapSize.Y;

                    if (boundsAspect > mapAspect)
                    {
                        mapSize.Y = mapSize.X / boundsAspect;
                    }
                    else
                    {
                        mapSize.X = mapSize.Y * boundsAspect;
                    }

                    foreach (var annotation in symbolUi.Annotations.Values)
                    {
                        var rect = ImRect.RectWithSize(annotation.PosOnCanvas, annotation.Size);
                        var min = (rect.Min - boundsMin) / boundsSize * mapSize + mapMin;
                        var max = (rect.Max - boundsMin) / boundsSize * mapSize + mapMin;
                        dl.AddRectFilled(min, max, annotation.Color.Fade(0.1f * opacity));
                    }

                    foreach (var child in symbolUi.ChildUis.Values)
                    {
                        var rect = ImRect.RectWithSize(child.PosOnCanvas, child.Size);
                        var min = (rect.Min - boundsMin) / boundsSize * mapSize + mapMin;
                        var max = (rect.Max - boundsMin) / boundsSize * mapSize + mapMin;

                        var fadedColor = UiColors.MiniMapItems.Fade(0.5f * opacity);
                        dl.AddRectFilled(min, max, fadedColor);
                    }

                    // Draw View Area
                    var viewMinInCanvas = canvas.InverseTransformPositionFloat(Vector2.Zero);
                    var viewMaxInCanvas = canvas.InverseTransformPositionFloat(canvas.WindowSize);

                    var min2 = (viewMinInCanvas - boundsMin) / boundsSize * mapSize + mapMin;
                    var max2 = (viewMaxInCanvas - boundsMin) / boundsSize * mapSize + mapMin;

                    dl.AddRect(min2, max2, UiColors.MiniMapItems.Fade(opacity));

                    var mousePos = ImGui.GetMousePos();
                    var normalizedMousePos = (mousePos - widgetPos - Vector2.One * padding) / mapSize;
                    var mousePosInCanvas = bounds.Min + bounds.GetSize() * normalizedMousePos;
                        
                    // Dragging
                    ImGui.InvisibleButton("##map", widgetSize);
                    if (ImGui.IsItemActive())
                    {
                        var scope = canvas.GetTargetScope();
                        scope.Scroll = mousePosInCanvas - (viewMaxInCanvas - viewMinInCanvas) / 2;
                        canvas.SetTargetScope(scope);
                    }

                    if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
                    {
                        var posInScreen = canvas.TransformPositionFloat(mousePosInCanvas);
                        canvas.ZoomWithMouseWheel(new MouseState()
                                                      {
                                                          Position = posInScreen, 
                                                          ScrollWheel  =ImGui.GetIO().MouseWheel
                                                      }, out _);
                    }
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawControlsAtBottom(Composition composition)
    {
        TimeControls.HandleTimeControlActions();
        if (!UserSettings.Config.ShowToolbar)
            return;

        ImGui.SetCursorPos(
                           new Vector2(
                                       ImGui.GetWindowContentRegionMin().X+1,
                                       ImGui.GetWindowContentRegionMax().Y - TimeControls.ControlSize.Y-1));

        ImGui.BeginChild("TimeControls", Vector2.Zero, false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
        {
            if (CustomComponents.IconButton(UsingCustomTimelineHeight ? Icon.ChevronDown : Icon.ChevronUp, TimeControls.ControlSize))
            {
                _customTimeLineHeight = UsingCustomTimelineHeight ? UseComputedHeight : 200;
                UserSettings.Config.ShowTimeline = true;
            }

            ImGui.SameLine();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            TimeControls.DrawTimeControls(Components.TimeLineCanvas, composition.Instance);
            ImGui.PopStyleVar();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            GraphImageBackground.DrawToolbarItems();
            ImGui.PopStyleVar();
        }
        ImGui.EndChild();
    }

    protected override void Close()
    {
        ConnectionMaker.RemoveWindow(GraphCanvas);
        GraphWindowInstances.Remove(this);
        if (Focused == this)
            Focused = GraphWindowInstances.FirstOrDefault();
            
        OnWindowDestroyed?.Invoke(this, Components.OpenedProject.Package);
    }

    protected override void AddAnotherInstance()
    {
        TryOpenPackage(Components.OpenedProject.Package, false, Components.CompositionOp);
    }
        
    private static class TitleAndBreadCrumbs
    {
        public static void Draw(GraphComponents window)
        {
            if (window.Composition == null)
                return;
            
            DrawBreadcrumbs(window);
            DrawNameAndDescription(window.Composition);
        }

        private static void DrawBreadcrumbs(GraphComponents components)
        {
            var composition = components.Composition;
            Debug.Assert(composition != null);
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
            FormInputs.AddVerticalSpace();
            var parents = Structure.CollectParentInstances(composition.Instance);

            ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            {
                var isFirstChild = true;
                foreach (var p in parents)
                {
                    if (isFirstChild)
                    {
                        isFirstChild=false;
                        ImGui.SameLine(7);
                    }
                    else
                    {
                        ImGui.SameLine(0);
                    }
                        
                        

                    ImGui.PushID(p.SymbolChildId.GetHashCode());

                    ImGui.PushFont(Fonts.FontSmall);
                    var clicked = ImGui.Button(p.Symbol.Name);
                    ImGui.PopFont();
                        
                    if (p.Parent == null && ImGui.BeginItemTooltip())
                    {
                        PopulateDependenciesTooltip(p);
                        ImGui.EndTooltip();
                    }

                    if (clicked)
                    {
                        components.TrySetCompositionOpToParent();
                        break;
                    }

                    ImGui.SameLine();
                    ImGui.PopID();
                    ImGui.PushFont(Icons.IconFont);
                    ImGui.TextUnformatted(_breadCrumbSeparator);
                    ImGui.PopFont();
                }
                    
                    
            }
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
        }

        private static void PopulateDependenciesTooltip(Instance p)
        {
            var project = p.Symbol.SymbolPackage;
            ImGui.Text("Project: " + project.DisplayName);
            ImGui.NewLine();
            ImGui.Text("Dependencies:");

            foreach (var dependency in project.Dependencies)
            {
                ImGui.Text(dependency.ToString());
            }
        }

        private static void DrawNameAndDescription(Composition compositionOp)
        {
            ImGui.SetCursorPosX(8);
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.TextUnformatted(compositionOp.Symbol.Name);

            if (compositionOp.Instance.Parent == null && ImGui.BeginItemTooltip())
            {
                ImGui.PushFont(Fonts.FontNormal);
                PopulateDependenciesTooltip(compositionOp.Instance);
                ImGui.PopFont();
                ImGui.EndTooltip();
            }
                
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.3f).Rgba);
            ImGui.TextUnformatted("  - " + compositionOp.Symbol.Namespace);
            ImGui.PopFont();
            ImGui.PopStyleColor();
        }
    }

    private bool _initializedAfterLayoutReady;
    internal GraphImageBackground GraphImageBackground => Components.GraphImageBackground;

    private static readonly string _breadCrumbSeparator = (char)Icon.ChevronRight + "";
    public IGraphCanvas GraphCanvas => Components.GraphCanvas;
    private const int UseComputedHeight = -1;
    private int _customTimeLineHeight = UseComputedHeight;
    private bool UsingCustomTimelineHeight => _customTimeLineHeight > UseComputedHeight;

    private float ComputedTimelineHeight => (Components.TimeLineCanvas.SelectedAnimationParameters.Count * DopeSheetArea.LayerHeight)
                                            + Components.TimeLineCanvas.LayersArea.LastHeight
                                            + TimeLineCanvas.TimeLineDragHeight
                                            + 1;

    private static readonly EditSymbolDescriptionDialog _editDescriptionDialog = new();
}