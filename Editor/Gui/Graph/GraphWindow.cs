#nullable enable
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    internal sealed partial class GraphWindow : Window
    {
        public static GraphWindow? Focused
        {
            get => _focused;
            private set
            {
                if (_focused == value)
                    return;

                _focused?.FocusLost?.Invoke(_focused, _focused);
                _focused = value;
                if(value != null)
                    Log.Debug($"Focused! {value.Config.Title} + {value.InstanceNumber}");
            }
        }
        
        internal event EventHandler<GraphWindow>? FocusLost;

        private Composition _composition;
        internal Instance CompositionOp => _composition.Instance;

        public readonly SymbolBrowser SymbolBrowser;
        public readonly EditorSymbolPackage Package;
        public readonly int InstanceNumber;
        private static int _instanceCounter;
        private const int NoInstanceNumber = -1;
        
        private GraphWindow(EditorSymbolPackage package, int instanceNumber)
        {
            InstanceNumber = instanceNumber;
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            Package = package;
            
            if(!package.TryGetRootInstance(out var actualRoot))
                throw new Exception("Could not get root instance from package");
            
            _rootInstance = actualRoot!;
            AllowMultipleInstances = true;
            
            ConnectionMaker.AddWindow(this);
            Structure = new Structure(() => RootInstance.Instance);
            
            var navigationHistory = new NavigationHistory(Structure);
            var nodeSelection = new NodeSelection(navigationHistory, Structure);

            GraphImageBackground = new GraphImageBackground(this, nodeSelection, Structure);
            GraphCanvas = new GraphCanvas(this, nodeSelection, navigationHistory);
            SymbolBrowser = new SymbolBrowser(this, GraphCanvas);
            TimeLineCanvas = new TimeLineCanvas(GraphCanvas);
            WindowDisplayTitle = package.DisplayName + "##" + InstanceNumber;
            SetWindowToNormal();
        }

        public static bool CanOpenAnotherWindow => true;

        internal static readonly List<GraphWindow> GraphWindowInstances = new();
        protected override string WindowDisplayTitle { get; }

        public override IReadOnlyList<Window> GetInstances() => GraphWindowInstances;

        private bool _focusOnNextFrame;

        public void FocusFromSymbolBrowser()
        {
            TakeFocus();
            _focusOnNextFrame = true;
        }

        public void SetBackgroundOutput(Instance instance)
        {
            GraphImageBackground.OutputInstance = instance;
        }

        protected override void DrawContent()
        {
            if (FitViewToSelectionHandling.FitViewToSelectionRequested)
                GraphCanvas.FocusViewToSelection();

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
                        TitleAndBreadCrumbs.Draw(this, _composition);
                    }

                    DrawControlsAtBottom(_composition);
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
                    var graphFade = (backgroundActive && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                        ? (windowSize.X + windowPos.X - mousePos.X - activeBorderWidth).Clamp(0, 100) / 100
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
                        
                        if (backgroundActive)
                            flags |= GraphCanvas.GraphDrawingFlags.HideGrid;

                        if (GraphImageBackground.HasInteractionFocus)
                            flags |= GraphCanvas.GraphDrawingFlags.PreventInteractions;

                        var preventInteractions = flags.HasFlag(GraphCanvas.GraphDrawingFlags.PreventInteractions);
                        GraphCanvas.Draw(preventInteractions);

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
                            ApplyComposition(ICanvas.Transition.JumpIn, null);
                            GraphCanvas.FocusViewToSelection();
                            _initializedAfterLayoutReady = true;
                        }

                        GraphBookmarkNavigation.HandleForCanvas(this);
                        GraphCanvas.DrawGraph(drawList, flags, preventInteractions, graphFade);
                        
                        ParameterPopUp.DrawParameterPopUp(this);
                    }
                }
                drawList.ChannelsMerge();

                EditDescriptionDialog.Draw(CompositionOp.Symbol);
            }
            ImGui.EndChild();

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
                        TimeLineCanvas.Draw(CompositionOp, Playback.Current);
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleVar(1);
                }
            }

            if (UserSettings.Config.ShowMiniMap)
                DrawMiniMap(_composition, GraphCanvas);
        }

        private static void DrawMiniMap(Composition compositionOp, ScalableCanvas canvas)
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
                TimeControls.DrawTimeControls(TimeLineCanvas, composition.Instance);
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
            ConnectionMaker.RemoveWindow(this);
            GraphWindowInstances.Remove(this);
            if (Focused == this)
                Focused = GraphWindowInstances.FirstOrDefault();
            
            WindowDestroyed?.Invoke(this, Package);
        }

        public event EventHandler<EditorSymbolPackage> WindowDestroyed;

        protected override void AddAnotherInstance()
        {
            TryOpenPackage(Package, false, CompositionOp);
        }
        
        private static class TitleAndBreadCrumbs
        {
            public static void Draw(GraphWindow window, Composition composition)
            {
                DrawBreadcrumbs(window, composition);
                DrawNameAndDescription(composition);
            }

            private static void DrawBreadcrumbs(GraphWindow window, Composition composition)
            {
                ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
                var parents = Structure.CollectParentInstances(composition.Instance);

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
                            window.TrySetCompositionOpToParent();
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

            private static void DrawNameAndDescription(Composition compositionOp)
            {
                ImGui.SetCursorPosX(8);
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted(compositionOp.Symbol.Name);
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.3f).Rgba);
                ImGui.TextUnformatted("  - " + compositionOp.Symbol.Namespace);
                ImGui.PopFont();
                ImGui.PopStyleColor();

                var symbolUi = compositionOp.SymbolUi;

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
                    EditDescriptionDialog.ShowNextFrame();

                ImGui.PopFont();
                ImGui.PopStyleColor(2);
            }
        }

        internal readonly GraphImageBackground GraphImageBackground;

        public readonly GraphCanvas GraphCanvas;
        private const int UseComputedHeight = -1;
        private int _customTimeLineHeight = UseComputedHeight;
        private bool UsingCustomTimelineHeight => _customTimeLineHeight > UseComputedHeight;

        private float ComputedTimelineHeight => (TimeLineCanvas.SelectedAnimationParameters.Count * DopeSheetArea.LayerHeight)
                                                + TimeLineCanvas.LayersArea.LastHeight
                                                + TimeLineCanvas.TimeLineDragHeight
                                                + 1;

        internal readonly TimeLineCanvas TimeLineCanvas;

        private static readonly EditSymbolDescriptionDialog EditDescriptionDialog = new();
        private static GraphWindow? _focused;

        public static bool TryOpenPackage(EditorSymbolPackage package, bool replaceFocused, Instance? startingComposition = null, WindowConfig? config = null, int instanceNumber = NoInstanceNumber)
        {
            if (!package.TryGetRootInstance(out var root))
            {
                LogFailure();
                return false;
            }

            var shouldBeVisible = true;
            if (replaceFocused && Focused != null)
            {
                shouldBeVisible = Focused.Config.Visible;
                Focused.Close();
            }

            instanceNumber = instanceNumber == NoInstanceNumber ? ++_instanceCounter : instanceNumber;
            var newWindow = new GraphWindow(package, instanceNumber);

            if (config == null)
            {
                config = newWindow.Config;
                config.Title = LayoutHandling.GraphPrefix + instanceNumber;
                config.Visible = shouldBeVisible;
            }
            else
            {
                config.Visible = true;
                newWindow.Config = config;
            }

            IReadOnlyList<Guid> rootPath = [root!.SymbolChildId];
            var startPath = rootPath;
            if (root == startingComposition)
            {
                // Legacy work-around
                var opId = UserSettings.GetLastOpenOpForWindow(config.Title);
                if (opId != Guid.Empty && opId != root.SymbolChildId)
                {
                    if (root.TryGetChildInstance(opId, true, out _, out var path))
                    {
                        startPath = path;
                    }
                }
            }

            const ICanvas.Transition transition = ICanvas.Transition.JumpIn;
            if (!newWindow.TrySetCompositionOp(startPath, transition) && !newWindow.TrySetCompositionOp(rootPath, transition))
            {
                LogFailure();
                newWindow.Close();
                return false;
            }
            
            GraphWindowInstances.Add(newWindow);
            newWindow.TakeFocus();
            return true;
            
            void LogFailure() => Log.Error($"Failed to open operator graph for {package.DisplayName}");
        }

        public void SetWindowToNormal()
        {
            WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                             ImGuiWindowFlags.NoResize);
        }

        public void TakeFocus()
        {
            Focused = this;
        }

        internal bool TrySetCompositionOp(IReadOnlyList<Guid> path, ICanvas.Transition transition = ICanvas.Transition.Undefined, Guid? nextSelectedUi = null)
        {
            if (!Package.IsReadOnly)
            {
                // refresh root in case the project root changed in code?
                if (!Package.TryGetRootInstance(out var rootInstance))
                {
                    return false;
                }

                _rootInstance = rootInstance!;
            }

            var newCompositionInstance = Structure.GetInstanceFromIdPath(path);

            if (newCompositionInstance == null)
            {
                var pathString = string.Join('/', Structure.GetReadableInstancePath(path));
                Log.Error("Failed to find instance with path " + pathString);
                return false;
            }
            
            var previousComposition = _composition;

            // composition is only null once in the very first call to TrySetCompositionOp
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (previousComposition != null)
            {
                if (path[0] != RootInstance.Instance.SymbolChildId)
                {
                    throw new Exception("Root instance is not the first element in the path");
                }
                if (previousComposition.SymbolChildId == newCompositionInstance.SymbolChildId)
                    return true;
            }

            _composition = Composition.GetFor(newCompositionInstance, true)!;
            _compositionPath.Clear();
            _compositionPath.AddRange(path);
            TimeLineCanvas.ClearSelection();

            
            if (nextSelectedUi != null)
            {
                var instance = _composition.Instance.Children[nextSelectedUi.Value];
                var symbolChildUi = instance.GetChildUi();
                
                if(symbolChildUi != null)
                    GraphCanvas.NodeSelection.SetSelectionToChildUi(symbolChildUi, instance);
                else
                    GraphCanvas.NodeSelection.Clear();
            }
            else
            {
                GraphCanvas.NodeSelection.Clear();
            }
            
            ApplyComposition(transition, previousComposition);
            DisposeOfCompositions(path, previousComposition, _compositionsWaitingForDisposal);

            UserSettings.SaveLastViewedOpForWindow(this, _composition.SymbolChildId);
            return true;

            static void DisposeOfCompositions(IReadOnlyList<Guid> currentPath, Composition? previous, List<Composition> compositionsWaitingForDisposal)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (previous != null && !compositionsWaitingForDisposal.Contains(previous))
                {
                    compositionsWaitingForDisposal.Add(previous);
                }

                for (int i = compositionsWaitingForDisposal.Count - 1; i >= 0; i--)
                {
                    var composition = compositionsWaitingForDisposal[i];
                    var symbolChildId = composition.SymbolChildId;
                    if (!currentPath.Contains(symbolChildId))
                    {
                        composition.Dispose();
                        compositionsWaitingForDisposal.RemoveAt(i);
                    }
                }
            }
        }
        
        private readonly List<Guid> _compositionPath = [];
        
        private Instance _rootInstance;
        internal Composition RootInstance => Composition.GetFor(_rootInstance, true);
        private bool _initializedAfterLayoutReady;
        private readonly List<Composition> _compositionsWaitingForDisposal = new();

        public readonly Structure Structure;

        public bool TrySetCompositionOpToChild(Guid symbolChildId)
        {
            // new list as _compositionPath is mutable
            var newPathList = new List<Guid>(_compositionPath.Count + 1);
            newPathList.AddRange(_compositionPath);
            newPathList.Add(symbolChildId);
            return TrySetCompositionOp(newPathList, ICanvas.Transition.JumpIn);
        }
        
        public bool TrySetCompositionOpToParent()
        {
            if (_compositionPath.Count == 1)
                return false;

            var previousComposition = _composition;
            
            // new list as _compositionPath is mutable
            var path = _compositionPath.GetRange(0, _compositionPath.Count - 1);

            // pass the child UI only in case the previous composition was a cloned instance
            return TrySetCompositionOp(path, ICanvas.Transition.JumpOut, previousComposition.SymbolChildId);
        }

        private void ApplyComposition(ICanvas.Transition transition, Composition? previousComposition)
        {
            GraphCanvas.SelectableNodeMovement.Reset();

            var compositionOp = CompositionOp;

            if (previousComposition != null)
            {
                // zoom timeline out if necessary
                if (transition == ICanvas.Transition.JumpOut)
                {
                    TimeLineCanvas.UpdateScaleAndTranslation(previousComposition.Instance, transition);
                }

                var targetScope = GraphCanvas.GetTargetScope();
                UserSettings.Config.OperatorViewSettings[previousComposition.SymbolChildId] = targetScope;
            }

            TimeLineCanvas.ClearSelection();
            GraphCanvas.FocusViewToSelection();

            var newProps = GraphCanvas.GetTargetScope();
            if (UserSettings.Config.OperatorViewSettings.TryGetValue(compositionOp.SymbolChildId, out var viewSetting))
            {
                newProps = viewSetting;
            }

            GraphCanvas.SetScopeWithTransition(newProps.Scale, newProps.Scroll, transition);

            if (transition == ICanvas.Transition.JumpIn)
            {
                TimeLineCanvas.UpdateScaleAndTranslation(compositionOp, transition);
            }
        }
    }
}