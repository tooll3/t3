using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas
    {
        public GraphCanvas(Instance opInstance)
        {
            _selectionFence = new SelectionFence(this);
            OpenComposition(opInstance);
        }

        public void OpenComposition(Instance opInstance, bool zoomIn = true)
        {
            // save old properties
            if (CompositionOp != null)
            {
                _canvasPropertiesForCompositionOpIds[CompositionOp.Id] = new CanvasProperties()
                                                                         {
                                                                             Scale = Scale,
                                                                             Scroll = Scroll,
                                                                         };
            }

            CompositionOp = opInstance;
            var id = opInstance.Id;
            var scale = Vector2.One;
            var scroll = Vector2.Zero;

            if (_canvasPropertiesForCompositionOpIds.ContainsKey(id))
            {
                var props = _canvasPropertiesForCompositionOpIds[opInstance.Id];
                scale = props.Scale;
                scroll = props.Scroll;
            }

            SetAreaWithTransition(scale, scroll, zoomIn);
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

                if (_showCombine)
                {
                    _showCombine = false;
                    ImGui.OpenPopup("Combine");
                }

                ImGui.SetNextWindowSize(new Vector2(500, 100), ImGuiCond.FirstUseEver);
                if (ImGui.BeginPopupModal("Combine"))
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Select name for new symbol");
                    ImGui.SameLine();
                    ImGui.InputText("##Select name for new symbol", ref _combineName, 255);

                    if (ImGui.Button("Combine"))
                    {
                        var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                        NodeOperations.CombineAsNewType(compositionSymbolUi, _selectedChildren, _combineName);
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
            }
            ImGui.EndGroup();
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

        private void DrawContextMenu()
        {
            CustomComponents.DrawContextMenuForScrollCanvas
                (
                 () =>
                 {
                     // Todo: Convert to Lync
                     var selectedChildren = new List<SymbolChildUi>();
                     _selectedChildren = selectedChildren;
                     foreach (var x in SelectionHandler.SelectedElements)
                     {
                         if (x is SymbolChildUi childUi)
                         {
                             selectedChildren.Add(childUi);
                         }
                     }

                     if (selectedChildren.Count > 0)
                     {
                         bool oneElementSelected = selectedChildren.Count == 1;
                         var label = oneElementSelected
                                         ? $"{selectedChildren[0].SymbolChild.ReadableName} Item..."
                                         : $"{selectedChildren.Count} Items...";

                         ImGui.Text(label);
                         if (ImGui.MenuItem(" Rename..", false))
                         {
                         }

                         if (ImGui.MenuItem(" Delete"))
                         {
                             var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                             var cmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
                             UndoRedoStack.AddAndExecute(cmd);
                         }

                         if (ImGui.MenuItem(" Duplicate as new type", oneElementSelected))
                         {
                             var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                             NodeOperations.DuplicateAsNewType(compositionSymbolUi, selectedChildren[0].SymbolChild);
                         }

                         if (ImGui.MenuItem(" Combine as new type"))
                         {
                             _showCombine = true;
                         }

                         if (ImGui.MenuItem(" Copy"))
                         {
                             CopySelectionToClipboard(selectedChildren);
                         }
                     }

                     if (ImGui.MenuItem("Paste"))
                     {
                         PasteClipboard();
                     }

                     ImGui.Separator();

                     if (ImGui.MenuItem("Rename..", false))
                     {
                     }

                     if (ImGui.MenuItem("Add"))
                     {
                         _symbolBrowser.OpenAt(InverseTransformPosition(ImGui.GetMousePos()), null, null);
                     }
                 }, ref _contextMenuIsOpen);
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
            var gridSize = 64.0f * Scale.X;
            for (var x = Scroll.X % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }

            for (var y = Scroll.Y % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos,
                                 new Color(0.5f, 0.5f, 0.5f, 0.1f));
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

        private class CanvasProperties
        {
            public Vector2 Scale;
            public Vector2 Scroll;
        }

        private readonly Dictionary<Guid, CanvasProperties> _canvasPropertiesForCompositionOpIds = new Dictionary<Guid, CanvasProperties>();

        public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private readonly SelectionFence _selectionFence;
        internal static Vector2 DefaultOpSize = new Vector2(110, 25);
        private List<SymbolChildUi> ChildUis { get; set; }
        private readonly SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private bool _showCombine;
        private string _combineName = "";
    }
}