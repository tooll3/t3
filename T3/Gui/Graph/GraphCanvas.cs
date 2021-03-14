using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas, INodeCanvas
    {
        public GraphCanvas(GraphWindow window, List<Guid> idPath)
        {
            //_selectionFence = new SelectionFence(this);
            _window = window;
            SetComposition(idPath, Transition.JumpIn);
        }

        public void SetComposition(List<Guid> childIdPath, Transition transition)
        {
            var previousFocusOnScreen = WindowPos + WindowSize / 2;

            var previousInstanceWasSet = _compositionPath != null && _compositionPath.Count > 0;
            if (previousInstanceWasSet)
            {
                var previousInstance = NodeOperations.GetInstanceFromIdPath(_compositionPath);
                UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId] = GetTargetScope();

                var newUiContainer = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var matchingChildUi = newUiContainer.ChildUis.FirstOrDefault(childUi => childUi.SymbolChild.Id == previousInstance.SymbolChildId);
                if (matchingChildUi != null)
                {
                    var centerOnCanvas = matchingChildUi.PosOnCanvas + matchingChildUi.Size / 2;
                    previousFocusOnScreen = TransformPosition(centerOnCanvas);
                }
            }

            _compositionPath = childIdPath;
            var comp = NodeOperations.GetInstanceFromIdPath(childIdPath);
            if (comp == null)
            {
                Log.Error("Can't resolve instance for id-path " + childIdPath);
                return;
            }
            CompositionOp = comp; 

            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();

            var newProps = GuessViewProperties();
            if (CompositionOp != null)
            {
                UserSettings.SaveLastViewedOpForWindow(_window, CompositionOp.SymbolChildId);
                if (UserSettings.Config.OperatorViewSettings.ContainsKey(CompositionOp.SymbolChildId))
                    newProps = UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId];
            }
            
            SetScopeWithTransition(newProps.Scale, newProps.Scroll, previousFocusOnScreen, transition);
        }

        public void SetCompositionToChildInstance(Instance instance)
        {
            // Validation that instance is valid
            // TODO: only do in debug mode
            var op = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            var matchingChild = op.Children.SingleOrDefault(child => child == instance);
            if (matchingChild == null)
            {
                throw new ArgumentException("Can't OpenChildNode because Instance is not a child of current composition");
            }

            var newPath = _compositionPath;
            newPath.Add(instance.SymbolChildId);
            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            SetComposition(newPath, Transition.JumpIn);
        }

        public void SetCompositionToParentInstance(Instance instance)
        {
            if (instance == null)
            {
                Log.Warning("can't jump to parent with invalid instance");
                return;
            }

            var previousCompositionOp = CompositionOp;
            var shortenedPath = new List<Guid>();
            foreach (var pathItemId in _compositionPath)
            {
                if (pathItemId == instance.SymbolChildId)
                    break;

                shortenedPath.Add(pathItemId);
            }

            shortenedPath.Add(instance.SymbolChildId);

            if (shortenedPath.Count() == _compositionPath.Count())
                throw new ArgumentException("Can't SetCompositionToParentInstance because Instance is not a parent of current composition");

            SetComposition(shortenedPath, Transition.JumpOut);
            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            var previousCompChildUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis
                                                      .SingleOrDefault(childUi => childUi.Id == previousCompositionOp.SymbolChildId);
            if (previousCompChildUi != null)
                SelectionManager.AddSymbolChildToSelection(previousCompChildUi, previousCompositionOp);
        }

        private Scope GuessViewProperties()
        {
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            FocusViewToSelection();
            return GetTargetScope();
        }

        public void MakeCurrent()
        {
            Current = this;
        }

        #region drawing UI ====================================================================
        public void Draw(ImDrawListPtr dl, bool showGrid)
        {
            // TODO: Refresh reference on every frame. Since this uses lists instead of dictionary
            // it can be really slow
            CompositionOp = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            if (CompositionOp == null)
            {
                Log.Error("unable to get composition op");
                return;
            }
            UpdateCanvas();
            if (this.CompositionOp == null)
            {
                Log.Error("Can't show graph for undefined CompositionOp");
                return;
            }
            GraphBookmarkNavigation.HandleForCanvas(this);

            MakeCurrent();
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            DrawList = dl;
            ImGui.BeginGroup();
            {
                DrawDropHandler();

                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    FocusViewToSelection();

                if (KeyboardBinding.Triggered(UserActions.Duplicate))
                {
                    var selectedChildren = GetSelectedChildUis();
                    CopySelectionToClipboard(selectedChildren);
                    PasteClipboard();
                }

                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();

                if (KeyboardBinding.Triggered(UserActions.CopyToClipboard))
                {
                    var selectedChildren = GetSelectedChildUis();
                    if (selectedChildren.Any())
                        CopySelectionToClipboard(selectedChildren);
                }

                if (KeyboardBinding.Triggered(UserActions.PasteFromClipboard))
                {
                    PasteClipboard();
                }

                if (KeyboardBinding.Triggered(UserActions.LayoutSelection))
                {
                    SelectableNodeMovement.ArrangeOps();
                }

                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                if(showGrid)
                    DrawGrid();
                
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    ConnectionMaker.ConnectionSplitHelper.PrepareNewFrame(this);
                }
                _symbolBrowser.Draw();
                
                Graph.DrawGraph(DrawList);
                RenameInstanceOverlay.Draw();
                HandleFenceSelection();

                var isOnBackground = ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive();
                if (isOnBackground && ImGui.IsMouseDoubleClicked(0))
                {
                    SetCompositionToParentInstance(CompositionOp.Parent);
                }

                if (ConnectionMaker.TempConnections.Count > 0 && ImGui.IsMouseReleased(0))
                {
                    var isAnyItemHovered = ImGui.IsAnyItemHovered();
                    var droppedOnBackground = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && !isAnyItemHovered;
                    if (droppedOnBackground)
                    {
                        ConnectionMaker.InitSymbolBrowserAtPosition(_symbolBrowser,
                                                                    InverseTransformPosition(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        if (ConnectionMaker.TempConnections[0].GetStatus() != ConnectionMaker.TempConnection.Status.TargetIsDraftNode)
                        {
                            ConnectionMaker.Cancel();
                        }
                    }
                }

                DrawList.PopClipRect();
                DrawContextMenu();

                _duplicateSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits, ref _symbolDescriptionForDialog);
                _combineToSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits, ref _symbolDescriptionForDialog);
                _renameSymbolDialog.Draw(GetSelectedChildUis(), ref _symbolNameForDialogEdits);
                _addInputDialog.Draw(CompositionOp.Symbol);
                _addOutputDialog.Draw(CompositionOp.Symbol);
                EditNodeOutputDialog.Draw();
            }
            ImGui.EndGroup();
            Current = null;
        }

        private void HandleFenceSelection()
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.PressedButNotMoved:
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                        SelectionManager.Clear();
                    break;

                case SelectionFence.States.Updated:
                    HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                    break;

                case SelectionFence.States.CompletedAsClick:
                    SelectionManager.Clear();
                    SelectionManager.SetSelectionToParent(CompositionOp);
                    break;
            }
        }
        
        private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

        private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
        {
            var boundsInCanvas = InverseTransformRect(boundsInScreen);
            var nodesToSelect = (from child in SelectableChildren
                                 let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                 where rect.Overlaps(boundsInCanvas)
                                 select child).ToList();


            SelectionManager.Clear();
            foreach (var node in nodesToSelect)
            {
                if (node is SymbolChildUi symbolChildUi)
                {
                    var instance = CompositionOp.Children.FirstOrDefault(child => child.SymbolChildId == symbolChildUi.Id);
                    if (instance == null)
                    {
                        Log.Warning("Can't find instance");
                    }

                    SelectionManager.AddSymbolChildToSelection(symbolChildUi, instance);
                }
                else
                {
                    SelectionManager.AddSelection(node);
                }
            }
        }
        
        /// <remarks>
        /// This method is completed, because it has to handle several edge cases and has potential to remove previous user data:
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
            ConnectionMaker.InitSymbolBrowserAtPosition(_symbolBrowser,
                                        childUi.PosOnCanvas + new Vector2(childUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0));
        }
        

        private Symbol GetSelectedSymbol()
        {
            var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
            return selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : CompositionOp.Symbol;
        }


        private void DrawDropHandler()
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

                        var symbol = SymbolRegistry.Entries[guid];
                        var parent = CompositionOp.Symbol;
                        var posOnCanvas = InverseTransformPosition(ImGui.GetMousePos());
                        var childUi = NodeOperations.CreateInstance(symbol, parent, posOnCanvas);

                        var instance = CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
                        SelectionManager.SetSelectionToChildUi(childUi, instance);

                        T3Ui.DraggingIsInProgress = false;
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        public IEnumerable<Symbol> GetParentSymbols()
        {
            return NodeOperations.GetParentInstances(CompositionOp, includeChildInstance: true).Select(p => p.Symbol);
        }

        private void FocusViewToSelection()
        {
            FitAreaOnCanvas(GetSelectionBounds());
        }

        private ImRect GetSelectionBounds(float padding = 50)
        {
            var selectedOrAll = SelectionManager.IsAnythingSelected()
                                    ? SelectionManager.GetSelectedNodes<ISelectableNode>().ToArray()
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

        private void DrawContextMenu()
        {
            if (T3Ui.OpenedPopUpName == string.Empty)
            {
                CustomComponents.DrawContextMenuForScrollCanvas(DrawContextMenuContent, ref _contextMenuIsOpen);
            }
        }

        private void DrawContextMenuContent()
        {
            var selectedChildUis = GetSelectedChildUis();
            if (selectedChildUis.Count > 0)
            {
                bool oneElementSelected = selectedChildUis.Count == 1;
                var label = oneElementSelected
                                ? $"Selected {selectedChildUis[0].SymbolChild.ReadableName}..."
                                : $"Selected {selectedChildUis.Count} items...";

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                ImGui.Text(label);
                ImGui.PopStyleColor();
                ImGui.PopFont();

                if (ImGui.BeginMenu("Styles"))
                {
                    if (ImGui.MenuItem("Default", "", selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Default)))
                    {
                        foreach (var childUi in selectedChildUis)
                        {
                            childUi.Style = SymbolChildUi.Styles.Default;
                        }
                    }

                    if (ImGui.MenuItem("Resizable", "", selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable)))
                    {
                        foreach (var childUi in selectedChildUis)
                        {
                            childUi.Style = SymbolChildUi.Styles.Resizable;
                        }
                    }

                    if (ImGui.MenuItem("Expanded", "", selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable)))
                    {
                        foreach (var childUi in selectedChildUis)
                        {
                            childUi.Style = SymbolChildUi.Styles.Expanded;
                        }
                    }

                    ImGui.EndMenu();
                }

                const bool allSelectedDisabled = false;
                const bool allSelectedEnabled = false;
                if (!allSelectedDisabled && ImGui.MenuItem("Disable"))
                {
                    // TODO: @cynic needs implementation
                    Log.Assert("Would disable selected ops");
                }

                if (!allSelectedEnabled && ImGui.MenuItem("Enable"))
                {
                    // TODO: @cynic needs implementation
                    Log.Assert("Would enable selected ops");
                }

                if (ImGui.MenuItem("Delete"))
                {
                    DeleteSelectedElements();
                }

                if (ImGui.MenuItem("Rename", oneElementSelected))
                {
                    RenameInstanceOverlay.OpenForSymbolChildUi(selectedChildUis[0]);
                }
                
                if (ImGui.MenuItem("Rename Symbol", oneElementSelected))
                {
                    _renameSymbolDialog.ShowNextFrame();
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name;
                    //NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, "NewName");
                }

                if (ImGui.MenuItem("Duplicate as new type", oneElementSelected))
                {
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name ?? string.Empty;
                    _nameSpaceForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _duplicateSymbolDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Combine as new type"))
                {
                    _nameSpaceForDialogEdits = CompositionOp.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _combineToSymbolDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Copy"))
                {
                    CopySelectionToClipboard(selectedChildUis);
                }
                
                if (oneElementSelected && ImGui.MenuItem("Set at background image"))
                {
                    var instance =CompositionOp.Children.Single(child => child.SymbolChildId == selectedChildUis[0].Id);
                    GraphWindow.SetBackgroundOutput(instance);
                }
                ImGui.Separator();
            }

            var selectedInputUis = GetSelectedInputUis().ToArray();
            if (selectedInputUis.Length > 0)
            {
                var oneElementSelected = selectedInputUis.Length == 1;
                var label = oneElementSelected
                                ? $"Input {selectedInputUis[0].InputDefinition.Name}..."
                                : $"Selected {selectedInputUis.Length} inputs...";

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                ImGui.Text(label);
                ImGui.PopStyleColor();
                ImGui.PopFont();

                if (ImGui.MenuItem("Remove input(s)"))
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), symbol);
                }
            }

            var selectedOutputUis = GetSelectedOutputUis().ToArray();
            if (selectedOutputUis.Length > 0)
            {
                var oneElementSelected = selectedOutputUis.Length == 1;
                var label = oneElementSelected
                                ? $"Output {selectedOutputUis[0].OutputDefinition.Name}..."
                                : $"Selected {selectedOutputUis.Length} outputs...";

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                ImGui.Text(label);
                ImGui.PopStyleColor();
                ImGui.PopFont();

                if (ImGui.MenuItem("Remove output(s)"))
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveOutputsFromSymbol(selectedOutputUis.Select(entry => entry.Id).ToArray(), symbol);
                }
            }

            if (ImGui.MenuItem("Add Node"))
            {
                _symbolBrowser.OpenAt(InverseTransformPosition(ImGui.GetMousePos()), null, null, false, null);
            }

            if (ImGui.MenuItem("Add input parameter"))
            {
                _addInputDialog.ShowNextFrame();
            }

            if (ImGui.MenuItem("Add output"))
            {
                _addOutputDialog.ShowNextFrame();
            }

            if (ImGui.MenuItem("Paste"))
            {
                PasteClipboard();
            }
        }

        private bool _contextMenuIsOpen;

        private void DeleteSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();
            if (selectedChildren.Any())
            {
                var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var cmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
                UndoRedoStack.AddAndExecute(cmd);
            }

            var selectedInputUis = SelectionManager.GetSelectedNodes<IInputUi>().ToList();
            if (selectedInputUis.Count > 0)
            {
                NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), CompositionOp.Symbol);
            }

            SelectionManager.Clear();
        }

        private static List<SymbolChildUi> GetSelectedChildUis()
        {
            return SelectionManager.GetSelectedNodes<SymbolChildUi>().ToList();
        }

        private IEnumerable<IInputUi> GetSelectedInputUis()
        {
            return SelectionManager.GetSelectedNodes<IInputUi>();
        }

        private IEnumerable<IOutputUi> GetSelectedOutputUis()
        {
            return SelectionManager.GetSelectedNodes<IOutputUi>();
        }

        private void CopySelectionToClipboard(List<SymbolChildUi> selectedChildren)
        {
            var containerOp = new Symbol(typeof(object), Guid.NewGuid());
            var newContainerUi = new SymbolUi(containerOp);
            SymbolUiRegistry.Entries.Add(newContainerUi.Symbol.Id, newContainerUi);

            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, selectedChildren, newContainerUi,
                                                    InverseTransformPosition(ImGui.GetMousePos()));
            cmd.Do();

            using (var writer = new StringWriter())
            {
                var json = new Json { Writer = new JsonTextWriter(writer) { Formatting = Formatting.Indented } };
                json.Writer.WriteStartArray();

                json.WriteSymbol(containerOp);

                var jsonUi = new UiJson { Writer = json.Writer };
                jsonUi.WriteSymbolUi(newContainerUi);

                json.Writer.WriteEndArray();

                try
                {
                    Clipboard.SetText(writer.ToString(), TextDataFormat.UnicodeText);
                    //Log.Info(Clipboard.GetText(TextDataFormat.UnicodeText));
                }
                catch (Exception)
                {
                    Log.Error("Could not copy elements to clipboard. Perhaps a tool like TeamViewer locks it.");
                }
            }

            SymbolUiRegistry.Entries.Remove(newContainerUi.Symbol.Id);
        }

        private void PasteClipboard()
        {
            try
            {
                var text = Clipboard.GetText();
                using (var reader = new StringReader(text))
                {
                    var json = new Json { Reader = new JsonTextReader(reader) };
                    if (!(JToken.ReadFrom(json.Reader) is JArray o))
                        return;

                    var symbolJson = o[0];
                    var containerSymbol = json.ReadSymbol(null, symbolJson, true);
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);

                    var symbolUiJson = o[1];
                    var containerSymbolUi = UiJson.ReadSymbolUi(symbolUiJson);
                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                    SymbolUiRegistry.Entries.Add(containerSymbolUi.Symbol.Id, containerSymbolUi);
                    var cmd = new CopySymbolChildrenCommand(containerSymbolUi, null, compositionSymbolUi,
                                                            InverseTransformPosition(ImGui.GetMousePos()));
                    cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ? 
                    SymbolUiRegistry.Entries.Remove(containerSymbolUi.Symbol.Id);
                    SymbolRegistry.Entries.Remove(containerSymbol.Id);
                    
                    // Select new operators
                    SelectionManager.Clear();
                    
                    foreach (var id in cmd.NewSymbolChildIds)
                    {
                        var newChildUi = compositionSymbolUi.ChildUis.Single(c => c.Id == id);
                        var instance = CompositionOp.Children.Single(c2 => c2.SymbolChildId == id);
                        SelectionManager.AddSymbolChildToSelection(newChildUi, instance);
                    }
                }
            }
            catch (Exception)
            {
                Log.Warning("Could not copy actual selection to clipboard.");
            }
        }

        private void DrawGrid()
        {
            var color = new Color(0, 0, 0, 0.3f);
            var gridSize = Math.Abs(64.0f * Scale.X);
            for (var x = Scroll.X % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 color);
            }

            for (var y = Scroll.Y % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos,
                                 color);
            }
        }

        public IEnumerable<ISelectableNode> SelectableChildren
        {
            get
            {
                _selectableItems.Clear();
                _selectableItems.AddRange(ChildUis);
                var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                _selectableItems.AddRange(symbolUi.InputUis.Values);
                _selectableItems.AddRange(symbolUi.OutputUis.Values);

                return _selectableItems;
            }
        }

        private readonly List<ISelectableNode> _selectableItems = new List<ISelectableNode>();
        #endregion

        #region public API
        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public ImDrawListPtr DrawList { get; private set; }
        public Instance CompositionOp { get; private set; }
        #endregion

        private List<Guid> _compositionPath = new List<Guid>();

        private readonly AddInputDialog _addInputDialog = new AddInputDialog();
        private readonly AddOutputDialog _addOutputDialog = new AddOutputDialog();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new CombineToSymbolDialog();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new DuplicateSymbolDialog();
        private readonly RenameSymbolDialog _renameSymbolDialog = new RenameSymbolDialog();
        public readonly EditNodeOutputDialog EditNodeOutputDialog = new EditNodeOutputDialog();

        //public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private List<SymbolChildUi> ChildUis { get; set; }
        public readonly SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private string _symbolNameForDialogEdits = "";
        private string _symbolDescriptionForDialog = "";
        private string _nameSpaceForDialogEdits = "";
        private readonly GraphWindow _window;

        public enum HoverModes
        {
            Disabled,
            Live,
            LastValue,
        }
    }
}
