using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Interaction.Connections
{
    /// <summary>
    /// Handles the creation of new  <see cref="Symbol.Connection"/>s. 
    /// It provides accessors for highlighting matching input slots and methods that need to be
    /// called when connections are completed or aborted.
    /// </summary>
    internal static class ConnectionMaker
    {
        public static readonly List<TempConnection> TempConnections = new();

        //private static readonly List<DeleteConnectionCommand> _tempDeletionCommands = new();
        private static MacroCommand _inProgressCommand;

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnections.Count > 0
                   && TempConnections[0].TargetSlotId == NotConnectedId
                   && TempConnections[0].ConnectionType == valueType;
        }

        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceSlotId == NotConnectedId
                   && TempConnections[0].ConnectionType == valueType;
        }

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceParentOrChildId == sourceUi.SymbolChild.Id
                   && TempConnections[0].SourceSlotId == outputDef.Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].TargetParentOrChildId == targetUi.SymbolChild.Id
                   && TempConnections[0].TargetSlotId == inputDef.Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceParentOrChildId == UseSymbolContainerId
                   && TempConnections[0].SourceSlotId == inputDef.Id;
        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].TargetParentOrChildId == UseSymbolContainerId
                   && TempConnections[0].TargetSlotId == outputDef.Id;
        }

        public static void StartFromOutputSlot(NodeSelection selection, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            StartOperation($"Connect from {sourceUi.SymbolChild.ReadableName}.{outputDef.Name}");

            var selectedSymbolChildUis = selection.GetSelectedChildUis().OrderBy(c => c.PosOnCanvas.Y * 100 + c.PosOnCanvas.X).ToList();
            selectedSymbolChildUis.Reverse();

            if (selectedSymbolChildUis.Count > 1 && (selectedSymbolChildUis.Any(c => c.Id == sourceUi.Id)))
            //if (selectedSymbolChildUis.Count > 1)
            {
                selectedSymbolChildUis.Reverse();
                
                // add temp connections for all selected nodes that have the same primary output type
                foreach (var selectedChild in selectedSymbolChildUis)
                {
                    var outputDefinitions = selectedChild.SymbolChild.Symbol.OutputDefinitions;
                    if (outputDefinitions.Count == 0)
                        continue;

                    var firstOutput = selectedChild == sourceUi ? outputDef 
                                          :    outputDefinitions[0];
                    if(firstOutput.ValueType != outputDef.ValueType)
                        continue;

                    TempConnections.Add(new TempConnection(sourceParentOrChildId: selectedChild.SymbolChild.Id,
                                                           sourceSlotId: firstOutput.Id,
                                                           targetParentOrChildId: NotConnectedId,
                                                           targetSlotId: NotConnectedId,
                                                           outputDef.ValueType));
                }
            }

            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                     sourceSlotId: outputDef.Id,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     outputDef.ValueType));
            }
        }

        private static bool _isDisconnectingFromInput;

        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputDef, multiInputIndex);
            if (existingConnection != null)
            {
                StartOperation($"Disconnect {targetUi.SymbolChild.ReadableName}.{inputDef.Name}");
                _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(parentSymbol, existingConnection, multiInputIndex));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     inputDef.DefaultValue.ValueType));
                _isDisconnectingFromInput = true;
            }
            else
            {
                StartOperation($"Connecting from {targetUi.SymbolChild.ReadableName}.{inputDef.Name}");
                SetTempConnection(new TempConnection(sourceParentOrChildId: NotConnectedId,
                                                     sourceSlotId: NotConnectedId,
                                                     targetParentOrChildId: targetUi.SymbolChild.Id,
                                                     targetSlotId: inputDef.Id,
                                                     inputDef.DefaultValue.ValueType));
                _isDisconnectingFromInput = false;
            }
        }

        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            StartOperation($"Connecting from {inputDef.Name}");
            SetTempConnection(new TempConnection(sourceParentOrChildId: UseSymbolContainerId,
                                                 sourceSlotId: inputDef.Id,
                                                 targetParentOrChildId: NotConnectedId,
                                                 targetSlotId: NotConnectedId,
                                                 inputDef.DefaultValue.ValueType));
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            
            StartOperation($"Connecting to {outputDef.Name}");
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainerId
                                                                        && c.TargetSlotId == outputDef.Id);
            if (existingConnection != null)
            {
                _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(parentSymbol, existingConnection, 0));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     outputDef.ValueType));
                _isDisconnectingFromInput = true;
            }
            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: NotConnectedId,
                                                     sourceSlotId: NotConnectedId,
                                                     targetParentOrChildId: UseSymbolContainerId,
                                                     targetSlotId: outputDef.Id,
                                                     outputDef.ValueType));
                _isDisconnectingFromInput = false;
            }
        }
        

        public static void StartOperation(string commandName)
        {
            if (TempConnections.Count != 0)
            {
                Log.Warning($"Inconsistent TempConnection count of {TempConnections.Count}. Last operation incomplete?");
            }

            // This can happen when recompiling operators
            if (_inProgressCommand != null)
            {
                //Log.Warning($"Incomplete command {_inProgressCommand.Name}");
                _inProgressCommand = null;
            }

            _inProgressCommand = new MacroCommand(commandName);
        }
        

        
        public static void CompleteOperation(List<ICommand> doneCommands = null, string newCommandTitle = null)
        {
            if (_inProgressCommand == null)
            {
                //Log.Debug("Setup temp op");
                StartOperation("Temp op");
            }
            
            if (doneCommands != null)
            {
                foreach (var c in doneCommands)
                {
                    _inProgressCommand.AddExecutedCommandForUndo(c);
                }
            }

            if (!string.IsNullOrEmpty(newCommandTitle))
            {
                _inProgressCommand.Name = newCommandTitle;
            }
            
            UndoRedoStack.Add(_inProgressCommand);
            Reset();
        }

        public static void AbortOperation()
        {
            _inProgressCommand?.Undo();
            Reset();
        }

        
        private static void Reset()
        {
            _isDisconnectingFromInput = false;
            _inProgressCommand = null;
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        private static void AdjustGraphLayoutForNewNode(Symbol parent, Symbol.Connection connection, NodeSelection selection)
        {
            if (connection.IsConnectedToSymbolOutput || connection.IsConnectedToSymbolInput)
            {
                Log.Debug("re-layout is not not supported for input and output nodes yet");
                return;
            }

            var parentSymbolUi = parent.GetSymbolUi();
            var sourceNodeUi = parentSymbolUi.ChildUis[connection.SourceParentOrChildId];
            var targetNodeUi = parentSymbolUi.ChildUis[connection.TargetParentOrChildId];
            var center = (sourceNodeUi.PosOnCanvas + targetNodeUi.PosOnCanvas) / 2;
            var commands = new List<ICommand>();

            var changedChildren = new List<ISelectableCanvasObject>();
            var changedChildUis = new List<SymbolChildUi>();

            var requiredGap = SymbolChildUi.DefaultOpSize.X + SelectableNodeMovement.SnapPadding.X;
            var xSource = sourceNodeUi.PosOnCanvas.X + sourceNodeUi.Size.X;
            var xTarget = targetNodeUi.PosOnCanvas.X;

            var currentGap = xTarget - xSource - SelectableNodeMovement.SnapPadding.X;
            if (currentGap > requiredGap)
                return;

            var offset = Math.Min(requiredGap - currentGap, requiredGap);

            // Collect all connected ops further down the tree
            var connectedOps = new HashSet<SymbolChild>();
            RecursivelyAddChildren(ref connectedOps, parent, targetNodeUi.SymbolChild);

            foreach (var child in connectedOps)
            {
                if (!parentSymbolUi.ChildUis.TryGetValue(child.Id, out var childUi) || childUi.PosOnCanvas.X > center.X)
                    continue;

                changedChildren.Add(childUi);
                changedChildUis.Add(childUi);
            }

            var command = new ModifyCanvasElementsCommand(parentSymbolUi.Symbol.Id, changedChildren, selection);
            foreach (var childUi in changedChildUis)
            {
                var pos = childUi.PosOnCanvas;

                pos.X -= offset;
                childUi.PosOnCanvas = pos;

            }
            command.StoreCurrentValues();

            _inProgressCommand.AddExecutedCommandForUndo(command);
            //commands.Add(new ModifyCanvasElementsCommand(parentSymbolUi.Symbol.Id, changedSymbols));
            //return new MacroCommand("adjust layout", commands);
        }

        private static void RecursivelyAddChildren(ref HashSet<SymbolChild> set, Symbol parent, SymbolChild targetNode)
        {
            foreach (var inputDef in targetNode.Symbol.InputDefinitions)
            {
                var connections = parent.Connections.FindAll(c => c.TargetSlotId == inputDef.Id
                                                                  && c.TargetParentOrChildId == targetNode.Id);

                foreach (var c in connections)
                {
                    if (c.SourceParentOrChildId == UseSymbolContainerId) // TODO move symbol inputs?
                        continue;

                    if (!parent.Children.TryGetValue(c.SourceParentOrChildId, out var sourceOp))
                    {
                        Log.Error($"Can't find child for connection source {c.SourceParentOrChildId}");
                        continue;
                    }

                    if (!set.Add(sourceOp))
                        continue;

                    RecursivelyAddChildren(ref set, parent, sourceOp);
                }
            }
        }

        public static void CompleteAtInputSlot(Instance childInstance, SymbolChildUi targetUi, Symbol.InputDefinition input, int multiInputIndex = 0,
                                               bool insertMultiInput = false)
        {
            var symbolInstance = childInstance.Parent;
            var sourceInstance = symbolInstance.Children[TempConnections[0].SourceParentOrChildId];
            
            // Check for cycles
            var outputSlot = sourceInstance.Outputs[0];
            var deps = new HashSet<ISlot>();
            Structure.CollectSlotDependencies(outputSlot, deps);

            foreach (var d in deps)
            {
                if (d.Parent.SymbolChildId != targetUi.Id)
                    continue;

                Log.Debug("Sorry, you can't do this. This connection would result in a cycle.");
                Log.Debug($"Dependency: [{d.Parent.Symbol.Name}], target: [{targetUi.SymbolChild.Symbol.Name}]");
                //TempConnections.Clear();
                //ConnectionSnapEndHelper.ResetSnapping();
                AbortOperation();
                return;
            }

            var symbol = symbolInstance.Symbol;

            var newConnections = new List<Symbol.Connection>();

            for (var index = TempConnections.Count - 1; index >= 0; index--)
            {
                var tempConnection = TempConnections[index];
                newConnections.Add(new Symbol.Connection(sourceParentOrChildId: tempConnection.SourceParentOrChildId,
                                                         sourceSlotId: tempConnection.SourceSlotId,
                                                         targetParentOrChildId: targetUi.SymbolChild.Id,
                                                         targetSlotId: input.Id));
            }

            // get the previous connection
            var allConnectionsToSlot = symbol.Connections.FindAll(c => c.TargetParentOrChildId == targetUi.SymbolChild.Id &&
                                                                             c.TargetSlotId == input.Id);
            var replacesConnection = false;
            if (insertMultiInput)
            {
                replacesConnection = multiInputIndex % 2 != 0;
                multiInputIndex /= 2; // divide by 2 to get correct insertion index in existing connections
            }
            else
            {
                replacesConnection = allConnectionsToSlot.Count != 0;
            }

            var addCommands = new List<ICommand>();
            foreach (var newConnection in newConnections)
            {
                addCommands.Add(new AddConnectionCommand(symbol, newConnection, multiInputIndex));
            }

            if (replacesConnection)
            {
                var connectionToRemove = allConnectionsToSlot[multiInputIndex];
                var deleteCommand = new DeleteConnectionCommand(symbol, connectionToRemove, multiInputIndex);
                _inProgressCommand.AddAndExecCommand(deleteCommand);
                foreach (var addCommand in addCommands)
                {
                    _inProgressCommand.AddAndExecCommand(addCommand);
                }
                _inProgressCommand.Name = $"Reconnect to {targetUi.SymbolChild.ReadableName}.{input.Name}";
            }
            else
            {
                foreach (var addCommand in addCommands)
                {
                    _inProgressCommand.AddAndExecCommand(addCommand);
                }
            }
            CompleteOperation();
        }

        public static void CompleteAtOutputSlot(Instance sourceInstance, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            // Check for cycles
            var deps = new HashSet<ISlot>();
            foreach (var inputSlot in sourceInstance.Inputs)
            {
                Structure.CollectSlotDependencies(inputSlot, deps);
            }

            foreach (var d in deps)
            {
                if (d.Parent.SymbolChildId != TempConnections[0].TargetParentOrChildId)
                    continue;

                Log.Debug("Sorry, you can't do this. This connection would result in a cycle.");
                TempConnections.Clear();
                ConnectionSnapEndHelper.ResetSnapping();
                return;
            }

            var newConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                      sourceSlotId: output.Id,
                                                      targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                      targetSlotId: TempConnections[0].TargetSlotId);
            
            _inProgressCommand.AddAndExecCommand(new AddConnectionCommand(sourceInstance.Parent.Symbol, newConnection, 0));
            CompleteOperation();
        }

        #region related to SymbolBrowser
        
        /// <remarks>
        /// Assumes that a temp connection has be created earlier and is now dropped on the background
        /// </remarks>
        public static void InitSymbolBrowserAtPosition(GraphWindow window, Vector2 canvasPosition)
        {
            if (TempConnections.Count == 0)
                return;

            if (_isDisconnectingFromInput)
            {
                CompleteOperation();
                return;
            }
            
            var symbolBrowser = window.SymbolBrowser;

            var firstConnectionType = TempConnections[0].ConnectionType;
            if (TempConnections.Count == 1)
            {
                if (TempConnections[0].TargetParentOrChildId == NotConnectedId)
                {
                    SetTempConnection(new TempConnection(sourceParentOrChildId: TempConnections[0].SourceParentOrChildId,
                                                         sourceSlotId: TempConnections[0].SourceSlotId,
                                                         targetParentOrChildId: UseDraftChildId,
                                                         targetSlotId: NotConnectedId,
                                                         firstConnectionType));
                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null, false);
                }
                else if (TempConnections[0].SourceParentOrChildId == NotConnectedId)
                {
                    SetTempConnection(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                         sourceSlotId: NotConnectedId,
                                                         targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                         targetSlotId: TempConnections[0].TargetSlotId,
                                                         firstConnectionType));
                    symbolBrowser.OpenAt(canvasPosition, null, firstConnectionType, false);
                }
            }
            // Multiple TempConnections only work when they are connected to outputs 
            else if (TempConnections.Count > 1)
            {
                var validForMultiInput = TempConnections.All(c =>
                                                                 c.GetStatus() == TempConnection.Status.TargetIsUndefined
                                                                 && c.ConnectionType == firstConnectionType);
                if (validForMultiInput)
                {
                    var oldConnections = TempConnections.ToArray().Reverse();
                    TempConnections.Clear();
                    foreach (var c in oldConnections)
                    {
                        TempConnections.Add(new TempConnection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                               sourceSlotId: c.SourceSlotId,
                                                               targetParentOrChildId: UseDraftChildId,
                                                               targetSlotId: NotConnectedId,
                                                               firstConnectionType));
                    }

                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null, onlyMultiInputs: true);
                }
            }
            else
            {
                AbortOperation();
            }
        }
        

        public static void OpenSymbolBrowserAtOutput(GraphWindow window, SymbolChildUi childUi, Instance instance,
                                                     Guid outputId)
        {
            var primaryOutput = instance.Outputs.SingleOrDefault(o => o.Id == outputId);
            if (primaryOutput == null)
                return;
            
            StartOperation("Insert Operator");
            InsertSymbolBrowser(window, childUi, instance, primaryOutput);
        }

        public static void InsertSymbolInstance(GraphWindow window, Symbol symbol)
        {
            var selection = window.GraphCanvas.NodeSelection;
            var instance = selection.GetSelectedInstanceWithoutComposition();
            if (instance == null)
            {
                return;
            }


            var parentUi = instance.Parent.GetSymbolUi();

            if (!parentUi.ChildUis.TryGetValue(instance.SymbolChildId, out var symbolChildUi))
            {
                return;
            }
            
            
            if (instance.Outputs.Count < 1)
                return;
            
            var posOnCanvas = symbolChildUi.PosOnCanvas;
            
            var primaryOutput = instance.Outputs[0];
            StartOperation("Insert Operator");
            var connections = instance.Parent.Symbol.Connections.FindAll(connection => connection.SourceParentOrChildId == instance.SymbolChildId
                                                                                       && connection.SourceSlotId == primaryOutput.Id);

            TempConnections.Add(new TempConnection(sourceParentOrChildId: instance.SymbolChildId,
                                                   sourceSlotId: primaryOutput.Id,
                                                   targetParentOrChildId: UseDraftChildId,
                                                   targetSlotId: NotConnectedId,
                                                   primaryOutput.ValueType));

            if (connections.Count > 0)
            {
                AdjustGraphLayoutForNewNode(instance.Parent.Symbol, connections[0], window.GraphCanvas.NodeSelection);
                foreach (var oldConnection in connections)
                {
                    var multiInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(oldConnection);
                    _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(instance.Parent.Symbol, oldConnection, multiInputIndex));
                    TempConnections.Add(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                           sourceSlotId: NotConnectedId,
                                                           targetParentOrChildId: oldConnection.TargetParentOrChildId,
                                                           targetSlotId: oldConnection.TargetSlotId,
                                                           primaryOutput.ValueType,
                                                           multiInputIndex));
                }
            }
            else
            {
                posOnCanvas.X += SymbolChildUi.DefaultOpSize.X +  SelectableNodeMovement.SnapPadding.X;
            }
            
            var commandsForUndo = new List<ICommand>();
            var parent = instance.Parent;
            var parentSymbol = parent.Symbol;

            var addSymbolChildCommand = new AddSymbolChildCommand(instance.Parent.Symbol, symbol.Id) { PosOnCanvas = posOnCanvas };
            commandsForUndo.Add(addSymbolChildCommand);
            addSymbolChildCommand.Do();
            var newSymbolChild = parentSymbol.Children[addSymbolChildCommand.AddedChildId];

            // Select new node
            var parentSymbolUi = parentSymbol.GetSymbolUi();
            if (!parentUi.ChildUis.TryGetValue(newSymbolChild.Id, out var newChildUi))
            {
                Log.Warning("Unable to create new operator");
                return;
            }
            
            var newInstance = instance.Parent.Children[newChildUi.Id];

            selection.SetSelectionToChildUi(newChildUi, newInstance);
            
            foreach (var c in ConnectionMaker.TempConnections)
            {
                switch (c.GetStatus())
                {
                    case ConnectionMaker.TempConnection.Status.SourceIsDraftNode:
                        var outputDef = newSymbolChild.Symbol.GetOutputMatchingType(c.ConnectionType);
                        if (outputDef == null)
                        {
                            Log.Error("Failed to find matching output connection type " + c.ConnectionType);
                            continue;
                        }
                        

                        var newConnectionToSource = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                                          sourceSlotId: outputDef.Id,
                                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                                          targetSlotId: c.TargetSlotId);
                        var addConnectionCommand = new AddConnectionCommand(parentSymbolUi.Symbol, newConnectionToSource, c.MultiInputIndex);
                        addConnectionCommand.Do();
                        commandsForUndo.Add(addConnectionCommand);
                        break;

                    case ConnectionMaker.TempConnection.Status.TargetIsDraftNode:
                        var inputDef = newSymbolChild.Symbol.GetInputMatchingType(c.ConnectionType);
                        if (inputDef == null)
                        {
                            Log.Warning("Failed to complete node creation");
                            continue;
                        }

                        var newConnectionToInput = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                                         sourceSlotId: c.SourceSlotId,
                                                                         targetParentOrChildId: newSymbolChild.Id,
                                                                         targetSlotId: inputDef.Id);
                        var connectionCommand = new AddConnectionCommand(parentSymbolUi.Symbol, newConnectionToInput, c.MultiInputIndex);
                        connectionCommand.Do();
                        commandsForUndo.Add(connectionCommand);
                        break;
                }
            }

            CompleteOperation(commandsForUndo, "Insert Op " + newChildUi.SymbolChild.ReadableName);
            ParameterPopUp.NodeIdRequestedForParameterWindowActivation = newSymbolChild.Id;
        }
        
        public static void OpenBrowserWithSingleSelection(GraphWindow window, SymbolChildUi childUi, Instance instance)
        {
            if (instance.Outputs.Count < 1)
                return;
            
            //StartOperation("Insert Operator");
            var primaryOutput = instance.Outputs[0];
            
            InsertSymbolBrowser(window, childUi, instance, primaryOutput);
        }

        private static void InsertSymbolBrowser(GraphWindow window, SymbolChildUi childUi, Instance instance, ISlot primaryOutput)
        {
            StartOperation("Insert Operator");
            
            var connections = instance.Parent.Symbol.Connections.FindAll(connection => connection.SourceParentOrChildId == instance.SymbolChildId
                                                                                       && connection.SourceSlotId == primaryOutput.Id);

            TempConnections.Add(new TempConnection(sourceParentOrChildId: instance.SymbolChildId,
                                                   sourceSlotId: primaryOutput.Id,
                                                   targetParentOrChildId: UseDraftChildId,
                                                   targetSlotId: NotConnectedId,
                                                   primaryOutput.ValueType));

            
            Type filterOutputType = null;
            if (connections.Count > 0)
            {
                var mainConnection = connections[0];
                if (mainConnection.IsConnectedToSymbolOutput)
                {
                    var compUi = instance.Parent.GetSymbolUi();
                    
                    if(compUi.OutputUis.TryGetValue(mainConnection.TargetSlotId, out var outputUi))
                    {
                        var moveCommand = new ModifyCanvasElementsCommand(compUi, new List<ISelectableCanvasObject>() {outputUi}, window.GraphCanvas.NodeSelection);
                        outputUi.PosOnCanvas += new Vector2(SymbolChildUi.DefaultOpSize.X, 0);
                        moveCommand.StoreCurrentValues();
                        _inProgressCommand.AddAndExecCommand(moveCommand);
                    }                    
                }
                else
                {
                    AdjustGraphLayoutForNewNode(instance.Parent.Symbol, mainConnection, window.GraphCanvas.NodeSelection);
                    foreach (var oldConnection in connections)
                    {
                        var multiInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(oldConnection);
                        _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(instance.Parent.Symbol, oldConnection, multiInputIndex));
                        TempConnections.Add(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                               sourceSlotId: NotConnectedId,
                                                               targetParentOrChildId: oldConnection.TargetParentOrChildId,
                                                               targetSlotId: oldConnection.TargetSlotId,
                                                               primaryOutput.ValueType,
                                                               multiInputIndex));
                        filterOutputType = primaryOutput.ValueType;
                    }
                }
            }

            window.SymbolBrowser.OpenAt(childUi.PosOnCanvas + new Vector2(childUi.Size.X, 0)
                                                     + new Vector2(SelectableNodeMovement.SnapPadding.X, 0),
                                 primaryOutput.ValueType, filterOutputType, false);
        }

        public static void SplitConnectionWithSymbolBrowser(GraphWindow window, Symbol parentSymbol, Symbol.Connection connection,
                                                            Vector2 positionInCanvas)
        {
            if (connection.IsConnectedToSymbolOutput)
            {
                Log.Debug("Splitting connections to output is not implemented yet");
                return;
            }
            else if (connection.IsConnectedToSymbolInput)
            {
                Log.Debug("Splitting connections from inputs is not implemented yet");
                return;
            }
            
            StartOperation("Split connection with new Operator");

            // Todo: Fix me for output nodes
            var child = parentSymbol.Children[connection.TargetParentOrChildId];
            var inputDef = child.Symbol.InputDefinitions.Single(i => i.Id == connection.TargetSlotId);

            //var commands = new List<ICommand>();
            var multiInputIndex = parentSymbol.GetMultiInputIndexFor(connection);
            //_tempDeletionCommands.Add(  new DeleteConnectionCommand(parentSymbol, connection, multiInputIndex));;

            AdjustGraphLayoutForNewNode(parentSymbol, connection, window.GraphCanvas.NodeSelection);
            _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(parentSymbol, connection, multiInputIndex));
            
            TempConnections.Clear();
            var connectionType = inputDef.DefaultValue.ValueType;
            TempConnections.Add(new TempConnection(sourceParentOrChildId: connection.SourceParentOrChildId,
                                                   sourceSlotId: connection.SourceSlotId,
                                                   targetParentOrChildId: UseDraftChildId,
                                                   targetSlotId: NotConnectedId,
                                                   connectionType));

            TempConnections.Add(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                   sourceSlotId: NotConnectedId,
                                                   targetParentOrChildId: connection.TargetParentOrChildId,
                                                   targetSlotId: connection.TargetSlotId,
                                                   connectionType,
                                                   multiInputIndex));

            window.SymbolBrowser.OpenAt(positionInCanvas, connectionType, connectionType, false);
        }
        #endregion

        public static void SplitConnectionWithDraggedNode(SymbolChildUi childUi, Symbol.Connection oldConnection, Instance instance,
                                                          ModifyCanvasElementsCommand moveCommand, NodeSelection selection)
        { 
            StartOperation($"Split connection with {childUi.SymbolChild.ReadableName}");
            
            _inProgressCommand.AddExecutedCommandForUndo(moveCommand); // FIXME: this will break consistency check 
            
            var parent = instance.Parent;
            var siblingDict = parent.Children;
            if (!siblingDict.TryGetValue(oldConnection.SourceParentOrChildId, out var sourceInstance) 
                || !siblingDict.TryGetValue(oldConnection.TargetParentOrChildId, out var targetInstance))
            {
                Log.Warning("Can't split this connection");
                return;
            }

            var outputDef = sourceInstance.Symbol.OutputDefinitions.Single(outDef => outDef.Id == oldConnection.SourceSlotId);
            var connectionType = outputDef.ValueType;

            // Check if nodes primary input is not connected
            var
                firstMatchingInput = instance.Inputs.FirstOrDefault(input => input.ValueType == connectionType);
            if (instance.Outputs.Count < 1)
            {
                Log.Warning("Can't use node without outputs for splitting");
                return;
            }

            var primaryOutput = instance.Outputs[0];

            if (primaryOutput == null
                || firstMatchingInput == null
                //|| primaryOutput.IsConnected 
                || firstMatchingInput.IsConnected)
            {
                Log.Warning("Op doesn't have valid connections");
                return;
            }

            if (primaryOutput.ValueType != firstMatchingInput.ValueType)
            {
                Log.Warning("Op doesn't match connection type");
                return;
            }

            //var connectionCommands = new List<ICommand>();
            var multiInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(oldConnection);
            AdjustGraphLayoutForNewNode(parent.Symbol, oldConnection, selection);

            var parentUi = parent.Symbol.GetSymbolUi();
            var sourceUi = parentUi.ChildUis[sourceInstance.SymbolChildId];
            var targetUi = parentUi.ChildUis[targetInstance.SymbolChildId];
            var isSnappedHorizontally = (Math.Abs(sourceUi.PosOnCanvas.Y - targetUi.PosOnCanvas.Y) < 0.01f)
                                        && Math.Abs(sourceUi.PosOnCanvas.X + sourceUi.Size.X + SelectableNodeMovement.SnapPadding.X) - targetUi.PosOnCanvas.X <
                                        0.1f;

            var parentSymbolUi = parent.GetSymbolUi();
            if (isSnappedHorizontally)
            {
                childUi.PosOnCanvas = sourceUi.PosOnCanvas + new Vector2(sourceUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0);
                _inProgressCommand.AddAndExecCommand(new ModifyCanvasElementsCommand(parentSymbolUi, [childUi], selection));
            }

            _inProgressCommand.AddAndExecCommand(new DeleteConnectionCommand(parent.Symbol, oldConnection, multiInputIndex));
            _inProgressCommand.AddAndExecCommand(new AddConnectionCommand(parentSymbolUi.Symbol, new Symbol.Connection(oldConnection.SourceParentOrChildId,
                                                                                                 oldConnection.SourceSlotId,
                                                                                                 childUi.SymbolChild.Id,
                                                                                                 firstMatchingInput.Id
                                                                                                ), 0));

            _inProgressCommand.AddAndExecCommand(new AddConnectionCommand(parentSymbolUi.Symbol, new Symbol.Connection(childUi.SymbolChild.Id,
                                                                                                 primaryOutput.Id,
                                                                                                 oldConnection.TargetParentOrChildId,
                                                                                                 oldConnection.TargetSlotId
                                                                                                ), multiInputIndex));
            //var marcoCommand = new MacroCommand("Insert node to connection", connectionCommands);
            //UndoRedoStack.AddAndExecute(marcoCommand);
            CompleteOperation();
        }

        public static void CompleteAtSymbolInputNode(SymbolUi parentSymbolUi, Symbol.InputDefinition inputDef)
        {
            //StartOperation("Insert Node");
            //var macroCommand = new MacroCommand("Insert node");
            foreach (var c in TempConnections)
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                          sourceSlotId: inputDef.Id,
                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                          targetSlotId: c.TargetSlotId);
                _inProgressCommand.AddAndExecCommand(new AddConnectionCommand(parentSymbolUi.Symbol, newConnection, 0));
            }

            //UndoRedoStack.AddAndExecute(macroCommand);
            //Reset();
            CompleteOperation();
            
        }

        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            foreach (var c in TempConnections)
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                          sourceSlotId: c.SourceSlotId,
                                                          targetParentOrChildId: UseSymbolContainerId,
                                                          targetSlotId: outputDef.Id);
                parentSymbol.AddConnection(newConnection);
            }

            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input,
                                                                   int multiInputIndex = 0)
        {
            var inputId = input.Id;
            var connections = parentSymbol.Connections.FindAll(c => c.TargetSlotId == inputId
                                                                    && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
            return (connections.Count > 0) ? connections[multiInputIndex] : null;
        }

        /// <summary>
        /// A special Id the flags a connection as incomplete because either the source or the target is not yet connected.
        /// </summary>
        public static Guid NotConnectedId = Guid.Parse("eeeeeeee-E0DF-47C7-A17F-E297672EE1F3");

        /// <summary>
        /// A special Id that indicates that the source of target of a connection is not a child but an input or output node
        /// </summary>
        public static readonly Guid UseSymbolContainerId = Guid.Empty;

        /// <summary>
        /// A special id indicating that the connection is ending in the <see cref="SymbolBrowser"/>
        /// </summary>
        public static Guid UseDraftChildId = Guid.Parse("ffffffff-E0DF-47C7-A17F-E297672EE1F3");

        private static void SetTempConnection(TempConnection c)
        {
            TempConnections.Clear();
            TempConnections.Add(c);
        }

        public class TempConnection : Symbol.Connection
        {
            public TempConnection(Guid sourceParentOrChildId, Guid sourceSlotId, Guid targetParentOrChildId, Guid targetSlotId, Type type,
                                  int multiInputIndex = 0) :
                base(sourceParentOrChildId, sourceSlotId, targetParentOrChildId, targetSlotId)
            {
                ConnectionType = type;
                MultiInputIndex = multiInputIndex;
            }

            public readonly Type ConnectionType;
            public int MultiInputIndex;

            public Status GetStatus()
            {
                if (TargetParentOrChildId == NotConnectedId
                    && TargetSlotId == NotConnectedId)
                {
                    return Status.TargetIsUndefined;
                }

                if (TargetParentOrChildId == UseDraftChildId
                    && TargetSlotId == NotConnectedId)
                {
                    return Status.TargetIsDraftNode;
                }

                if (SourceParentOrChildId == NotConnectedId
                    && SourceSlotId == NotConnectedId)
                {
                    return Status.SourceIsUndefined;
                }

                if (SourceParentOrChildId == UseDraftChildId
                    && SourceSlotId == NotConnectedId)
                {
                    return Status.SourceIsDraftNode;
                }

                if (SourceParentOrChildId == NotConnectedId
                    && SourceSlotId == NotConnectedId
                    && TargetParentOrChildId == NotConnectedId
                    && TargetSlotId == NotConnectedId
                    )
                {
                    return Status.NotTemporary;
                }

                Log.Warning("Found undefined connection type:" + this);
                return Status.Undefined;
            }

            public enum Status
            {
                NotTemporary,
                SourceIsUndefined,
                SourceIsDraftNode,
                TargetIsUndefined,
                TargetIsDraftNode,
                Undefined,
            }
        }
    }
}