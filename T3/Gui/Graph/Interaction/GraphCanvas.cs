using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
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
                        CombineAsNewType(compositionSymbolUi, _selectedChildren, _combineName);
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }

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
        private List<SymbolChildUi> _selectedChildren;
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
                        var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                        DuplicateAsNewType(compositionSymbolUi, selectedChildren[0].SymbolChild);
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

        private static void CombineAsNewType(SymbolUi compositionSymbolUi, List<SymbolChildUi> selectedChildren, string newSymbolName)
        {
            Dictionary<Guid, Guid> oldToNewIdMap = new Dictionary<Guid, Guid>();

            // get all the connections that go into the selection (selected ops as target)
            var compositionSymbol = compositionSymbolUi.Symbol;
            var potentialTargetIds = from child in selectedChildren select child.Id;
            var inputConnections = (from con in compositionSymbol.Connections
                                    from id in potentialTargetIds
                                    where con.TargetParentOrChildId == id
                                    where potentialTargetIds.All(potId => potId != con.SourceParentOrChildId)
                                    select con).ToArray();
            var inputsToGenerate = (from con in inputConnections
                                    from child in compositionSymbol.Children
                                    where child.Id == con.TargetParentOrChildId
                                    from input in child.Symbol.InputDefinitions
                                    where input.Id == con.TargetSlotId
                                    select (child, input)).ToList().Distinct();
            var usingStringBuilder = new StringBuilder();
            var inputStringBuilder = new StringBuilder();
            var outputStringBuilder = new StringBuilder();
            var connectionsFromNewInputs = new List<Symbol.Connection>(inputConnections.Length);
            int inputNameCounter = 2;
            var inputNameHashSet = new HashSet<string>();
            foreach (var (child, input) in inputsToGenerate)
            {
                var inputValueType = input.DefaultValue.ValueType;
                if (TypeNameRegistry.Entries.TryGetValue(inputValueType, out var typeName))
                {
                    var @namespace = input.DefaultValue.ValueType.Namespace;
                    usingStringBuilder.AppendLine("using " + @namespace + ";");
                    Guid newInputGuid = Guid.NewGuid();
                    oldToNewIdMap.Add(input.Id, newInputGuid);
                    var attributeString = "        [Input(Guid = \"" + newInputGuid + "\")]";
                    inputStringBuilder.AppendLine(attributeString);
                    var newInputName = inputNameHashSet.Contains(input.Name) ? (input.Name + inputNameCounter++) : input.Name;
                    inputNameHashSet.Add(newInputName);
                    var slotString = (input.IsMultiInput ? "MultiInputSlot<" : "InputSlot<") + typeName + ">";
                    var inputString = "        public readonly " + slotString + " " + newInputName + " = new " + slotString + "();";
                    inputStringBuilder.AppendLine(inputString);
                    inputStringBuilder.AppendLine("");

                    var newConnection = new Symbol.Connection(Guid.Empty, newInputGuid, child.Id, input.Id);
                    connectionsFromNewInputs.Add(newConnection);
                }
                else
                {
                    Log.Error($"Error, no registered name found for typename: {input.DefaultValue.ValueType.Name}");
                }
            }

            var outputConnections = (from con in compositionSymbol.Connections
                                     from id in potentialTargetIds
                                     where con.SourceParentOrChildId == id
                                     where potentialTargetIds.All(potId => potId != con.TargetParentOrChildId)
                                     select con).ToArray();
            var outputsToGenerate = (from con in outputConnections
                                     from child in compositionSymbol.Children
                                     where child.Id == con.SourceParentOrChildId
                                     from output in child.Symbol.OutputDefinitions
                                     where output.Id == con.SourceSlotId
                                     select (child, output)).ToList().Distinct();
            var connectionsToNewOutputs = new List<Symbol.Connection>(outputConnections.Length);
            int outputNameCounter = 2;
            var outputNameHashSet = new HashSet<string>();
            foreach (var (child, output) in outputsToGenerate)
            {
                var outputValueType = output.ValueType;
                if (TypeNameRegistry.Entries.TryGetValue(outputValueType, out var typeName))
                {
                    var @namespace = outputValueType.Namespace;
                    usingStringBuilder.AppendLine("using " + @namespace + ";");
                    Guid newOutputGuid = Guid.NewGuid();
                    oldToNewIdMap.Add(output.Id, newOutputGuid);
                    var attributeString = "        [Output(Guid = \"" + newOutputGuid + "\")]";
                    outputStringBuilder.AppendLine(attributeString);
                    var newOutputName = outputNameHashSet.Contains(output.Name) ? (output.Name + outputNameCounter++) : output.Name;
                    outputNameHashSet.Add(newOutputName);
                    var slotString = "Slot<" + typeName + ">";
                    var outputString = "        public readonly " + slotString + " " + newOutputName + " = new " + slotString + "();";
                    outputStringBuilder.AppendLine(outputString);
                    outputStringBuilder.AppendLine("");

                    var newConnection = new Symbol.Connection(child.Id, output.Id, Guid.Empty, newOutputGuid);
                    connectionsToNewOutputs.Add(newConnection);
                }
                else
                {
                    Log.Error($"Error, no registered name found for typename: {output.ValueType.Name}");
                }
            }

            usingStringBuilder.AppendLine("using T3.Core.Operator;");

            var classStringBuilder = new StringBuilder(usingStringBuilder.ToString());
            classStringBuilder.AppendLine("");
            classStringBuilder.AppendLine("namespace T3.Operators.Types");
            classStringBuilder.AppendLine("{");
            classStringBuilder.AppendFormat("    public class {0} : Instance<{0}>\n", newSymbolName);
            classStringBuilder.AppendLine("    {");
            classStringBuilder.Append(outputStringBuilder.ToString());
            classStringBuilder.AppendLine("");
            classStringBuilder.Append(inputStringBuilder.ToString());
            classStringBuilder.AppendLine("    }");
            classStringBuilder.AppendLine("}");
            classStringBuilder.AppendLine("");
            var newSource = classStringBuilder.ToString();
            Log.Info(newSource);

            var newSourcePath = @"Operators\Types\" + newSymbolName + ".cs";

            // todo: below same code as in duplicate new type 
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
            }

            Type type = symbolResource.OperatorAssembly.ExportedTypes.FirstOrDefault();
            if (type == null)
            {
                Log.Error("Error, new symbol has no compiled instance type");
            }

            // create and register the new symbol
            var newSymbol = new Symbol(type, newSymbolId);
            SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
            var newSymbolUi = UiModel.UpdateUiEntriesForSymbol(newSymbol);
            newSymbol.SourcePath = newSourcePath;

            // apply content to new symbol
            var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, selectedChildren, newSymbolUi, Vector2.Zero);
            cmd.Do();
            cmd.OldToNewIdDict.ToList().ForEach(x => oldToNewIdMap.Add(x.Key, x.Value));

            foreach (var con in connectionsFromNewInputs)
            {
                var sourceId = con.SourceParentOrChildId;
                var sourceSlotId = con.SourceSlotId;
                var targetId = oldToNewIdMap[con.TargetParentOrChildId];
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                newSymbol.AddConnection(newConnection);
            }

            foreach (var con in connectionsToNewOutputs)
            {
                var sourceId = oldToNewIdMap[con.SourceParentOrChildId];
                var sourceSlotId = con.SourceSlotId;
                var targetId = con.TargetParentOrChildId;
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                newSymbol.AddConnection(newConnection);
            }

            var mousePos = GraphCanvas.Current.InverseTransformPosition(ImGui.GetMousePos());
            var addCommand = new AddSymbolChildCommand(compositionSymbolUi.Symbol, newSymbol.Id) {PosOnCanvas = mousePos};
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChildId = addCommand.AddedChildId;

            foreach (var con in inputConnections.Reverse()) // reverse for multi input order preservation
            {
                var sourceId = con.SourceParentOrChildId;
                var sourceSlotId = con.SourceSlotId;
                var targetId = newSymbolChildId;
                var targetSlotId = oldToNewIdMap[con.TargetSlotId];

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                compositionSymbol.AddConnection(newConnection);
            }

            foreach (var con in outputConnections.Reverse()) // reverse for multi input order preservation
            {
                var sourceId = newSymbolChildId;
                var sourceSlotId = oldToNewIdMap[con.SourceSlotId];
                var targetId = con.TargetParentOrChildId;
                var targetSlotId = con.TargetSlotId;

                var newConnection = new Symbol.Connection(sourceId, sourceSlotId, targetId, targetSlotId);
                compositionSymbol.AddConnection(newConnection);
            }

            var deleteCmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
            UndoRedoStack.AddAndExecute(deleteCmd);
        }

        private static Symbol DuplicateAsNewType(SymbolUi compositionUi, SymbolChild symbolChildToDuplicate)
        {
            var sourceSymbol = symbolChildToDuplicate.Symbol;
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
            newSymbol.SourcePath = newSourcePath;

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

            var mousePos = GraphCanvas.Current.InverseTransformPosition(ImGui.GetMousePos());
            var addCommand = new AddSymbolChildCommand(compositionUi.Symbol, newSymbol.Id) {PosOnCanvas = mousePos};
            UndoRedoStack.AddAndExecute(addCommand);

            // copy the values of the input of the duplicated type: default values of symbol and the ones in composition context
            var newSymbolInputs = newSymbol.InputDefinitions;
            for (int i = 0; i < sourceSymbol.InputDefinitions.Count; i++)
            {
                newSymbolInputs[i].DefaultValue = sourceSymbol.InputDefinitions[i].DefaultValue.Clone();
            }

            var newSymbolChild = compositionUi.Symbol.Children.Find(child => child.Id == addCommand.AddedChildId);
            var newSymbolInputValues = newSymbolChild.InputValues;

            foreach (var input in symbolChildToDuplicate.InputValues)
            {
                var newInput = newSymbolInputValues[oldToNewIdMap[input.Key]];
                newInput.Value.Assign(input.Value.Value.Clone());
                newInput.IsDefault = input.Value.IsDefault;
            }

            // update the positions
            var sourceSelectables = sourceSymbolUi.GetSelectables().ToArray();
            var newSelectables = newSymbolUi.GetSelectables().ToArray();
            Debug.Assert(sourceSelectables.Length == newSelectables.Length);
            for (int i = 0; i < sourceSelectables.Length; i++)
            {
                newSelectables[i].PosOnCanvas = sourceSelectables[i].PosOnCanvas; // todo: check if this is enough or if id check needed
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
        public Instance CompositionOp { get; private set; }
        #endregion
        
        
        private class CanvasProperties
        {
            public Vector2 Scale;
            public Vector2 Scroll;
        }

        private readonly Dictionary<Guid, CanvasProperties> _canvasPropertiesForCompositionOpIds = new Dictionary<Guid, CanvasProperties>();

        public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private SelectionFence _selectionFence;
        internal static Vector2 DefaultOpSize = new Vector2(110, 25);
        internal List<SymbolChildUi> ChildUis { get; set; }
        private SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private bool _showCombine;
        private string _combineName = "";
    }
}
