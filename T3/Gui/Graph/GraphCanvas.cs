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
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas
    {
        public GraphCanvas(GraphWindow window, List<Guid> idPath)
        {
            _selectionFence = new SelectionFence(this);
            _window = window;
            SetComposition(idPath, Transition.JumpIn);
        }

        public void SetComposition(List<Guid> childIdPath, Transition transition)
        {
            var previousFocusOnScreen = WindowPos + WindowSize / 2;
            
            var previousInstanceWasSet = _compositionPath != null && _compositionPath.Count > 0;
            if (previousInstanceWasSet)
            {
                var previousInstance = GetInstanceFromIdPath(_compositionPath);
                UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId] = GetTargetProperties();
                
                var newUiContainer = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var matchingChildUi = newUiContainer.ChildUis.FirstOrDefault(childUi => childUi.SymbolChild.Id == previousInstance.SymbolChildId);
                if (matchingChildUi != null)
                {
                    var centerOnCanvas = matchingChildUi.PosOnCanvas + matchingChildUi.Size / 2;
                    previousFocusOnScreen = TransformPosition(centerOnCanvas);
                }
            }
            
            _compositionPath = childIdPath;
            CompositionOp = GetInstanceFromIdPath(childIdPath);
            
            SelectionHandler.Clear();

            UserSettings.SaveLastViewedOpForWindow(_window, CompositionOp.SymbolChildId);

            var newProps = UserSettings.Config.OperatorViewSettings.ContainsKey(CompositionOp.SymbolChildId)
                               ? UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId]
                               : GuessViewProperties();
            
            SetAreaWithTransition(newProps.Scale, newProps.Scroll, previousFocusOnScreen, transition);
        }


        public void SetCompositionToChildInstance(Instance instance)
        {
            // Validation that instance is valid
            // TODO: only do in debug mode
            var op = GetInstanceFromIdPath(_compositionPath);
            var matchingChild = op.Children.Single(child => child == instance);
            if (matchingChild == null)
            {
                throw new ArgumentException("Can't OpenChildNode because Instance is not a child of current composition");
            }

            var newPath = _compositionPath;
            newPath.Add(instance.SymbolChildId);
            SetComposition(newPath, Transition.JumpIn);
        }
        
        public void SetCompositionToParentInstance(Instance instance)
        {
            var shortenedPath = new List<Guid>();
            foreach (var pathItemId in _compositionPath)
            {
                if (pathItemId == instance.SymbolChildId)
                    break;
                
                shortenedPath.Add(pathItemId);
            }
            shortenedPath.Add(instance.SymbolChildId);
            
            if(shortenedPath.Count() == _compositionPath.Count())
                throw new ArgumentException("Can't SetCompositionToParentInstance because Instance is not a parent of current composition");

            SetComposition(shortenedPath, Transition.JumpOut);
        }        
        
        private CanvasProperties GuessViewProperties()
        {
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            FocusViewToSelection();
            return GetTargetProperties();
        }

        #region drawing UI ====================================================================
        public void Draw(ImDrawListPtr dl)
        {
            // TODO: Refresh reference on every frame. Since this uses lists instead of dictionary
            // it can be really slow
            CompositionOp = GetInstanceFromIdPath(_compositionPath);

            UpdateCanvas();

            Current = this;
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

                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                DrawGrid();
                _symbolBrowser.Draw();

                Graph.DrawGraph(DrawList);

                if (ConnectionMaker.TempConnection != null && ImGui.IsMouseReleased(0))
                {
                    var droppedOnBackground = ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered();
                    if (droppedOnBackground)
                    {
                        ConnectionMaker.InitSymbolBrowserAtPosition(
                                                                    _symbolBrowser,
                                                                    InverseTransformPosition(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        ConnectionMaker.Cancel();
                    }
                }

                _selectionFence.Draw();
                DrawList.PopClipRect();
                DrawContextMenu();

                _duplicateSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpace, ref _combineName);
                _combineToSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpace, ref _combineName);

                var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
                _addInputDialog.Draw(selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : CompositionOp.Symbol);
            }
            ImGui.EndGroup();
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
                        SelectionHandler.SetElement(childUi);

                        T3Ui.DraggingIsInProgress = false;
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        public IEnumerable<Instance> GetParents(bool includeCompositionOp = false)
        {
            var parents = new List<Instance>();
            var op = CompositionOp;
            if (includeCompositionOp)
                parents.Add(op);

            while (op.Parent != null)
            {
                op = op.Parent;
                parents.Insert(0, op);
            }

            return parents;
        }

        public IEnumerable<Symbol> GetParentSymbols()
        {
            return GetParents(includeCompositionOp: true).Select(p => p.Symbol);
        }

        private void FocusViewToSelection()
        {
            FitAreaOnCanvas(GetSelectionBounds());
        }

        private ImRect GetSelectionBounds(float padding = 50)
        {
            var selectedOrAll = !SelectionHandler.SelectedElements.Any()
                                    ? SelectableChildren.ToArray()
                                    : SelectionHandler.SelectedElements.ToArray();

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
            CustomComponents.DrawContextMenuForScrollCanvas
                (
                 () =>
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
                             if (ImGui.MenuItem("Default", "", selectedChildUis.Any(child => child.Style == SymbolUi.Styles.Default)))
                             {
                                 foreach (var childUi in selectedChildUis)
                                 {
                                     childUi.Style = SymbolUi.Styles.Default;
                                 }
                             }

                             if (ImGui.MenuItem("Resizable", "", selectedChildUis.Any(child => child.Style == SymbolUi.Styles.Resizable)))
                             {
                                 foreach (var childUi in selectedChildUis)
                                 {
                                     childUi.Style = SymbolUi.Styles.Resizable;
                                 }
                             }
                             
                             if (ImGui.MenuItem("Expanded", "", selectedChildUis.Any(child => child.Style == SymbolUi.Styles.Resizable)))
                             {
                                 foreach (var childUi in selectedChildUis)
                                 {
                                     childUi.Style = SymbolUi.Styles.Expanded;
                                 }
                             }

                             ImGui.EndMenu();
                         }

                         if (ImGui.MenuItem("Delete"))
                         {
                             DeleteSelectedElements();
                         }

                         if (ImGui.MenuItem("Duplicate as new type", oneElementSelected))
                         {
                             _combineName = selectedChildUis[0].SymbolChild.Symbol.Name;
                             _nameSpace = selectedChildUis[0].SymbolChild.Symbol.Namespace;
                             _duplicateSymbolDialog.ShowNextFrame();
                         }

                         if (ImGui.MenuItem("Combine as new type"))
                         {
                             _combineToSymbolDialog.ShowNextFrame();
                         }

                         if (ImGui.MenuItem("Copy"))
                         {
                             CopySelectionToClipboard(selectedChildUis);
                         }

                         ImGui.Separator();
                     }

                     var selectedInputUis = GetSelectableInputUis();
                     if (selectedInputUis.Count > 0)
                     {
                         var oneElementSelected = selectedInputUis.Count == 1;
                         var label = oneElementSelected
                                         ? $"Input {selectedInputUis[0].InputDefinition.Name}..."
                                         : $"Selected {selectedInputUis.Count} inputs...";

                         ImGui.PushFont(Fonts.FontSmall);
                         ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                         ImGui.Text(label);
                         ImGui.PopStyleColor();
                         ImGui.PopFont();
                         if (ImGui.MenuItem("Remove input(s)", enabled: false))
                         {
                             // TODO: Please implement
                         }
                     }

                     if (ImGui.MenuItem("Add input parameter"))
                     {
                         _addInputDialog.ShowNextFrame();
                     }

                     if (ImGui.MenuItem("Paste"))
                     {
                         PasteClipboard();
                     }

                     if (ImGui.MenuItem("Add"))
                     {
                         _symbolBrowser.OpenAt(InverseTransformPosition(ImGui.GetMousePos()), null, null);
                     }
                 }, ref _contextMenuIsOpen);
        }

        private bool _contextMenuIsOpen;

        private void DeleteSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();
            if (!selectedChildren.Any())
                return;

            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            var cmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
            UndoRedoStack.AddAndExecute(cmd);
        }

        private List<SymbolChildUi> GetSelectedChildUis()
        {
            var selectedChildren = new List<SymbolChildUi>();
            foreach (var x in SelectionHandler.SelectedElements)
            {
                if (x is SymbolChildUi childUi)
                {
                    selectedChildren.Add(childUi);
                }
            }

            return selectedChildren;
        }

        private List<IInputUi> GetSelectableInputUis()
        {
            var selectedInputs = new List<IInputUi>();

            //_selectedChildren = selectedChildren;
            foreach (var x in SelectionHandler.SelectedElements)
            {
                if (x is IInputUi inputUi)
                {
                    selectedInputs.Add(inputUi);
                }
            }

            return selectedInputs;
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
                    Log.Info(Clipboard.GetText(TextDataFormat.UnicodeText));
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
                    var containerSymbol = json.ReadSymbol(null, symbolJson);
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);

                    var symbolUiJson = o[1];
                    var containerSymbolUi = UiJson.ReadSymbolUi(symbolUiJson);
                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                    SymbolUiRegistry.Entries.Add(containerSymbolUi.Symbol.Id, containerSymbolUi);
                    var cmd = new CopySymbolChildrenCommand(containerSymbolUi, null, compositionSymbolUi,
                                                            InverseTransformPosition(ImGui.GetMousePos()));
                    cmd.Do();
                    SymbolUiRegistry.Entries.Remove(containerSymbolUi.Symbol.Id);
                    SymbolRegistry.Entries.Remove(containerSymbol.Id);
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

        public override IEnumerable<ISelectable> SelectableChildren
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

        private readonly List<ISelectable> _selectableItems = new List<ISelectable>();
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

        private static Instance GetInstanceFromIdPath(IEnumerable<Guid> childPath)
        {
            if (childPath == null || childPath.Count() == 0)
                return null;
            
            var instance = T3Ui.UiModel.RootInstance;
            foreach (var childId in childPath)
            {
                if (childId == T3Ui.UiModel.RootInstance.SymbolChildId)
                    continue;
                
                instance = instance.Children.FirstOrDefault(child => child.SymbolChildId == childId);
                if(instance == null)
                    throw new Exception("Invalid composition path");
            }
            return instance;
        }

        
        private  List<Guid> _compositionPath = new List<Guid>();
        
        private readonly AddInputDialog _addInputDialog = new AddInputDialog();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new CombineToSymbolDialog();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new DuplicateSymbolDialog();
        public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private readonly SelectionFence _selectionFence;
        private List<SymbolChildUi> ChildUis { get; set; }
        private readonly SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private string _combineName = "";
        private string _nameSpace = "";
        private readonly GraphWindow _window;

        public static HoverModes HoverMode { get; set; } = HoverModes.Live;
        public enum HoverModes
        {
            Disabled,
            Live,
            LastValue,
        }
    }
}