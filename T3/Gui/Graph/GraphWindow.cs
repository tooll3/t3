using System;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.TransformGizmos;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public partial class GraphWindow : Window
    {
        public GraphWindow()
        {
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;



            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);
            var shownOpInstance = FindIdInNestedChildren(T3Ui.UiModel.RootInstance, opId) ?? T3Ui.UiModel.RootInstance;
            var path = NodeOperations.BuildIdPathForInstance(shownOpInstance);
            GraphCanvas = new GraphCanvas(this, path);

            _timeLineCanvas = new TimeLineCanvas();

            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            GraphWindowInstances.Add(this);
        }

        public static bool CanOpenAnotherWindow()
        {
            if (_instanceCounter > 0)
            {
                //Log.Error("only one graph window supported for now");
                return false;
            }

            return true;
        }

        public static Instance FindIdInNestedChildren(Instance instance, Guid childId)
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
        public static readonly List<Window> GraphWindowInstances = new List<Window>();

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
            return mainGraphWindow?.GraphCanvas.CompositionOp;
        }

        public override List<Window> GetInstances()
        {
            return GraphWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            //_playback.Update(ImGui.GetIO().DeltaTime, UserSettings.Config.KeepBeatTimeRunningInPause);
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

            area.Expand(400);

            GraphCanvas.FitAreaOnCanvas(area);
        }

        public static void SetBackgroundOutput(Instance instance)
        {
            if (_currentWindow == null)
                return;

            _currentWindow._imageBackground.BackgroundNodePath = instance != null
                                                                     ? NodeOperations.BuildIdPathForInstance(instance)
                                                                     : null;
        }
        
        public static void ClearBackground()
        {
            if (_currentWindow == null)
                return;

            _currentWindow._imageBackground.BackgroundNodePath = null;
        }

        protected override void DrawContent()
        {
            if (GraphCanvas.CompositionOp == null)
                return;
            
            if (FitViewToSelectionHandling.FitViewToSelectionRequested)
                FitViewToSelection();

            _imageBackground.Draw();

            ImGui.SetCursorPos(Vector2.Zero);
            THelpers.DebugContentRect("window");
            
            if (_imageBackground.IsActive && TransformGizmoHandling.IsDragging)
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
                        GraphCanvas.MakeCurrent();
                        TitleAndBreadCrumbs.Draw(GraphCanvas.CompositionOp);
                    }

                    DrawControlsAtBottom();
                }

                drawList.ChannelsSetCurrent(0);
                {
                    GraphCanvas.Draw(drawList, showGrid: !_imageBackground.IsActive);
                }
                drawList.ChannelsMerge();

                EditDescriptionDialog.Draw(GraphCanvas.CompositionOp.Symbol);
            }
            ImGui.EndChild();
            


            if (UserSettings.Config.ShowTimeline)
            {
                var availableRestHeight = ImGui.GetContentRegionAvail().Y;
                if (availableRestHeight <= 3)
                {
                    //Log.Warning($"skipping rending of timeline because layout is inconsistent: only {availableRestHeight}px left.");
                }
                else
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
                    ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize,0);
                    
                    ImGui.BeginChild("##timeline", Vector2.Zero, false,
                                     ImGuiWindowFlags.NoScrollbar 
                                     | ImGuiWindowFlags.NoMove 
                                     | ImGuiWindowFlags.NoResize
                                     | ImGuiWindowFlags.NoScrollWithMouse
                                     | ImGuiWindowFlags.NoDecoration);
                    {
                        _timeLineCanvas.Draw(GraphCanvas.CompositionOp);
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleVar(1);
                }
            }
            
            if(UserSettings.Config.ShowMiniMap)
                DrawMiniMap(GraphCanvas.CompositionOp);
        }

        private void DrawMiniMap(Instance compositionOp)
        {
            Vector2 widgetSize = new Vector2(200, 200);
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

                dl.AddRectFilled(widgetPos,widgetPos+ widgetSize,  T3Style.Colors.Background.Fade(0.8f));
                
                if (SymbolUiRegistry.Entries.TryGetValue(compositionOp.Symbol.Id, out var symbolUi))
                {
                    var hasFirst= false;
                    ImRect bounds = new ImRect();
                    foreach (var child in symbolUi.ChildUis)
                    {
                        var rect = ImRect.RectWithSize(child.PosOnCanvas, child.Size);
                        
                        if (!hasFirst)
                        {
                            bounds = rect;
                            hasFirst = true;
                        }
                        else
                        {
                            bounds.Add(rect);
                        }
                    }

                    if (hasFirst)
                    {
                        var mapMin = widgetPos + Vector2.One* 5;
                        //var mapMax = windowPos + mapWidgetSize - Vector2.One * 5;
                        var mapSize = widgetSize - Vector2.One * 10;
                        
                        var boundsMin = bounds.Min;
                        var boundsMax = bounds.Max;
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
                            dl.AddRectFilled(min,max, annotation.Color.Fade(0.1f));
                        }
                        
                        foreach (var child in symbolUi.ChildUis)
                        {
                            var rect = ImRect.RectWithSize(child.PosOnCanvas, child.Size);
                            var min = (rect.Min - boundsMin) / boundsSize * mapSize + mapMin;
                            var max = (rect.Max - boundsMin) / boundsSize * mapSize + mapMin;
                            
                            dl.AddRectFilled(min,max, Color.White.Fade(0.6f));
                        }
                        
                        // Draw View Area
                        var viewMin = GraphCanvas.InverseTransformPosition(Vector2.Zero);
                        var viewMax = GraphCanvas.InverseTransformPosition(GraphCanvas.WindowSize);
                        
                        var min2 = (viewMin - boundsMin) / boundsSize * mapSize + mapMin;
                        var max2 = (viewMax - boundsMin) / boundsSize * mapSize + mapMin;
                            
                        dl.AddRect(min2,max2, Color.White);
                        
                        
                        ImGui.InvisibleButton("##map", widgetSize);
                        if (ImGui.IsItemActive())
                        {
                            var posInMap = ImGui.GetMousePos();
                            var normalizedPos = (posInMap - widgetPos) / widgetSize;
                            var posInCanvas = bounds.Min + bounds.GetSize() * normalizedPos;
                            var scope = GraphCanvas.GetTargetScope();
                            scope.Scroll = posInCanvas;
                            GraphCanvas.SetTargetScope(scope);
                        }
                    }
                }
                
                // dl.AddRectFilled(windowPos+ new Vector2(10, 10), windowPos+ new Vector2(30, 30), Color.Orange);
            }

            ImGui.EndChild();
        }

        private void DrawControlsAtBottom()
        {
            if (!UserSettings.Config.ShowToolbar)
                return;
            
            ImGui.SetCursorPos(
                               new Vector2(
                                           ImGui.GetWindowContentRegionMin().X,
                                           ImGui.GetWindowContentRegionMax().Y - TimeControls.ControlSize.Y));

            ImGui.BeginChild("TimeControls", Vector2.Zero, false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);
            {
                if (CustomComponents.IconButton(UsingCustomTimelineHeight ?  Icon.ChevronDown : Icon.ChevronUp,
                                                "##TimelineToggle", TimeControls.ControlSize))
                {
                    _customTimeLineHeight = UsingCustomTimelineHeight ? UseComputedHeight : 200;
                    UserSettings.Config.ShowTimeline = true;
                }

                ImGui.SameLine();

                TimeControls.DrawTimeControls(_timeLineCanvas);
                if (_imageBackground.IsActive)
                {
                    _imageBackground.DrawResolutionSelector();
                    ImGui.SameLine();
                    if (ImGui.Button("Clear BG"))
                    {
                        ClearBackground();
                    }

                    ImGui.SameLine();
                    

                    var showGizmos = _imageBackground.ShowGizmos != T3.Core.Operator.GizmoVisibility.Off;
                    if (CustomComponents.ToggleIconButton(Icon.Grid, "##gizmos", ref showGizmos, Vector2.One * ImGui.GetFrameHeight()))
                    {
                        _imageBackground.ShowGizmos = showGizmos
                                                            ? T3.Core.Operator.GizmoVisibility.On
                                                            : T3.Core.Operator.GizmoVisibility.Off;
                    }                    
                    ImGui.SameLine();
                }
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

            public static void DrawBreadcrumbs(Instance compositionOp)
            {
                ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
                IEnumerable<Instance> parents = NodeOperations.GetParentInstances(compositionOp);

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

                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.3f).Rgba);
                ImGui.TextUnformatted("  - " + compositionOp.Symbol.Namespace);
                ImGui.PopFont();
                ImGui.PopStyleColor();

                var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];

                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    var desc = symbolUi.Description;
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
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
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);

                ImGui.PushFont(Fonts.FontSmall);
                if (ImGui.Button("Edit description..."))
                    EditDescriptionDialog.ShowNextFrame();

                ImGui.PopFont();
                ImGui.PopStyleColor(2);
            }
        }

        private readonly ImageBackground _imageBackground = new ImageBackground();

        public readonly GraphCanvas GraphCanvas;
        private const int UseComputedHeight = -1;
        private int _customTimeLineHeight = UseComputedHeight;
        private bool UsingCustomTimelineHeight => _customTimeLineHeight > UseComputedHeight;

        private float ComputedTimelineHeight => (_timeLineCanvas.SelectedAnimationParameters.Count * DopeSheetArea.LayerHeight)
                                                + _timeLineCanvas.LayersArea.LastHeight
                                                + TimeLineCanvas.TimeLineDragHeight
                                                + 2;

        private readonly TimeLineCanvas _timeLineCanvas;

        private static readonly EditSymbolDescriptionDialog EditDescriptionDialog = new EditSymbolDescriptionDialog();
    }
}