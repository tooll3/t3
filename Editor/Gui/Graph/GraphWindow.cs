using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphWindow : Window
    {
        public GraphWindow()
        {
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;

            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiSymbolData.RootInstance, opId) ?? T3Ui.UiSymbolData.RootInstance;
            var path = OperatorUtils.BuildIdPathForInstance(shownOpInstance);
            GraphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas();

            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            GraphWindowInstances.Add(this);
        }

        public static bool CanOpenAnotherWindow()
        {
            // if (_instanceCounter > 0)
            // {
            //     //Log.Error("only one graph window supported for now");
            //     return false;
            // }

            return true;
        }

        private static Instance FindIdInNestedChildren(Instance instance, Guid childId)
        {
            foreach (var child in instance.Children)
            {
                if (child.SymbolChildId == childId)
                {
                    return child;
                }

                var result = FindIdInNestedChildren(child, childId);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static int _instanceCounter;
        public static readonly List<Window> GraphWindowInstances = new();

        public static IEnumerable<GraphWindow> GetVisibleInstances()
        {
            foreach (var i in GraphWindowInstances)
            {
                if (!(i is GraphWindow graphWindow))
                    continue;

                if (!i.Config.Visible)
                    continue;

                yield return graphWindow;
            }
        }

        public static GraphWindow GetPrimaryGraphWindow()
        {
            return GraphWindow.GetVisibleInstances().FirstOrDefault();
        }

        public static Instance GetMainComposition()
        {
            var mainGraphWindow = GetPrimaryGraphWindow();
            return mainGraphWindow?.GraphCanvas?.CompositionOp;
        }

        public override List<Window> GetInstances()
        {
            return GraphWindowInstances;
        }

        private static GraphWindow _currentWindow;
        private bool _focusOnNextFrame;

        public void Focus()
        {
            _focusOnNextFrame = true;
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in GraphWindowInstances.ToArray())
            {
                _currentWindow = w as GraphWindow;
                w.DrawOneInstance();
            }

            _currentWindow = null;
        }

        private void FitViewToSelection()
        {
            var selection = NodeSelection.GetSelectedChildUis().ToArray();

            if (selection.Length == 0)
                return;

            var area = new ImRect(selection[0].PosOnCanvas,
                                  selection[0].PosOnCanvas + selection[0].Size);

            for (var index = 1; index < selection.Length; index++)
            {
                var selectedItem = selection[index];
                area.Add(new ImRect(selectedItem.PosOnCanvas,
                                    selectedItem.PosOnCanvas + selectedItem.Size));
            }

            area.Expand(550);

            GraphCanvas.FitAreaOnCanvas(area);
        }

        public static void SetBackgroundInstanceForCurrentGraph(Instance instance)
        {
            if (_currentWindow == null)
                return;

            _currentWindow.GraphImageBackground.OutputInstance = instance;
        }

        public void SetBackgroundOutput(Instance instance)
        {
            GraphImageBackground.OutputInstance = instance;
        }

        protected override void DrawContent()
        {
            //if (GraphCanvas.CompositionOp == null)
            //    return;

            if (FitViewToSelectionHandling.FitViewToSelectionRequested)
                FitViewToSelection();

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
                    if (GraphCanvas.CompositionOp != null && UserSettings.Config.ShowTitleAndDescription)
                    {
                        GraphCanvas.MakeCurrent();
                        TitleAndBreadCrumbs.Draw(GraphCanvas.CompositionOp);
                    }

                    DrawControlsAtBottom();
                }

                drawList.ChannelsSetCurrent(0);
                {
                    const float activeBorderWidth = 30;
                    // Fade and toggle graph on right edge
                    var windowPos = Vector2.Zero;
                    var windowSize = ImGui.GetIO().DisplaySize;
                    var mousePos = ImGui.GetMousePos();
                    var showBackgroundOnly = GraphImageBackground.IsActive && mousePos.X > windowSize.X + windowPos.X - activeBorderWidth;

                    var graphFade = (GraphImageBackground.IsActive && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                        ? (windowSize.X + windowPos.X - mousePos.X - activeBorderWidth ).Clamp(0, 100) / 100
                                        : 1;

                    if (graphFade < 1)
                    {
                        var x = windowPos.X + windowSize.X - activeBorderWidth;
                        drawList.AddRectFilled(new Vector2(x, windowPos.Y ),
                                               new Vector2(x+1, windowPos.Y + windowSize.Y),
                                               UiColors.BackgroundFull.Fade((1-graphFade)) * 0.5f);
                        drawList.AddRectFilled(new Vector2(x+1, windowPos.Y ),
                                               new Vector2(x+2, windowPos.Y + windowSize.Y),
                                               UiColors.ForegroundFull.Fade((1-graphFade)) * 0.5f);
                    }

                    if (showBackgroundOnly)
                    {
                        if ((!ImGui.IsAnyItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
                            GraphImageBackground.HasInteractionFocus = !GraphImageBackground.HasInteractionFocus;
                    }
                    else
                    {
                        var flags = GraphCanvas.GraphDrawingFlags.None;
                        if (GraphImageBackground.IsActive)
                            flags |= GraphCanvas.GraphDrawingFlags.HideGrid;

                        if (GraphImageBackground.HasInteractionFocus)
                            flags |= GraphCanvas.GraphDrawingFlags.PreventInteractions;

                        GraphCanvas.Draw(drawList, flags, graphFade);
                        ParameterPopUp.DrawParameterPopUp(this);
                    }
                }
                drawList.ChannelsMerge();

                if (GraphCanvas.CompositionOp != null)
                    _editDescriptionDialog.Draw(GraphCanvas.CompositionOp.Symbol);
            }
            ImGui.EndChild();

            if (GraphCanvas.CompositionOp != null && UserSettings.Config.ShowTimeline)
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
                        _timeLineCanvas.Draw(GraphCanvas.CompositionOp, Playback.Current);
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleVar(1);
                }
            }

            if (UserSettings.Config.ShowMiniMap)
                DrawMiniMap(GraphCanvas.CompositionOp, GraphCanvas);
        }

        private static void DrawMiniMap(Instance compositionOp, ScalableCanvas canvas)
        {
            if (compositionOp == null || canvas == null)
                return;

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

                if (SymbolUiRegistry.Entries.TryGetValue(compositionOp.Symbol.Id, out var symbolUi))
                {
                    var hasChildren = false;
                    ImRect bounds = new ImRect();
                    foreach (var child in symbolUi.ChildUis)
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

                        foreach (var child in symbolUi.ChildUis)
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

                        // Debug visualization
                        //var posInScreen = graphCanvas.TransformPosition(posInCanvas);
                        //ImGui.GetForegroundDrawList().AddCircle(posInScreen, 10, Color.Green);

                        // Dragging
                        ImGui.InvisibleButton("##map", widgetSize);
                        if (ImGui.IsItemActive())
                        {
                            var scope = canvas.GetTargetScope();
                            scope.Scroll = mousePosInCanvas - (viewMaxInCanvas - viewMinInCanvas) / 2;
                            canvas.SetTargetScope(scope);
                        }

                        if (ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel != 0)
                        {
                            var centerInCanvas = (viewMaxInCanvas + viewMinInCanvas) / 2;
                            canvas.ZoomWithMouseWheel(centerInCanvas);
                        }
                    }
                }
            }

            ImGui.EndChild();
        }

        private void DrawControlsAtBottom()
        {
            TimeControls.HandleTimeControlActions();
            if (!UserSettings.Config.ShowToolbar)
                return;

            ImGui.SetCursorPos(
                               new Vector2(
                                           ImGui.GetWindowContentRegionMin().X,
                                           ImGui.GetWindowContentRegionMax().Y - TimeControls.ControlSize.Y));

            ImGui.BeginChild("TimeControls", Vector2.Zero, false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
            {
                if (CustomComponents.IconButton(UsingCustomTimelineHeight ? Icon.ChevronDown : Icon.ChevronUp, TimeControls.ControlSize))
                {
                    _customTimeLineHeight = UsingCustomTimelineHeight ? UseComputedHeight : 200;
                    UserSettings.Config.ShowTimeline = true;
                }

                ImGui.SameLine();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                TimeControls.DrawTimeControls(_timeLineCanvas);
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
            GraphWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphWindow(); // Must call constructor
        }

        private static class TitleAndBreadCrumbs
        {
            public static void Draw(Instance compositionOp)
            {
                DrawBreadcrumbs(compositionOp);
                DrawNameAndDescription(compositionOp);
            }

            private static void DrawBreadcrumbs(Instance compositionOp)
            {
                ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
                IEnumerable<Instance> parents = Structure.CollectParentInstances(compositionOp);

                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushFont(Fonts.FontSmall);
                {
                    foreach (var p in parents)
                    {
                        ImGui.SameLine();
                        ImGui.PushID(p.SymbolChildId.GetHashCode());

                        var clicked = ImGui.Button(p.Symbol.Name);

                        if (clicked)
                        {
                            GraphCanvas.Current.SetCompositionToParentInstance(p);
                            break;
                        }

                        ImGui.SameLine();
                        ImGui.PopID();
                        ImGui.TextUnformatted(">");
                    }
                }
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }

            private static void DrawNameAndDescription(Instance compositionOp)
            {
                ImGui.SetCursorPosX(8);
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted(compositionOp.Symbol.Name);
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.3f).Rgba);
                ImGui.TextUnformatted("  - " + compositionOp.Symbol.Namespace);
                ImGui.PopFont();
                ImGui.PopStyleColor();

                var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];

                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    var desc = symbolUi.Description;
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    {
                        var sizeMatchingDescription = ImGui.CalcTextSize(desc) + new Vector2(20, 40);
                        sizeMatchingDescription.X = Math.Max(300, sizeMatchingDescription.X);
                        ImGui.Indent(9);
                        ImGui.TextWrapped(desc);
                    }
                    ImGui.PopStyleColor(2);
                    ImGui.PopFont();
                }

                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);

                ImGui.PushFont(Fonts.FontSmall);
                if (ImGui.Button("Edit description..."))
                    _editDescriptionDialog.ShowNextFrame();

                ImGui.PopFont();
                ImGui.PopStyleColor(2);
            }
        }

        internal readonly GraphImageBackground GraphImageBackground = new();

        public readonly GraphCanvas GraphCanvas;
        private const int UseComputedHeight = -1;
        private int _customTimeLineHeight = UseComputedHeight;
        private bool UsingCustomTimelineHeight => _customTimeLineHeight > UseComputedHeight;

        private float ComputedTimelineHeight => (_timeLineCanvas.SelectedAnimationParameters.Count * DopeSheetArea.LayerHeight)
                                                + _timeLineCanvas.LayersArea.LastHeight
                                                + TimeLineCanvas.TimeLineDragHeight
                                                + 1;

        private readonly TimeLineCanvas _timeLineCanvas;

        public TimeLineCanvas CurrentTimeLine => _timeLineCanvas;

        private static readonly EditSymbolDescriptionDialog _editDescriptionDialog = new();
    }
}