using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Selection;
using T3.Gui.Windows;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas
    {
        public GraphCanvas(Instance opInstance)
        {
            CompositionOp = opInstance;
            _selectionFence = new SelectionFence(this);
        }

        #region drawing UI ====================================================================
        public void Draw()
        {
            UpdateCanvas();

            Current = this;
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            DrawList = ImGui.GetWindowDrawList();

            ImGui.BeginGroup();
            {
                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                DrawGrid();
                _symbolBrowser.Draw();

                Graph.DrawGraph();

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

                if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, InverseTransformPosition(ImGui.GetMousePos()));
                }
            }
            ImGui.EndGroup();
        }

        public List<Instance> GetParents(bool includeCompositionOp = false)
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



        private bool _contextMenuIsOpen = false;
        private void DrawContextMenu()
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                ImGui.GetMousePosOnOpeningCurrentPopup();
                _contextMenuIsOpen = true;

                // Todo: Convert to linc
                var selectedChildren = new List<SymbolChildUi>();
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
                    var label = oneElementSelected ? $"{selectedChildren[0].SymbolChild.ReadableName} Item..." : $"{selectedChildren.Count} Items...";

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
                        var newSymbol = DuplicateAsNewType(selectedChildren[0].SymbolChild.Symbol);
                        if (newSymbol != null)
                        {
                            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                            var mousePos = InverseTransformPosition(ImGui.GetMousePos());
                            var addCommand = new AddSymbolChildCommand(compositionSymbolUi.Symbol, newSymbol.Id) { PosOnCanvas = mousePos };
                            UndoRedoStack.AddAndExecute(addCommand);
                        }
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
                    QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), CompositionOp.Symbol, InverseTransformPosition(ImGui.GetMousePos()));
                }
                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }
            ImGui.PopStyleVar();
        }

        private static Symbol DuplicateAsNewType(Symbol symbolToDuplicate)
        {
            var sourceSymbol = symbolToDuplicate;
            string originalSourcePath = sourceSymbol.SourcePath;
            Log.Info($"original symbol path: {originalSourcePath}");
            string newName = sourceSymbol.Name + "2";
            int lastSeparatorIndex = originalSourcePath.LastIndexOf("\\");
            string newSourcePath = originalSourcePath.Substring(0, lastSeparatorIndex + 1) + newName + ".cs";
            Log.Info($"new symbol path: {newSourcePath}");

            var sr = new StreamReader(originalSourcePath);
            string originalSource = sr.ReadToEnd();
            sr.Dispose();
            var oldToNewIdMap = new Dictionary<Guid, Guid>(20);
            string newSource = Regex.Replace(originalSource,
                                             @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}",
                                             match =>
                                             {
                                                 Guid newGuid = Guid.NewGuid();
                                                 oldToNewIdMap.Add(Guid.Parse(match.Value), newGuid);
                                                 return newGuid.ToString();
                                             },
                                             RegexOptions.IgnoreCase);
            newSource = Regex.Replace(newSource, sourceSymbol.Name, match => newName);
            var sw = new StreamWriter(newSourcePath);
            sw.Write(newSource);
            sw.Dispose();

            var resourceManager = ResourceManager.Instance();
            Guid newSymbolId = Guid.NewGuid();
            uint symbolResourceId = resourceManager.CreateOperatorEntry(newSourcePath, newSymbolId.ToString());
            var symbolResource = resourceManager.GetResource<OperatorResource>(symbolResourceId);
            symbolResource.Update(newSourcePath);
            if (!symbolResource.Updated)
            {
                Log.Error("Error, new symbol was not updated/compiled");
                return null;
            }

            Type type = symbolResource.OperatorAssembly.ExportedTypes.FirstOrDefault();
            if (type == null)
            {
                Log.Error("Error, new symbol has no compiled instance type");
                return null;
            }

            // create and register the new symbol
            var newSymbol = new Symbol(type, newSymbolId);
            SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
            var newSymbolUi = UiModel.UpdateUiEntriesForSymbol(newSymbol);

            // apply content to new symbol
            var sourceSymbolUi = SymbolUiRegistry.Entries[sourceSymbol.Id];
            var cmd = new CopySymbolChildrenCommand(sourceSymbolUi, null, newSymbolUi, Vector2.One);
            cmd.Do();
            cmd.OldToNewIdDict.ToList().ForEach(x => oldToNewIdMap.Add(x.Key, x.Value));
            
            // now copy connection from/to inputs/outputs that are not copied with the command 
            // todo: check if this can be put into the command
            var connectionsToCopy = sourceSymbol.Connections.FindAll(c => c.IsConnectedToSymbolInput || c.IsConnectedToSymbolOutput);
            foreach (var conToCopy in connectionsToCopy)
            {
                bool inputConnection = conToCopy.IsConnectedToSymbolInput;
                var newSourceSlotId = inputConnection ? oldToNewIdMap[conToCopy.SourceSlotId] : conToCopy.SourceSlotId;
                var newSourceId = inputConnection ? conToCopy.SourceParentOrChildId : oldToNewIdMap[conToCopy.SourceParentOrChildId];

                bool outputConnection = conToCopy.IsConnectedToSymbolOutput;
                var newTargetSlotId = outputConnection ? oldToNewIdMap[conToCopy.TargetSlotId] : conToCopy.TargetSlotId;
                var newTargetId = outputConnection ? conToCopy.TargetParentOrChildId : oldToNewIdMap[conToCopy.TargetParentOrChildId];

                var newConnection = new Symbol.Connection(newSourceId, newSourceSlotId, newTargetId, newTargetSlotId);
                newSymbol.AddConnection(newConnection);
            }

            return newSymbol;
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
                // MetaManager.WriteOpWithWriter(containerOp, writer);
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
            for (float x = Scroll.X % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }

            for (float y = Scroll.Y % gridSize; y < WindowSize.Y; y += gridSize)
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
        public void DrawRect(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.Current.DrawList.AddRect(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }

        public void DrawRectFilled(ImRect rectOnCanvas, Color color)
        {
            GraphCanvas.Current.DrawList.AddRectFilled(TransformPosition(rectOnCanvas.Min), TransformPosition(rectOnCanvas.Max), color);
        }

        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public ImDrawListPtr DrawList { get; private set; }
        public Instance CompositionOp { get; set; }
        #endregion

        public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private SelectionFence _selectionFence;
        internal static Vector2 DefaultOpSize = new Vector2(100, 30);
        internal List<SymbolChildUi> ChildUis { get; set; }
        private SymbolBrowser _symbolBrowser = new SymbolBrowser();
    }
}
