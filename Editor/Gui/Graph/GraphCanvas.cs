using System.IO;
using System.Runtime.InteropServices;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Core.UserData;
using T3.Editor.External;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Annotations;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    internal class GraphCanvas : ScalableCanvas, INodeCanvas
    {
        internal GraphCanvas(GraphWindow window, NodeSelection nodeSelection, NavigationHistory navigationHistory)
        {
            _window = window;
            Structure = window.Structure;
            NavigationHistory = navigationHistory;
            NodeSelection = nodeSelection;
            _nodeGraphLayouting = new NodeGraphLayouting(NodeSelection, Structure);
            NodeNavigation = new NodeNavigation(() => window.CompositionOp, this, Structure, NavigationHistory);
            SelectableNodeMovement = new SelectableNodeMovement(window, this, NodeSelection);
            _graph = new Graph(window, this);
            

            window.WindowDestroyed += (_, _) => Destroyed = true;
        }

        /// <summary>
        /// Uses an ID-path to open an instance's parent composition and centers the instance.
        /// This can be useful to jump to elements (e.g. through bookmarks)
        /// </summary>
        public void OpenAndFocusInstance(List<Guid> childIdPath)
        {
            var pathToParent = childIdPath.GetRange(0, childIdPath.Count - 1);
            var success = _window.TrySetCompositionOp(pathToParent);

            if (!success)
                return;

            var composition = _window.CompositionOp;
            var instanceId = childIdPath[^1];

            if (!composition.TryGetChildInstance(instanceId, false, out var childInstance, out _))
            {
                Log.Error($"Failed to find child instance with id {instanceId}");
                return;
            }
            
            NodeSelection.SetSelectionToChildUi(childInstance!.GetSymbolChildUi(), composition);
            FitViewToSelectionHandling.FitViewToSelection();
        }

        public void SetCompositionToChildInstance(Instance instance) => _window.TrySetCompositionOpToChild(instance.SymbolChildId);

        [Flags]
        public enum GraphDrawingFlags
        {
            None = 0,
            PreventInteractions = 1 << 1,
            HideGrid = 1 << 2,
        }

        #region drawing UI ====================================================================
        /// <summary>
        /// Scales the canvas and handles basic canvas interactions
        /// </summary>
        public void Draw(bool preventInteractions)
        {
            var editingFlags = T3Ui.EditingFlags.None;

            if (_window.SymbolBrowser.IsOpen)
                editingFlags |= T3Ui.EditingFlags.PreventZoomWithMouseWheel;

            if (preventInteractions)
                editingFlags |= T3Ui.EditingFlags.PreventMouseInteractions;

            UpdateCanvas(out _, editingFlags);

        }

        public void DrawGraph(ImDrawListPtr drawList, GraphDrawingFlags drawingFlags, bool preventInteractions, float graphOpacity)
        {
            ImGui.BeginGroup();
            {
                ImGui.SetScrollY(0);
                var focused = CustomComponents.DrawWindowFocusFrame();
                if (focused)
                    _window.TakeFocus();
                
                DrawDropHandler(_window.CompositionOp, _window.CompositionOp.GetSymbolUi());
                ImGui.SetCursorScreenPos(Vector2.One * 100);

                if (!preventInteractions)
                {
                    var compositionOp = _window.CompositionOp;
                    var compositionUi = compositionOp.GetSymbolUi();
                    compositionUi.FlagAsModified();
                    
                    if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                        FocusViewToSelection();

                    if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.Duplicate))
                    {
                        CopySelectedNodesToClipboard(compositionOp);
                        PasteClipboard(compositionOp);
                    }

                    if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    {
                        var canModify = !compositionUi.Symbol.SymbolPackage.IsReadOnly;
                        DeleteSelectedElements(compositionUi, canModify);
                    }

                    if (KeyboardBinding.Triggered(UserActions.ToggleDisabled))
                    {
                        ToggleDisabledForSelectedElements();
                    }

                    if (KeyboardBinding.Triggered(UserActions.ToggleBypassed))
                    {
                        ToggleBypassedForSelectedElements();
                    }

                    if (KeyboardBinding.Triggered(UserActions.PinToOutputWindow))
                    {
                        PinSelectedToOutputWindow(compositionOp);
                    }

                    if (KeyboardBinding.Triggered(UserActions.CopyToClipboard))
                    {
                        CopySelectedNodesToClipboard(compositionOp);
                    }

                    if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.PasteFromClipboard))
                    {
                        PasteClipboard(compositionOp);
                    }

                    if (KeyboardBinding.Triggered(UserActions.LayoutSelection))
                    {
                        _nodeGraphLayouting.ArrangeOps(compositionOp);
                    }

                    if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.AddAnnotation))
                    {
                        AddAnnotation(compositionOp);
                    }

                    List<Guid>? navigationPath = null;

                    // Navigation
                    if (KeyboardBinding.Triggered(UserActions.NavigateBackwards))
                    {
                        navigationPath = NavigationHistory.NavigateBackwards();
                    }

                    if (KeyboardBinding.Triggered(UserActions.NavigateForward))
                    {
                        navigationPath = NavigationHistory.NavigateForward();
                    }

                    if (navigationPath != null)
                        OpenAndFocusInstance(navigationPath);

                    if (KeyboardBinding.Triggered(UserActions.SelectToAbove))
                    {
                        NodeNavigation.SelectAbove();
                    }

                    if (KeyboardBinding.Triggered(UserActions.SelectToRight))
                    {
                        NodeNavigation.SelectRight();
                    }

                    if (KeyboardBinding.Triggered(UserActions.SelectToBelow))
                    {
                        NodeNavigation.SelectBelow();
                    }

                    if (KeyboardBinding.Triggered(UserActions.AddComment))
                    {
                        EditCommentDialog.ShowNextFrame();
                    }

                    if (KeyboardBinding.Triggered(UserActions.SelectToLeft))
                    {
                        NodeNavigation.SelectLeft();
                    }

                    if (KeyboardBinding.Triggered(UserActions.DisplayImageAsBackground))
                    {
                        var selectedImage = NodeSelection.GetFirstSelectedInstance();
                        if (selectedImage != null)
                        {
                            _window.GraphImageBackground.OutputInstance = selectedImage;
                        }
                    }
                }

                if (ImGui.IsWindowFocused() && !preventInteractions)
                {
                    var io = ImGui.GetIO();
                    var editingSomething = ImGui.IsAnyItemActive();

                    if (!io.KeyCtrl && !io.KeyShift && !io.KeyAlt && !editingSomething)
                    {
                        if (ImGui.IsKeyDown((ImGuiKey)Key.W))
                        {
                            _dampedScrollVelocity.Y -= InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).Y;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.S))
                        {
                            _dampedScrollVelocity.Y += InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).Y;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.A))
                        {
                            _dampedScrollVelocity.X -= InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).X;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.D))
                        {
                            _dampedScrollVelocity.X += InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).X;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.Q))
                        {
                            var center = WindowPos + WindowSize / 2;
                            ApplyZoomDelta(center, 1.05f, out _);
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.E))
                        {
                            var center = WindowPos + WindowSize / 2;
                            ApplyZoomDelta(center, 1 / 1.05f, out _);
                        }
                    }
                }

                ScrollTarget += _dampedScrollVelocity;
                _dampedScrollVelocity *= 0.90f;

                drawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                if (!drawingFlags.HasFlag(GraphDrawingFlags.HideGrid))
                    DrawGrid(drawList);

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    ConnectionSplitHelper.PrepareNewFrame(_window);
                }

                _window.SymbolBrowser.Draw();

                graphOpacity *= preventInteractions ? 0.3f : 1;
                _graph.DrawGraph(drawList, drawingFlags.HasFlag(GraphDrawingFlags.PreventInteractions), _window.CompositionOp, graphOpacity);

                RenameInstanceOverlay.Draw(_window);

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
                    && !preventInteractions
                    && ConnectionMaker.TempConnections.Count == 0)
                    HandleFenceSelection(_window.CompositionOp);

                var isOnBackground = ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive();
                if (isOnBackground && ImGui.IsMouseDoubleClicked(0))
                {
                    _window.TrySetCompositionOpToParent();
                }

                if (ConnectionMaker.TempConnections.Count > 0 && ImGui.IsMouseReleased(0))
                {
                    var isAnyItemHovered = ImGui.IsAnyItemHovered();
                    var droppedOnBackground =
                        ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && !isAnyItemHovered;
                    if (droppedOnBackground)
                    {
                        ConnectionMaker.InitSymbolBrowserAtPosition(_window,
                                                                    InverseTransformPositionFloat(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        var connectionDroppedOnBackground =
                            ConnectionMaker.TempConnections[0].GetStatus() != ConnectionMaker.TempConnection.Status.TargetIsDraftNode;
                        if (connectionDroppedOnBackground)
                        {
                            //Log.Warning("Skipping complete operation on background drop?");
                            //  ConnectionMaker.CompleteOperation();
                        }
                    }
                }

                drawList.PopClipRect();
                
                var compositionUpdated = _window.CompositionOp;
                var canModifyUpdated = !compositionUpdated.Symbol.SymbolPackage.IsReadOnly;
                
                if (FrameStats.Current.OpenedPopUpName != string.Empty)
                    CustomComponents.DrawContextMenuForScrollCanvas(() => DrawContextMenuContent(canModifyUpdated, compositionUpdated), ref _contextMenuIsOpen);

                _duplicateSymbolDialog.Draw(compositionUpdated, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);
                _combineToSymbolDialog.Draw(compositionUpdated, GetSelectedChildUis(),
                                            NodeSelection.GetSelectedNodes<Annotation>().ToList(),
                                            ref _nameSpaceForDialogEdits,
                                            ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);

                if (canModifyUpdated)
                {
                    _renameSymbolDialog.Draw(GetSelectedChildUis(), ref _symbolNameForDialogEdits);
                    EditCommentDialog.Draw(NodeSelection);

                    if (compositionUpdated != _window.RootInstance.Instance)
                    {
                        var symbol = compositionUpdated.Symbol;
                        _addInputDialog.Draw(symbol);
                        _addOutputDialog.Draw(symbol);
                    }
                }

                LibWarningDialog.Draw(this);
                EditNodeOutputDialog.Draw();
            }
            ImGui.EndGroup();
        }

        private void HandleFenceSelection(Instance compositionOp)
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.PressedButNotMoved:
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                        NodeSelection.Clear();
                    break;

                case SelectionFence.States.Updated:
                    HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen, compositionOp);
                    break;

                case SelectionFence.States.CompletedAsClick:
                    // A hack to prevent clearing selection when opening parameter popup
                    if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                        break;

                    NodeSelection.Clear();
                    NodeSelection.SetSelectionToComposition(compositionOp);
                    break;
            }
        }

        private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

        private void HandleSelectionFenceUpdate(ImRect boundsInScreen, Instance compositionOp)
        {
            var boundsInCanvas = InverseTransformRect(boundsInScreen);
            var nodesToSelect = SelectableChildren
                               .Select(child => (child, rect: new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size) ))
                               .Where(t => t.rect.Overlaps(boundsInCanvas))
                               .Select(t => t.child)
                               .ToList();

            if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
            {
                NodeSelection.Clear();
            }

            foreach (var node in nodesToSelect)
            {
                if (node is SymbolChildUi symbolChildUi)
                {
                    if (!compositionOp.TryGetChildInstance(symbolChildUi.Id, false, out var instance, out _))
                    {
                        Log.Warning("Can't find instance");
                    }
                    else
                    {
                    }

                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                    {
                        NodeSelection.DeselectNode(symbolChildUi, instance);
                    }
                    else
                    {
                        NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                    }
                }
                else if (node is Annotation annotation)
                {
                    var annotationRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);
                    if (boundsInCanvas.Contains(annotationRect))
                    {
                        if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                        {
                            NodeSelection.DeselectNode(annotation);
                        }
                        else
                        {
                            NodeSelection.AddSelection(annotation);
                        }
                    }
                }
                else
                {
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                    {
                        NodeSelection.DeselectNode(node);
                    }
                    else
                    {
                        NodeSelection.AddSelection(node);
                    }
                }
            }
        }

        /// <remarks>
        /// This method is complex, because it has to handle several edge cases and has potential to remove previous user data:
        /// - We have to preserve the previous state.
        /// - We have to make space -> Shift all connected operators towards the right.
        /// - We have to convert all existing connections from the output into temporary connections.
        /// - We have to insert a new temp connection line between output and symbol browser
        ///
        /// - If the user completes the symbol browser, it must complete the previous connections from the temp connections.
        /// - If the user cancels the operation, the previous state has to be restored. This might be tricky
        /// </remarks>
        public void OpenSymbolBrowserForOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef)
        {
            ConnectionMaker.InitSymbolBrowserAtPosition(_window,
                                                        childUi.PosOnCanvas + new Vector2(childUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0));
        }

        // private Symbol GetSelectedSymbol()
        // {
        //     var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
        //     return selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : compositionOp.Symbol;
        // }

        private void DrawDropHandler(Instance compositionOp, SymbolUi compositionOpSymbolUi)
        {
            if (!T3Ui.DraggingIsInProgress)
                return;

            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.InvisibleButton("## drop", ImGui.GetWindowSize());

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("Symbol");
                if (ImGui.IsMouseReleased(0))
                {
                    var myString = Marshal.PtrToStringAuto(payload.Data);
                    if (myString != null)
                    {
                        var guidString = myString.Split('|')[0];
                        var guid = Guid.Parse(guidString);
                        Log.Debug("dropped symbol here" + payload + " " + myString + "  " + guid);

                        var symbol = EditorSymbolPackage.AllSymbols.First(x => x.Id == guid);
                        var posOnCanvas = InverseTransformPositionFloat(ImGui.GetMousePos());
                        var childUi = GraphOperations.AddSymbolChild(symbol, compositionOpSymbolUi, posOnCanvas);

                        var instance = compositionOp.GetChildInstanceWithId(childUi.Id);
                        NodeSelection.SetSelectionToChildUi(childUi, instance);

                        T3Ui.DraggingIsInProgress = false;
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        internal void FocusViewToSelection()
        {
            FitAreaOnCanvas(GetSelectionBounds());
        }

        private ImRect GetSelectionBounds(float padding = 50)
        {
            var selectedOrAll = NodeSelection.IsAnythingSelected()
                                    ? NodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToArray()
                                    : SelectableChildren.ToArray();

            if (selectedOrAll.Length == 0)
                return new ImRect();

            var firstElement = selectedOrAll[0];
            var bounds = new ImRect(firstElement.PosOnCanvas, firstElement.PosOnCanvas + Vector2.One);
            foreach (var element in selectedOrAll)
            {
                bounds.Add(element.PosOnCanvas);
                bounds.Add(element.PosOnCanvas + element.Size);
            }

            bounds.Expand(padding);
            return bounds;
        }

        private void DrawContextMenuContent(bool canModify, Instance compositionOp)
        {
            var clickPosition = ImGui.GetMousePosOnOpeningCurrentPopup();
            var compositionSymbolUi = compositionOp.GetSymbolUi();

            var selectedChildUis = GetSelectedChildUis();
            var nextUndoTitle = UndoRedoStack.CanUndo ? $" ({UndoRedoStack.GetNextUndoTitle()})" : string.Empty;
            if (ImGui.MenuItem("Undo" + nextUndoTitle,
                               shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.Undo, false),
                               selected: false,
                               enabled: UndoRedoStack.CanUndo))
            {
                UndoRedoStack.Undo();
            }

            ImGui.Separator();

            // ------ for selection -----------------------
            var oneOpSelected = selectedChildUis.Count == 1;
            var someOpsSelected = selectedChildUis.Count > 0;
            var snapShotsEnabledFromSomeOps = !selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SnapshotGroupIndex == 0);

            var label = oneOpSelected
                            ? $"{selectedChildUis[0].SymbolChild.ReadableName}..."
                            : $"{selectedChildUis.Count} selected items...";

            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();

            var allSelectedDisabled = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            if (ImGui.MenuItem("Disable",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                               selected: allSelectedDisabled,
                               enabled: selectedChildUis.Count > 0))
            {
                ToggleDisabledForSelectedElements();
            }

            var allSelectedBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
            if (ImGui.MenuItem("Bypassed",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleBypassed, false),
                               selected: allSelectedBypassed,
                               enabled: selectedChildUis.Count > 0))
            {
                ToggleBypassedForSelectedElements();
            }

            if (ImGui.MenuItem("Rename", oneOpSelected))
            {
                RenameInstanceOverlay.OpenForSymbolChildUi(selectedChildUis[0]);
            }

            if (ImGui.MenuItem("Add Comment",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.AddComment, false),
                               selected: false,
                               enabled: oneOpSelected))
            {
                EditCommentDialog.ShowNextFrame();
            }

            if (ImGui.MenuItem("Arrange sub graph",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.LayoutSelection, false),
                               selected: false,
                               enabled: someOpsSelected))
            {
                _nodeGraphLayouting.ArrangeOps(compositionOp);
            }

            if (canModify)
            {
                if (ImGui.MenuItem("Enable for snapshots",
                                   KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleSnapshotControl, false),
                                   selected: snapShotsEnabledFromSomeOps,
                                   enabled: someOpsSelected))
                {
                    // Disable if already enabled for all
                    var enabledForAll = selectedChildUis.TrueForAll(c2 => c2.SnapshotGroupIndex > 0);
                    foreach (var c in selectedChildUis)
                    {
                        c.SnapshotGroupIndex = enabledForAll ? 0 : 1;
                    }

                    compositionSymbolUi.FlagAsModified();
                }
            }

            if (ImGui.BeginMenu("Display as..."))
            {
                if (ImGui.MenuItem("Small", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Default),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Default;
                    }
                }

                if (ImGui.MenuItem("Resizable", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Resizable;
                    }
                }

                if (ImGui.MenuItem("Expanded", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Expanded;
                    }
                }

                ImGui.Separator();

                var isImage = oneOpSelected
                              && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions.Count > 0
                              && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions[0].ValueType == typeof(Texture2D);
                if (ImGui.MenuItem("Set image as graph background",
                                   KeyboardBinding.ListKeyboardShortcuts(UserActions.DisplayImageAsBackground, false),
                                   selected: false,
                                   enabled: isImage))
                {
                    var instance = compositionOp.Children.Single(child => child.SymbolChildId == selectedChildUis[0].Id);
                    _window.GraphImageBackground.OutputInstance = instance;
                }

                if (ImGui.MenuItem("Pin to output", oneOpSelected))
                {
                    PinSelectedToOutputWindow(compositionOp);
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Copy",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.CopyToClipboard, false),
                               selected: false,
                               enabled: someOpsSelected))
            {
                CopySelectedNodesToClipboard(compositionOp);
            }

            if (ImGui.MenuItem("Paste", KeyboardBinding.ListKeyboardShortcuts(UserActions.PasteFromClipboard, false)))
            {
                PasteClipboard(compositionOp);
            }

            var selectedInputUis = GetSelectedInputUis().ToList();
            var selectedOutputUis = GetSelectedOutputUis().ToList();

            if (ImGui.MenuItem("Delete",
                               shortcut: "Del", // dynamic assigned shortcut is too long
                               selected: false,
                               enabled: someOpsSelected || selectedInputUis.Count > 0 || selectedOutputUis.Count > 0))
            {
                DeleteSelectedElements(compositionSymbolUi, canModify, selectedChildUis, selectedInputUis, selectedOutputUis);
            }

            if (ImGui.MenuItem("Duplicate",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.Duplicate, false),
                               selected: false,
                               enabled: selectedChildUis.Count > 0))
            {
                CopySelectedNodesToClipboard(compositionOp);
                PasteClipboard(compositionOp);
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Change Symbol", someOpsSelected))
            {
                var startingSearchString = selectedChildUis[0].SymbolChild.Symbol.Name;
                var position = selectedChildUis.Count == 1 ? selectedChildUis[0].PosOnCanvas : InverseTransformPositionFloat(ImGui.GetMousePos());
                _window.SymbolBrowser.OpenAt(position, null, null, false, startingSearchString,
                                             symbol => { ChangeSymbol.ChangeOperatorSymbol(this, compositionOp, selectedChildUis, symbol); });
            }

            if (ImGui.BeginMenu("Symbol definition..."))
            {
                if (ImGui.MenuItem("Rename Symbol", oneOpSelected))
                {
                    _renameSymbolDialog.ShowNextFrame();
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name;
                    //NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, "NewName");
                }

                if (ImGui.MenuItem("Duplicate as new type...", oneOpSelected))
                {
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name ?? string.Empty;
                    _nameSpaceForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _duplicateSymbolDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Combine into new type...", someOpsSelected))
                {
                    _nameSpaceForDialogEdits = compositionOp.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _combineToSymbolDialog.ShowNextFrame();
                }

                ImGui.EndMenu();
            }
            //}

            if (ImGui.BeginMenu("Add..."))
            {
                if (ImGui.MenuItem("Add Node...", "TAB", false, true))
                {
                    _window.SymbolBrowser.OpenAt(InverseTransformPositionFloat(clickPosition), null, null, false);
                }

                if (canModify)
                {
                    if (ImGui.MenuItem("Add input parameter..."))
                    {
                        _addInputDialog.ShowNextFrame();
                    }

                    if (ImGui.MenuItem("Add output..."))
                    {
                        _addOutputDialog.ShowNextFrame();
                    }
                }

                if (ImGui.MenuItem("Add Annotation",
                                   shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.AddAnnotation, false),
                                   selected: false,
                                   enabled: true))
                {
                    AddAnnotation(compositionOp);
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Export as Executable", oneOpSelected))
            {
                switch (PlayerExporter.TryExportInstance(compositionOp, selectedChildUis.Single(), out var reason, out var exportDir))
                {
                    case false:
                        Log.Error(reason);
                        EditorUi.Instance.ShowMessageBox(reason, $"Failed to export {label}");
                        break;
                    default:
                        Log.Info(reason);
                        EditorUi.Instance.ShowMessageBox(reason, $"Exported {label} successfully!");
                        // open export directory in native file explorer
                        CoreUi.Instance.OpenWithDefaultApplication(exportDir);
                        break;
                }
            }

            if (oneOpSelected)
            {
                var symbol = selectedChildUis.Single().SymbolChild.Symbol;
                CustomComponents.DrawSymbolCodeContextMenuItem(symbol);
                var childUi = selectedChildUis.Single();

                // get instance that is currently selected
                var instance = compositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);

                if (TryGetShaderPath(instance, out var filePath, out var owner))
                {
                    var shaderIsReadOnly = !owner.IsReadOnly;

                    if (ImGui.MenuItem("Open in Shader Editor", true))
                    {
                        if (shaderIsReadOnly)
                        {
                            CopyToTempShaderPath(filePath, out filePath);
                            EditorUi.Instance.ShowMessageBox("Warning - viewing a read-only shader. Modifications will not be saved.\n" +
                                                             "Following #include directives outside of the temp folder may lead you to read-only files, " +
                                                             "and editing those can break operators.\n\nWith great power...", "Warning");
                        }

                        EditorUi.Instance.OpenWithDefaultApplication(filePath);
                    }
                }
            }
        }

        private static void CopyToTempShaderPath(string filePath, out string newFilePath)
        {
            var directory = Path.GetDirectoryName(filePath)!;
            var destinationDirectory = Path.Combine(UserData.TempFolder, "ReadOnlyShaders");

            if (Directory.Exists(destinationDirectory))
            {
                try
                {
                    Directory.Delete(destinationDirectory, true);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to delete temp directory: {e}");
                }
            }

            Directory.CreateDirectory(destinationDirectory);

            // copy all files in directory to temp directory for intellisense to work
            var allFilesInDirectory = Directory.EnumerateFiles(directory);
            foreach (var file in allFilesInDirectory)
            {
                var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(file));
                File.Copy(file, destinationPath);
            }

            ShaderLinter.AddPackage(new ShaderCompiler.ShaderResourcePackage(destinationDirectory), ResourceManager.SharedShaderPackages,
                                    replaceExisting: true);
            newFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
        }

        // Todo: There must be a better way...
        private static bool TryGetShaderPath(Instance instance, out string filePath, out IResourcePackage owner)
        {
            bool found = false;
            if (instance is IShaderOperator<PixelShader> pixelShader)
            {
                found = TryGetSourceFile(pixelShader, out filePath, out owner);
            }
            else if (instance is IShaderOperator<ComputeShader> computeShader)
            {
                found = TryGetSourceFile(computeShader, out filePath, out owner);
            }
            else if (instance is IShaderOperator<GeometryShader> geometryShader)
            {
                found = TryGetSourceFile(geometryShader, out filePath, out owner);
            }
            else if (instance is IShaderOperator<VertexShader> vertexShader)
            {
                found = TryGetSourceFile(vertexShader, out filePath, out owner);
            }
            else
            {
                filePath = null;
                owner = null;
            }

            return found;

            static bool TryGetSourceFile<T>(IShaderOperator<T> op, out string filePath, out IResourcePackage package) where T : class, IDisposable
            {
                if (op.SourceIsSourceCode)
                {
                    package = null;
                    filePath = null;
                    return false;
                }

                var relative = op.Source.GetCurrentValue();
                var instance = op.Instance;
                return ResourceManager.TryResolvePath(relative, instance.AvailableResourcePackages, out filePath, out package);
            }
        }

        private void AddAnnotation(Instance compositionOp)
        {
            var size = new Vector2(100, 140);
            var posOnCanvas = InverseTransformPositionFloat(ImGui.GetMousePos());
            var area = new ImRect(posOnCanvas, posOnCanvas + size);

            if (NodeSelection.IsAnythingSelected())
            {
                for (var index = 0; index < NodeSelection.Selection.Count; index++)
                {
                    var node = NodeSelection.Selection[index];
                    var nodeArea = new ImRect(node.PosOnCanvas,
                                              node.PosOnCanvas + node.Size);

                    if (index == 0)
                    {
                        area = nodeArea;
                    }
                    else
                    {
                        area.Add(nodeArea);
                    }
                }

                area.Expand(60);
            }

            var annotation = new Annotation()
                                 {
                                     Id = Guid.NewGuid(),
                                     Title = "Untitled Annotation",
                                     Color = UiColors.Gray,
                                     PosOnCanvas = area.Min,
                                     Size = area.GetSize()
                                 };
            
            var command = new AddAnnotationCommand(compositionOp.GetSymbolUi(), annotation);
            UndoRedoStack.AddAndExecute(command);

            _graph.RenameAnnotation(annotation);
        }

        private void PinSelectedToOutputWindow(Instance compositionOp)
        {
            var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(ow => ow.Config.Visible) as OutputWindow;
            if (outputWindow == null)
            {
                //Log.Warning("Can't pin selection without visible output window");
                return;
            }

            var selection = GetSelectedChildUis();
            if (selection.Count != 1)
            {
                Log.Info("Please select only one operator to pin to output window");
                return;
            }

            if (compositionOp.TryGetChildInstance(selection[0].Id, false, out var child, out _))
            {
                outputWindow.Pinning.PinInstance(child, this);
            }
        }

        private bool _contextMenuIsOpen;

        private void DeleteSelectedElements(SymbolUi compositionSymbolUi, bool canModify, List<SymbolChildUi> selectedChildUis = null, List<IInputUi> selectedInputUis = null,
                                            List<IOutputUi> selectedOutputUis = null)
        {
            var commands = new List<ICommand>();
            selectedChildUis ??= GetSelectedChildUis();
            if (selectedChildUis.Any())
            {
                var cmd = new DeleteSymbolChildrenCommand(compositionSymbolUi, selectedChildUis);
                commands.Add(cmd);
            }

            foreach (var selectedAnnotation in NodeSelection.GetSelectedNodes<Annotation>())
            {
                var cmd = new DeleteAnnotationCommand(compositionSymbolUi, selectedAnnotation);
                commands.Add(cmd);
            }

            if (canModify)
            {
                selectedInputUis ??= NodeSelection.GetSelectedNodes<IInputUi>().ToList();
                if (selectedInputUis.Count > 0)
                {
                    InputsAndOutputs.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), compositionSymbolUi.Symbol);
                }

                selectedOutputUis ??= NodeSelection.GetSelectedNodes<IOutputUi>().ToList();
                if (selectedOutputUis.Count > 0)
                {
                    InputsAndOutputs.RemoveOutputsFromSymbol(selectedOutputUis.Select(entry => entry.Id).ToArray(), compositionSymbolUi.Symbol);
                }
            }

            var deleteCommand = new MacroCommand("Delete elements", commands);
            UndoRedoStack.AddAndExecute(deleteCommand);
            NodeSelection.Clear();
        }

        private void ToggleDisabledForSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();

            var allSelectedDisabled = selectedChildren.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var shouldDisable = !allSelectedDisabled;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildren)
            {
                commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, shouldDisable));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Disable/Enable", commands));
        }

        private void ToggleBypassedForSelectedElements()
        {
            var selectedChildUis = GetSelectedChildUis();

            var allSelectedAreBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
            var shouldBypass = !allSelectedAreBypassed;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildUis)
            {
                commands.Add(new ChangeInstanceBypassedCommand(selectedChildUi.SymbolChild, shouldBypass));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Changed Bypassed", commands));
        }

        private List<SymbolChildUi> GetSelectedChildUis()
        {
            return NodeSelection.GetSelectedNodes<SymbolChildUi>().ToList();
        }

        private IEnumerable<IInputUi> GetSelectedInputUis()
        {
            return NodeSelection.GetSelectedNodes<IInputUi>();
        }

        private IEnumerable<IOutputUi> GetSelectedOutputUis()
        {
            return NodeSelection.GetSelectedNodes<IOutputUi>();
        }

        private void CopySelectedNodesToClipboard(Instance composition)
        {
            var selectedChildren = NodeSelection.GetSelectedNodes<SymbolChildUi>().ToList();
            var selectedAnnotations = NodeSelection.GetSelectedNodes<Annotation>().ToList();
            if (selectedChildren.Count + selectedAnnotations.Count == 0)
                return;
            
            var resultJsonString = GraphOperations.CopyNodesAsJson(composition, selectedChildren, selectedAnnotations);

            EditorUi.Instance.SetClipboardText(resultJsonString);
        }

        private void PasteClipboard(Instance compositionOp)
        {
            try
            {
                var text = EditorUi.Instance.GetClipboardText();
                using var reader = new StringReader(text);
                var jsonReader = new JsonTextReader(reader);
                if (JToken.ReadFrom(jsonReader, SymbolJson.LoadSettings) is not JArray jArray)
                    return;

                var symbolJson = jArray[0];

                var gotSymbolJson = GetPastedSymbol(symbolJson, compositionOp.Symbol.SymbolPackage, out var containerSymbol);
                if (!gotSymbolJson)
                {
                    Log.Error($"Failed to paste symbol due to invalid symbol json");
                    return;
                }

                if (!SymbolRegistry.EntriesEditable.TryAdd(containerSymbol.Id, containerSymbol))
                    throw new Exception($"Failed to add symbol for {containerSymbol.Name}");

                var symbolUiJson = jArray[1];
                var hasContainerSymbolUi = SymbolUiJson.TryReadSymbolUiExternal(symbolUiJson, out var containerSymbolUi);
                if (!hasContainerSymbolUi)
                {
                    Log.Error($"Failed to paste symbol due to invalid symbol ui json");
                    return;
                }

                var compositionSymbolUi = compositionOp.GetSymbolUi();
                var cmd = new CopySymbolChildrenCommand(containerSymbolUi,
                                                        null,
                                                        containerSymbolUi.Annotations.Values.ToList(),
                                                        compositionSymbolUi,
                                                        InverseTransformPositionFloat(ImGui.GetMousePos()));
                cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ? 
                SymbolRegistry.EntriesEditable.Remove(containerSymbol.Id, out _);

                // Select new operators
                NodeSelection.Clear();

                foreach (var id in cmd.NewSymbolChildIds)
                {
                    var newChildUi = compositionSymbolUi.ChildUis.Single(c => c.Id == id);
                    var instance = compositionOp.Children.Single(c2 => c2.SymbolChildId == id);
                    NodeSelection.AddSymbolChildToSelection(newChildUi, instance);
                }

                foreach (var id in cmd.NewSymbolAnnotationIds)
                {
                    var annotation = compositionSymbolUi.Annotations[id];
                    NodeSelection.AddSelection(annotation);
                }
            }
            catch (Exception e)
            {
                Log.Warning("Could not paste selection from clipboard.");
                Log.Debug("Paste exception: " + e);
            }
        }

        private void DrawGrid(ImDrawListPtr drawList)
        {
            var gridSize = Math.Abs(64.0f * Scale.X);
            for (var x = (-Scroll.X * Scale.X) % gridSize; x < WindowSize.X; x += gridSize)
            {
                drawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos, UiColors.CanvasGrid);
            }

            for (var y = (-Scroll.Y * Scale.Y) % gridSize; y < WindowSize.Y; y += gridSize)
            {
                drawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos, UiColors.CanvasGrid);
            }
        }

        public IEnumerable<ISelectableCanvasObject> SelectableChildren
        {
            get
            {
                _selectableItems.Clear();
                var compositionOp = _window.CompositionOp;
                var symbolUi = compositionOp.GetSymbolUi();
                _selectableItems.AddRange(compositionOp.Children.Select(x =>
                                                                        {
                                                                            var child = x.GetSymbolChildUi();
                                                                            if(child == null)
                                                                                throw new Exception($"Failed to get symbol child ui for {x.SymbolChildId}");
                                                                            return child;
                                                                        }));
                _selectableItems.AddRange(symbolUi.InputUis.Values);
                _selectableItems.AddRange(symbolUi.OutputUis.Values);
                _selectableItems.AddRange(symbolUi.Annotations.Values);

                return _selectableItems;
            }
        }

        private readonly List<ISelectableCanvasObject> _selectableItems = new();
        #endregion

        // todo - better encapsulate this in SymbolJson
        private static bool GetPastedSymbol(JToken jToken, SymbolPackage package, out Symbol symbol)
        {
            var guidString = jToken[SymbolJson.JsonKeys.Id].Value<string>();
            var hasId = Guid.TryParse(guidString, out var guid);

            if (!hasId)
            {
                Log.Error($"Failed to parse guid in symbol json: `{guidString}`");
                symbol = null;
                return false;
            }

            symbol = EditorSymbolPackage.AllSymbols.FirstOrDefault(x => x.Id == guid);

            // is this really necessary? just bringing things into parity with what was previously there, but I feel like
            // everything below can be skipped, unless "allowNonOperatorInstanceType" actually matters
            if (symbol != null)
                return true;

            var jsonResult = SymbolJson.ReadSymbolRoot(guid, jToken, typeof(object), package);

            if (jsonResult.Symbol is null)
                return false;

            if (SymbolJson.TryReadAndApplySymbolChildren(jsonResult))
            {
                symbol = jsonResult.Symbol;
                return true;
            }

            Log.Error($"Failed to get children of pasted token:\n{jToken}");
            return false;
        }

        #region public API
        public bool Destroyed { get; private set; }
        #endregion

        private readonly AddInputDialog _addInputDialog = new();
        private readonly AddOutputDialog _addOutputDialog = new();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new();
        private readonly RenameSymbolDialog _renameSymbolDialog = new();
        public readonly EditNodeOutputDialog EditNodeOutputDialog = new();
        public static readonly EditCommentDialog EditCommentDialog = new();
        public static readonly LibWarningDialog LibWarningDialog = new();

        private string _symbolNameForDialogEdits = "";
        private string _symbolDescriptionForDialog = "";
        private string _nameSpaceForDialogEdits = "";
        private readonly GraphWindow _window;
        public GraphWindow Window => _window;
        private static Vector2 _dampedScrollVelocity = Vector2.Zero;
        internal readonly NavigationHistory NavigationHistory;
        internal readonly Structure Structure;
        internal readonly NodeNavigation NodeNavigation;
        internal readonly NodeSelection NodeSelection;
        private readonly NodeGraphLayouting _nodeGraphLayouting;
        private readonly Graph _graph;
        internal readonly SelectableNodeMovement SelectableNodeMovement;
    }

    public enum GraphHoverModes
    {
        Disabled,
        Live,
        LastValue,
    }
}