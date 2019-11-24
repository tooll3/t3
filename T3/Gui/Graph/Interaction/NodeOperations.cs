using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace T3.Gui.Graph.Interaction
{
    internal static class NodeOperations
    {
        public static void CombineAsNewType(SymbolUi compositionSymbolUi, List<SymbolChildUi> selectedChildren, string newSymbolName, string nameSpace)
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
            classStringBuilder.Append(outputStringBuilder);
            classStringBuilder.AppendLine("");
            classStringBuilder.Append(inputStringBuilder);
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

            AddSourceFileToProject(newSourcePath);

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
            newSymbol.Namespace = nameSpace;

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
            var addCommand = new AddSymbolChildCommand(compositionSymbolUi.Symbol, newSymbol.Id) { PosOnCanvas = mousePos };
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

        public static Symbol DuplicateAsNewType(SymbolUi compositionUi, SymbolChild symbolChildToDuplicate, string combineName, string nameSpace)
        {
            var sourceSymbol = symbolChildToDuplicate.Symbol;
            string originalSourcePath = sourceSymbol.SourcePath;
            Log.Info($"original symbol path: {originalSourcePath}");
            int lastSeparatorIndex = originalSourcePath.LastIndexOf("\\", StringComparison.Ordinal);
            string newSourcePath = originalSourcePath.Substring(0, lastSeparatorIndex + 1) + combineName + ".cs";
            Log.Info($"new symbol path: {newSourcePath}");
            AddSourceFileToProject(newSourcePath);

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
            newSource = Regex.Replace(newSource, sourceSymbol.Name, match => combineName);
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
            newSymbol.Namespace = nameSpace;

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
            var addCommand = new AddSymbolChildCommand(compositionUi.Symbol, newSymbol.Id) { PosOnCanvas = mousePos };
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

        public static SymbolChildUi CreateInstance(Symbol symbol, Symbol parent, Vector2 positionOnCanvas)
        {
            
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = positionOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var childUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);
            return childUi;
            
        }

        /// <summary>
        /// Inserts an entry like...
        ///
        ///      <Compile Include="Types\GfxPipelineExample.cs" />
        ///
        /// ... ot the project file.
        /// </summary>
        private static void AddSourceFileToProject(string newSourceFilePath)
        {
            var path = Path.GetDirectoryName(newSourceFilePath);
            var newFileName = Path.GetFileName(newSourceFilePath);
            var directoryInfo = new DirectoryInfo(path).Parent;
            if (directoryInfo == null)
            {
                Log.Error("Can't find project file folder for " + newSourceFilePath);
                return;
            }

            var parentPath = directoryInfo.FullName;
            var projectFilePath = Path.Combine(parentPath, "Operators.csproj");

            if (!File.Exists(projectFilePath))
            {
                Log.Error("Can't find project file in " + projectFilePath);
                return;
            }

            var orgLine = "<ItemGroup>\r\n    <Compile Include";
            var newLine = $"<ItemGroup>\r\n    <Compile Include=\"Types\\{newFileName}\" />\n\r<Compile Include ";
            var newContent = File.ReadAllText(projectFilePath).Replace(orgLine, newLine);
            File.WriteAllText(projectFilePath, newContent);
        }

        public static bool IsNewSymbolNameValid(string newSymbolName)
        {
            return !string.IsNullOrEmpty(newSymbolName)
                   && ValidTypeNamePattern.IsMatch(newSymbolName)
                   && !SymbolRegistry.Entries.Values.Any(value => string.Equals(value.Name, newSymbolName, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly Regex ValidTypeNamePattern = new Regex("^[A-Za-z_]+[A-Za-z0-9_]*$");
    }
}