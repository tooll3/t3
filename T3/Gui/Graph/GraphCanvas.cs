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
using T3.Gui.Graph.Interaction;
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
        public GraphCanvas(GraphWindow window, Instance opInstance)
        {
            _selectionFence = new SelectionFence(this);
            _window = window;
            OpenComposition(opInstance, zoomIn: true);
        }

        public void OpenComposition(Instance opInstance, bool zoomIn = true)
        {
            // save old properties
            if (CompositionOp != null)
            {
                UserSettings.Config.OperatorViewSettings[CompositionOp.Id] = GetTargetProperties();
            }

            SelectionHandler.Clear();
            CompositionOp = opInstance;

            var newProps = UserSettings.Config.OperatorViewSettings.ContainsKey(CompositionOp.Id)
                               ? UserSettings.Config.OperatorViewSettings[CompositionOp.Id]
                               : GuessViewProperties();

            UserSettings.SaveLastViewedOpForWindow(_window, opInstance.Id);
            ApplyProperties(newProps, zoomIn);
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

                _duplicateSymbolDialog.Draw(() =>
                                            {
                                                ImGui.PushFont(Fonts.FontSmall);
                                                ImGui.Text("Namespace");
                                                ImGui.SameLine();
                                                
                                                ImGui.SetCursorPosX(250+20); // Not sure how else to layout this
                                                ImGui.Text("Name");
                                                ImGui.PopFont();

                                                ImGui.SetNextItemWidth(250);
                                                ImGui.InputText("##namespace", ref _nameSpace, 255);

                                                ImGui.SetNextItemWidth(150);
                                                ImGui.SameLine();

                                                if (ImGui.IsWindowAppearing())
                                                    ImGui.SetKeyboardFocusHere();

                                                ImGui.InputText("##name", ref _combineName, 255);

                                                CustomComponents
                                                   .HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                                                ImGui.Spacing();

                                                if (CustomComponents.DisablableButton("Duplicate", NodeOperations.IsNewSymbolNameValid(_combineName)))
                                                {
                                                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                                                    NodeOperations.DuplicateAsNewType(compositionSymbolUi, 
                                                                                      GetSelectedChildUis()[0].SymbolChild, 
                                                                                      combineName: _combineName, 
                                                                                      nameSpace: _nameSpace);
                                                    ImGui.CloseCurrentPopup();
                                                }

                                                ImGui.SameLine();
                                                if (ImGui.Button("Cancel"))
                                                {
                                                    ImGui.CloseCurrentPopup();
                                                }
                                            });

                _combineDialog.Draw(() =>
                                    {
                                        ImGui.PushFont(Fonts.FontSmall);
                                        ImGui.Text("Namespace");
                                        ImGui.SameLine();
                                                
                                        ImGui.SetCursorPosX(250+20); // Not sure how else to layout this
                                        ImGui.Text("Name");
                                        ImGui.PopFont();

                                        ImGui.SetNextItemWidth(250);
                                        ImGui.InputText("##namespace", ref _nameSpace, 255);

                                        ImGui.SetNextItemWidth(150);
                                        ImGui.SameLine();

                                        if (ImGui.IsWindowAppearing())
                                            ImGui.SetKeyboardFocusHere();

                                        ImGui.InputText("##name", ref _combineName, 255);


                                        CustomComponents.HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                                        ImGui.Spacing();

                                        if (CustomComponents.DisablableButton("Combine", NodeOperations.IsNewSymbolNameValid(_combineName)))
                                        {
                                            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                                            NodeOperations.CombineAsNewType(compositionSymbolUi, _selectedChildren, _combineName, _nameSpace);
                                            ImGui.CloseCurrentPopup();
                                        }

                                        ImGui.SameLine();
                                        if (ImGui.Button("Cancel"))
                                        {
                                            ImGui.CloseCurrentPopup();
                                        }
                                    });
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

        private bool _contextMenuIsOpen;
        private List<SymbolChildUi> _selectedChildren;

        private void FocusViewToSelection()
        {
            FitArea(GetSelectionBounds());
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
                     var selectedChildren = GetSelectedChildUis();

                     if (selectedChildren.Count > 0)
                     {
                         bool oneElementSelected = selectedChildren.Count == 1;
                         var label = oneElementSelected
                                         ? $"Selected {selectedChildren[0].SymbolChild.ReadableName}..."
                                         : $"Selected {selectedChildren.Count} items...";

                         ImGui.PushFont(Fonts.FontSmall);
                         ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                         ImGui.Text(label);
                         ImGui.PopStyleColor();
                         ImGui.PopFont();

                         if (ImGui.BeginMenu("Styles"))
                         {
                             if (ImGui.MenuItem("Default", "", selectedChildren.Any(child => child.Style == SymbolUi.Styles.Default)))
                             {
                                 foreach (var childUi in selectedChildren)
                                 {
                                     childUi.Style = SymbolUi.Styles.Default;
                                 }
                             }

                             if (ImGui.MenuItem("Resizable", "", selectedChildren.Any(child => child.Style == SymbolUi.Styles.Resizable)))
                             {
                                 foreach (var childUi in selectedChildren)
                                 {
                                     childUi.Style = SymbolUi.Styles.Resizable;
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
                             _combineName = selectedChildren[0].SymbolChild.Symbol.Name;
                             _nameSpace = selectedChildren[0].SymbolChild.Symbol.Namespace;
                             _duplicateSymbolDialog.ShowNextFrame();
                         }

                         if (ImGui.MenuItem("Combine as new type"))
                         {
                             _combineDialog.ShowNextFrame();
                         }

                         if (ImGui.MenuItem("Copy"))
                         {
                             CopySelectionToClipboard(selectedChildren);
                         }

                         ImGui.Separator();
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
            _selectedChildren = selectedChildren;
            foreach (var x in SelectionHandler.SelectedElements)
            {
                if (x is SymbolChildUi childUi)
                {
                    selectedChildren.Add(childUi);
                }
            }

            return selectedChildren;
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
                var json = new Json();
                json.Writer = new JsonTextWriter(writer);
                json.Writer.Formatting = Formatting.Indented;
                json.Writer.WriteStartArray();

                json.WriteSymbol(containerOp);

                var jsonUi = new UiJson();
                jsonUi.Writer = json.Writer;
                jsonUi.WriteSymbolUi(newContainerUi);

                json.Writer.WriteEndArray();

                try
                {
                    Clipboard.SetText(writer.ToString(), TextDataFormat.UnicodeText);
                    Log.Info(Clipboard.GetText(TextDataFormat.UnicodeText));
                }
                catch (Exception)
                {
                    Log.Error("Could not copy elements to clipboard. Perhaps a tool like Teamviewer locks it.");
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
                    var json = new Json();
                    json.Reader = new JsonTextReader(reader);
                    var o = JToken.ReadFrom(json.Reader) as JArray;
                    if (o == null)
                        return;

                    var symbolJson = o[0];
                    var containerSymbol = json.ReadSymbol(null, symbolJson);
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);
                    var uiJson = new UiJson();
                    uiJson.Reader = json.Reader;
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

        //private readonly Dictionary<Guid, CanvasProperties> _canvasPropertiesForCompositionOpIds = new Dictionary<Guid, CanvasProperties>();

        private readonly ModalDialog _combineDialog = new ModalDialog("Combine into symbol");
        private readonly ModalDialog _duplicateSymbolDialog = new ModalDialog("Duplicate as new symbol");
        public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private readonly SelectionFence _selectionFence;
        private List<SymbolChildUi> ChildUis { get; set; }
        private readonly SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private string _combineName = "";
        private string _nameSpace = "";
        private GraphWindow _window;
    }
}