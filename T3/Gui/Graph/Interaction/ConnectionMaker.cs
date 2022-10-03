using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using t3.Gui.Commands.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Interaction.TransformGizmos;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="Symbol.Connection"/>s. 
    /// It provides accessors for highlighting matching input slots and methods that need to be
    /// called when connections are completed or aborted.
    /// </summary>
    public static class ConnectionMaker
    {
        public static List<TempConnection> TempConnections = new List<TempConnection>();

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnections.Count == 1
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

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            TempConnections.Clear();
            _isDisconnectingFromInput = false;

            var selectedSymbolChildUis = NodeSelection.GetSelectedChildUis().OrderBy(c => c.PosOnCanvas.Y * 100 + c.PosOnCanvas.X).ToList();
            selectedSymbolChildUis.Reverse();
            
            if (selectedSymbolChildUis.Count > 1 && selectedSymbolChildUis.Any(c => c.Id == sourceUi.Id))
            {
                selectedSymbolChildUis.Reverse();
                foreach (var selectedChild in selectedSymbolChildUis)
                {
                    if (selectedChild.SymbolChild.Symbol.Id != sourceUi.SymbolChild.Symbol.Id)
                        return;

                    TempConnections.Add(new TempConnection(sourceParentOrChildId: selectedChild.SymbolChild.Id,
                                                           sourceSlotId: outputDef.Id,
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
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, multiInputIndex));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     inputDef.DefaultValue.ValueType));
                _isDisconnectingFromInput = true;
            }
            else
            {
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
            SetTempConnection(new TempConnection(sourceParentOrChildId: UseSymbolContainerId,
                                                 sourceSlotId: inputDef.Id,
                                                 targetParentOrChildId: NotConnectedId,
                                                 targetSlotId: NotConnectedId,
                                                 inputDef.DefaultValue.ValueType));
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainerId
                                                                        && c.TargetSlotId == outputDef.Id);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, 0));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     outputDef.ValueType));
            }
            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: NotConnectedId,
                                                     sourceSlotId: NotConnectedId,
                                                     targetParentOrChildId: UseSymbolContainerId,
                                                     targetSlotId: outputDef.Id,
                                                     outputDef.ValueType));
            }
        }

        public static void Update()
        {
            ConnectionSnapEndHelper.PrepareNewFrame();
        }

        public static void Cancel()
        {
            TempConnections.Clear();
            _isDisconnectingFromInput = false;
            ConnectionSnapEndHelper.ResetSnapping();
        }

        public static MacroCommand AdjustGraphLayoutForNewNode(Symbol parent, Symbol.Connection connection)
        {
            if (connection.IsConnectedToSymbolOutput || connection.IsConnectedToSymbolInput)
            {
                Log.Debug("re-layout is not not supported for input and output nodes yet");
                return null;
            }

            var sourceNode = parent.Children.Single(child => child.Id == connection.SourceParentOrChildId);
            var targetNode = parent.Children.Single(child => child.Id == connection.TargetParentOrChildId);

            var parentSymbolUi = SymbolUiRegistry.Entries[parent.Id];
            var sourceNodeUi = parentSymbolUi.ChildUis.Single(node => node.Id == sourceNode.Id);
            var targetNodeUi = parentSymbolUi.ChildUis.Single(node => node.Id == targetNode.Id);
            var center = (sourceNodeUi.PosOnCanvas + targetNodeUi.PosOnCanvas) / 2;
            var commands = new List<ICommand>();

            var changedSymbols = new List<ISelectableCanvasObject>();

            var requiredGap = SymbolChildUi.DefaultOpSize.X + SelectableNodeMovement.SnapPadding.X;
            var xSource = sourceNodeUi.PosOnCanvas.X + sourceNodeUi.Size.X;
            var xTarget = targetNodeUi.PosOnCanvas.X;

            var currentGap = xTarget - xSource - SelectableNodeMovement.SnapPadding.X;
            if (currentGap > requiredGap)
                return null;

            var offset = Math.Min(requiredGap - currentGap, requiredGap);
            
            // Collect all connected ops further down the tree
            var connectedOps = new HashSet<SymbolChild>();
            RecursivelyAddChildren(ref connectedOps, parent, targetNodeUi.SymbolChild);
            
            foreach( var child in connectedOps)
            {
                var childUi = parentSymbolUi.ChildUis.FirstOrDefault(c => c.Id == child.Id);
                
                if ( childUi == null || childUi.PosOnCanvas.X > center.X)
                    continue;

                changedSymbols.Add(childUi);
                var pos = childUi.PosOnCanvas;

                pos.X -= offset;
                childUi.PosOnCanvas = pos;
            }

            commands.Add(new ModifyCanvasElementsCommand(parentSymbolUi.Symbol.Id, changedSymbols));
            return new MacroCommand("adjust layout", commands);
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
                    
                    var sourceOp = parent.Children.FirstOrDefault(child => child.Id == c.SourceParentOrChildId);
                    if (sourceOp == null)
                    {
                        Log.Error($"Can't find child for connection source {c.SourceParentOrChildId}");
                        continue;
                    }

                    if (set.Contains(sourceOp))
                        continue;
                    
                    
                    set.Add(sourceOp);
                    RecursivelyAddChildren(ref set, parent, sourceOp);
                }
            }
        }

        public static void CompleteAtInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input, int multiInputIndex = 0,
                                               bool insertMultiInput = false)
        {
            // Check for cycles
            var sourceInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.SymbolChildId == TempConnections[0].SourceParentOrChildId);
            if (sourceInstance != null)
            {
                var outputSlot = sourceInstance.Outputs[0];
                var deps = new HashSet<ISlot>();  
                NodeOperations.CollectDependencies(outputSlot, deps);
                
                foreach (var d in deps)
                {
                    if (d.Parent.SymbolChildId != targetUi.SymbolChild.Id)
                        continue;
                    
                    Log.Debug("Sorry, you can't do this. This connection would result in a cycle.");
                    TempConnections.Clear();
                    ConnectionSnapEndHelper.ResetSnapping();
                    return;
                }
            }
            
            // TODO: Support simultaneous connection to multiInput
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnections[0].SourceParentOrChildId,
                                                      sourceSlotId: TempConnections[0].SourceSlotId,
                                                      targetParentOrChildId: targetUi.SymbolChild.Id,
                                                      targetSlotId: input.Id);

            bool replaceConnection = multiInputIndex % 2 != 0;
            multiInputIndex /= 2; // divide by 2 to get correct insertion index in existing connections
            var addCommand = new AddConnectionCommand(parentSymbol, newConnection, multiInputIndex);

            if (replaceConnection)
            {
                // get the previous connection
                var allConnectionsToSlot = parentSymbol.Connections.FindAll(c => c.TargetParentOrChildId == targetUi.SymbolChild.Id &&
                                                                                 c.TargetSlotId == input.Id);
                var connectionToRemove = allConnectionsToSlot[multiInputIndex];
                var deleteCommand = new DeleteConnectionCommand(parentSymbol, connectionToRemove, multiInputIndex);
                var replaceCommand = new MacroCommand("Replace Connection", new ICommand[] { deleteCommand, addCommand });
                UndoRedoStack.AddAndExecute(replaceCommand);
            }
            else
            {
                UndoRedoStack.AddAndExecute(addCommand);
            }

            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            // Check for cycles
            var sourceInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.SymbolChildId == sourceUi.SymbolChild.Id);
            if (sourceInstance != null)
            {
                var deps = new HashSet<ISlot>();
                foreach (var inputSlot in sourceInstance.Inputs)
                {
                    NodeOperations.CollectDependencies(inputSlot, deps);
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
            }

            
            
            // Todo: Support simultaneous connection from multiple inputs
            var newConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                      sourceSlotId: output.Id,
                                                      targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                      targetSlotId: TempConnections[0].TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        #region related to SymbolBrowser
        /// <remarks>
        /// Assumes that a temp connection has be created earlier and is now dropped on the background
        /// </remarks>
        public static void InitSymbolBrowserAtPosition(SymbolBrowser symbolBrowser, Vector2 canvasPosition)
        {
            if (TempConnections.Count == 0)
                return;

            if (_isDisconnectingFromInput)
            {
                TempConnections.Clear();
                _isDisconnectingFromInput = false;
                return;
            }

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
                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null, false, null);
                }
                else if (TempConnections[0].SourceParentOrChildId == NotConnectedId)
                {
                    SetTempConnection(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                         sourceSlotId: NotConnectedId,
                                                         targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                         targetSlotId: TempConnections[0].TargetSlotId,
                                                         firstConnectionType));
                    symbolBrowser.OpenAt(canvasPosition, null, firstConnectionType, false, null);
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
                    var oldConnections = TempConnections.ToArray();
                    TempConnections.Clear();
                    foreach (var c in oldConnections)
                    {
                        TempConnections.Add(new TempConnection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                               sourceSlotId: c.SourceSlotId,
                                                               targetParentOrChildId: UseDraftChildId,
                                                               targetSlotId: NotConnectedId,
                                                               firstConnectionType));
                    }

                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null, onlyMultiInputs: true, null);
                }
            }
            else
            {
                Cancel();
            }
        }

        public static void CompleteConnectsToBuiltNode(Symbol parent, SymbolChild newSymbolChild)
        {
            foreach (var c in TempConnections)
            {
                switch (c.GetStatus())
                {
                    case TempConnection.Status.SourceIsDraftNode:
                        var outputDef = newSymbolChild.Symbol.GetOutputMatchingType(c.ConnectionType);
                        var newConnectionToSource = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                                          sourceSlotId: outputDef.Id,
                                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                                          targetSlotId: c.TargetSlotId);
                        UndoRedoStack.AddAndExecute(new AddConnectionCommand(parent, newConnectionToSource, 0));
                        break;

                    case TempConnection.Status.TargetIsDraftNode:
                        var inputDef = newSymbolChild.Symbol.GetInputMatchingType(c.ConnectionType);
                        if (inputDef == null)
                        {
                            Log.Warning("Failed to complete node creation");
                            Reset();
                            return;
                        }

                        var newConnectionToInput = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                                         sourceSlotId: c.SourceSlotId,
                                                                         targetParentOrChildId: newSymbolChild.Id,
                                                                         targetSlotId: inputDef.Id);
                        UndoRedoStack.AddAndExecute(new AddConnectionCommand(parent, newConnectionToInput, 0));
                        break;
                }
            }

            Reset();
        }


        public static void OpenSymbolBrowserAtOutput(SymbolBrowser symbolBrowser, SymbolChildUi childUi, Instance instance,
                                                       Guid outputId)
        {
            var primaryOutput = instance.Outputs.SingleOrDefault(o => o.Id == outputId);
            if (primaryOutput == null)
                return;
            
            InsertSymbolBrowser(symbolBrowser, childUi, instance, primaryOutput);
        }
        

        public static void OpenBrowserWithSingleSelection(SymbolBrowser symbolBrowser, SymbolChildUi childUi, Instance instance)
        {
            if (instance.Outputs.Count < 1)
                return;
            
            var primaryOutput = instance.Outputs[0];
            InsertSymbolBrowser(symbolBrowser, childUi, instance, primaryOutput);
        }        

        private static void InsertSymbolBrowser(SymbolBrowser symbolBrowser, SymbolChildUi childUi, Instance instance, ISlot primaryOutput)
        {
            var connections = instance.Parent.Symbol.Connections.FindAll(connection => connection.SourceParentOrChildId == instance.SymbolChildId
                                                                                       && connection.SourceSlotId == primaryOutput.Id);

            TempConnections.Clear();
            TempConnections.Add(new TempConnection(sourceParentOrChildId: instance.SymbolChildId,
                                                   sourceSlotId: primaryOutput.Id,
                                                   targetParentOrChildId: UseDraftChildId,
                                                   targetSlotId: NotConnectedId,
                                                   primaryOutput.ValueType));

            var commands = new List<ICommand>();

            if (connections.Count > 0)
            {
                foreach (var oldConnection in connections)
                {
                    var multiInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(oldConnection);
                    commands.Add(new DeleteConnectionCommand(instance.Parent.Symbol, oldConnection, multiInputIndex));
                    TempConnections.Add(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                           sourceSlotId: NotConnectedId,
                                                           targetParentOrChildId: oldConnection.TargetParentOrChildId,
                                                           targetSlotId: oldConnection.TargetSlotId,
                                                           primaryOutput.ValueType,
                                                           multiInputIndex));
                }
            }

            if (connections.Count > 0)
            {
                var adjustLayoutCommand = AdjustGraphLayoutForNewNode(instance.Parent.Symbol, connections[0]);
                if (adjustLayoutCommand != null)
                    commands.Add(adjustLayoutCommand);
            }

            var prepareCommand = new MacroCommand("insert operator", commands);
            prepareCommand.Do();

            symbolBrowser.OpenAt(childUi.PosOnCanvas + new Vector2(childUi.Size.X, 0)
                                                     + new Vector2(SelectableNodeMovement.SnapPadding.X, 0),
                                 primaryOutput.ValueType, null, false, prepareCommand);
        }

        public static void SplitConnectionWithSymbolBrowser(Symbol parentSymbol, SymbolBrowser symbolBrowser, Symbol.Connection connection,
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

            // Todo: Fix me for output nodes
            var child = parentSymbol.Children.Single(child2 => child2.Id == connection.TargetParentOrChildId);
            var inputDef = child.Symbol.InputDefinitions.Single(i => i.Id == connection.TargetSlotId);

            var commands = new List<ICommand>();
            var multiInputIndex = parentSymbol.GetMultiInputIndexFor(connection);
            commands.Add(new DeleteConnectionCommand(parentSymbol, connection, multiInputIndex));

            var adjustLayoutCommand = AdjustGraphLayoutForNewNode(parentSymbol, connection);
            if (adjustLayoutCommand != null)
                commands.Add(adjustLayoutCommand);

            var prepareCommand = new MacroCommand("Split", commands);
            UndoRedoStack.AddAndExecute(prepareCommand);

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

            symbolBrowser.OpenAt(positionInCanvas, connectionType, connectionType, false, prepareCommand);
        }

        #endregion

        public static void SplitConnectionWithDraggedNode(SymbolChildUi childUi, Symbol.Connection oldConnection, Instance instance)
        {
            var parent = instance.Parent;
            var sourceInstance = instance.Parent.Children.SingleOrDefault(child => child.SymbolChildId == oldConnection.SourceParentOrChildId);
            var targetInstance = instance.Parent.Children.SingleOrDefault(child => child.SymbolChildId == oldConnection.TargetParentOrChildId);
            if (sourceInstance == null || targetInstance == null)
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

            var connectionCommands = new List<ICommand>();
            var multiInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(oldConnection);
            var layoutCommand = AdjustGraphLayoutForNewNode(parent.Symbol, oldConnection);
            if (layoutCommand != null)
                connectionCommands.Add(layoutCommand);

            var parentUi = SymbolUiRegistry.Entries[parent.Symbol.Id];
            var sourceUi = parentUi.ChildUis.Single(child => child.Id == sourceInstance.SymbolChildId);
            var targetUi = parentUi.ChildUis.Single(child => child.Id == targetInstance.SymbolChildId);
            var isSnappedHorizontally = (Math.Abs(sourceUi.PosOnCanvas.Y - targetUi.PosOnCanvas.Y) < 0.01f)
                                        && Math.Abs(sourceUi.PosOnCanvas.X + sourceUi.Size.X + SelectableNodeMovement.SnapPadding.X) - targetUi.PosOnCanvas.X <
                                        0.1f;

            if (isSnappedHorizontally)
            {
                childUi.PosOnCanvas = sourceUi.PosOnCanvas + new Vector2(sourceUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0);
                connectionCommands.Add(new ModifyCanvasElementsCommand(parent.Symbol.Id, new List<ISelectableCanvasObject>() { childUi }));
            }

            connectionCommands.Add(new DeleteConnectionCommand(parent.Symbol, oldConnection, multiInputIndex));
            connectionCommands.Add(new AddConnectionCommand(parent.Symbol, new Symbol.Connection(oldConnection.SourceParentOrChildId,
                                                                                                 oldConnection.SourceSlotId,
                                                                                                 childUi.SymbolChild.Id,
                                                                                                 firstMatchingInput.Id
                                                                                                ), 0));

            connectionCommands.Add(new AddConnectionCommand(parent.Symbol, new Symbol.Connection(childUi.SymbolChild.Id,
                                                                                                 primaryOutput.Id,
                                                                                                 oldConnection.TargetParentOrChildId,
                                                                                                 oldConnection.TargetSlotId
                                                                                                ), multiInputIndex));
            var marcoCommand = new MacroCommand("Insert node to connection", connectionCommands);
            UndoRedoStack.AddAndExecute(marcoCommand);
        }

        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            foreach (var c in TempConnections)
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                          sourceSlotId: inputDef.Id,
                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                          targetSlotId: c.TargetSlotId);
                UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            }

            Reset();
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

        private static void Reset()
        {
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
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

        /// <summary>
        /// A helper that collects potential collection targets during connection drag operations.
        /// </summary>
        public static class ConnectionSnapEndHelper
        {
            public static void PrepareNewFrame()
            {
                _mousePosition = ImGui.GetMousePos();
                BestMatchLastFrame = _bestMatchYetForCurrentFrame;
                if (BestMatchLastFrame != null)
                {
                    // drawList.AddRect(_bestMatchLastFrame.Area.Min, _bestMatchLastFrame.Area.Max, Color.Orange);
                    var textSize = ImGui.CalcTextSize(BestMatchLastFrame.Name);
                    ImGui.SetNextWindowPos(_mousePosition + new Vector2(-textSize.X - 20, -textSize.Y / 2));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(BestMatchLastFrame.Name);
                    ImGui.EndTooltip();
                    ImGui.PopStyleVar();
                }

                _bestMatchYetForCurrentFrame = null;
                _bestMatchDistance = float.PositiveInfinity;
            }

            public static void ResetSnapping()
            {
                BestMatchLastFrame = null;
            }

            public static void RegisterAsPotentialTarget(SymbolChildUi childUi, IInputUi inputUi, int slotIndex, ImRect areaOnScreen)
            {
                if (TempConnections == null || TempConnections.Count == 0)
                    return;

                if (TempConnections.All(c => c.ConnectionType != inputUi.Type))
                    return;

                var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
                if (distance > SnapDistance || distance > _bestMatchDistance)
                {
                    return;
                }

                _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                                   {
                                                       TargetParentOrChildId = childUi.SymbolChild.Id,
                                                       TargetInputId = inputUi.InputDefinition.Id,
                                                       Area = areaOnScreen,
                                                       Name = inputUi.InputDefinition.Name,
                                                       SlotIndex = slotIndex
                                                   };
                _bestMatchDistance = distance;
            }

            public static void RegisterAsPotentialTarget(IOutputUi outputUi, ImRect areaOnScreen)
            {
                if (TempConnections == null || TempConnections.Count == 0)
                    return;

                if (TempConnections.All(c => c.ConnectionType != outputUi.Type))
                    return;

                var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
                if (distance > SnapDistance || distance > _bestMatchDistance)
                {
                    return;
                }

                _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                                   {
                                                       TargetParentOrChildId = UseSymbolContainerId,
                                                       TargetInputId = outputUi.OutputDefinition.Id,
                                                       Area = areaOnScreen,
                                                       Name = outputUi.OutputDefinition.Name,
                                                       SlotIndex = 0
                                                   };
                _bestMatchDistance = distance;
            }
            
            
            public static bool IsNextBestTarget(SymbolChildUi childUi, Guid inputDefinitionId, int socketIndex)
            {
                return BestMatchLastFrame != null && BestMatchLastFrame.TargetParentOrChildId == childUi.SymbolChild.Id
                                                  && BestMatchLastFrame.TargetInputId == inputDefinitionId
                                                  && BestMatchLastFrame.SlotIndex == socketIndex;
            }
            
            public static bool IsNextBestTarget(IOutputUi outputUi)
            {
                return BestMatchLastFrame != null && BestMatchLastFrame.TargetParentOrChildId == UseSymbolContainerId
                                                  && BestMatchLastFrame.TargetInputId == outputUi.Id
                                                  && BestMatchLastFrame.SlotIndex == 0;
            }

            public static PotentialConnectionTarget BestMatchLastFrame;
            private static PotentialConnectionTarget _bestMatchYetForCurrentFrame;
            private static float _bestMatchDistance = float.PositiveInfinity;
            private const int SnapDistance = 50;
            private static Vector2 _mousePosition;

            public class PotentialConnectionTarget
            {
                public Guid TargetParentOrChildId;
                public Guid TargetInputId;
                public ImRect Area;
                public string Name;
                public int SlotIndex;
            }
        }

        public class ConnectionSplitHelper
        {
            public static void PrepareNewFrame(GraphCanvas graphCanvas)
            {
                _mousePosition = ImGui.GetMousePos();
                BestMatchLastFrame = _bestMatchYetForCurrentFrame;
                if (BestMatchLastFrame != null && TempConnections.Count == 0)
                {
                    var time = ImGui.GetTime();
                    if (_hoverStartTime < 0)
                        _hoverStartTime = time;

                    var hoverDuration = time - _hoverStartTime;
                    var radius = EaseFunctions.EaseOutElastic((float)hoverDuration) * 4;
                    var drawList = ImGui.GetForegroundDrawList();

                    drawList.AddCircleFilled(_bestMatchYetForCurrentFrame.PositionOnScreen, radius, _bestMatchYetForCurrentFrame.Color, 30);
                    ImGui.SetCursorScreenPos(_bestMatchYetForCurrentFrame.PositionOnScreen - Vector2.One * radius / 2);

                    ImGui.InvisibleButton("splitMe", Vector2.One * radius);
                    if (ImGui.IsItemDeactivated()
                        && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() < UserSettings.Config.ClickThreshold
                        )
                    {
                        var posOnScreen = graphCanvas.InverseTransformPositionFloat(_bestMatchYetForCurrentFrame.PositionOnScreen) - SymbolChildUi.DefaultOpSize / 2;

                        SplitConnectionWithSymbolBrowser(graphCanvas.CompositionOp.Symbol,
                                                         graphCanvas.SymbolBrowser,
                                                         _bestMatchYetForCurrentFrame.Connection,
                                                         posOnScreen);
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.BeginTooltip();
                    {
                        var connection = _bestMatchYetForCurrentFrame.Connection;
                        
                        ISlot outputSlot = null;
                        SymbolChild.Output output = null;
                        Symbol.OutputDefinition outputDefinition = null;
                        
                        var sourceOpInstance = graphCanvas.CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == connection.SourceParentOrChildId);
                        var sourceOp =  graphCanvas.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.SourceParentOrChildId);
                        if (sourceOpInstance != null)
                        {
                            outputDefinition = sourceOpInstance.Symbol.OutputDefinitions.SingleOrDefault(outDef => outDef.Id == connection.SourceSlotId);
                            if (outputDefinition != null && sourceOp != null)
                            {
                                output = sourceOp.Outputs[connection.SourceSlotId];
                                outputSlot = sourceOpInstance.Outputs.Single(slot => slot.Id == outputDefinition.Id);
                            }
                        }

                        SymbolChild.Input input = null;
                        var targetOp = graphCanvas.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.TargetParentOrChildId);
                        if (targetOp != null)
                        {
                            input = targetOp.InputValues[connection.TargetSlotId];
                        }
                        
                        
                        if (outputSlot != null && output != null && input != null)
                        {
                            ImGui.PushFont(Fonts.FontSmall);
                            var connectionSource = sourceOp.ReadableName + "." + output.OutputDefinition.Name;
                            ImGui.TextColored(Color.Gray, connectionSource);
                            
                            var connectionTarget = "->" + targetOp.ReadableName + "." + input.InputDefinition.Name;
                            ImGui.TextColored(Color.Gray, connectionTarget);
                            ImGui.PopFont();

                            var width = 160f;
                            ImGui.BeginChild("thumbnail", new Vector2(width, width*9/16f));
                            {
                                TransformGizmoHandling.SetDrawList(drawList);
                                ImageCanvasForTooltips.Update();
                                ImageCanvasForTooltips.SetAsCurrent();
                                
                                //var sourceOpUi = SymbolUiRegistry.Entries[graphCanvas.CompositionOp.Symbol.Id].ChildUis.Single(childUi => childUi.Id == sourceOp.Id);
                                var sourceOpUi = SymbolUiRegistry.Entries[sourceOpInstance.Symbol.Id];
                                IOutputUi outputUi = sourceOpUi.OutputUis[output.OutputDefinition.Id];
                                EvaluationContext.Reset();
                                EvaluationContext.RequestedResolution = new Size2(1280 / 2, 720 / 2);
                                outputUi.DrawValue(outputSlot, EvaluationContext, recompute: UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Live);

                                if (!string.IsNullOrEmpty(sourceOpUi.Description))
                                {
                                    ImGui.Spacing();
                                    ImGui.PushFont(Fonts.FontSmall);
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Color(1, 1, 1, 0.5f).Rgba);
                                    ImGui.TextWrapped(sourceOpUi.Description);
                                    ImGui.PopStyleColor();
                                    ImGui.PopFont();
                                }
                                ImageCanvasForTooltips.Deactivate();
                                TransformGizmoHandling.RestoreDrawList();
                            }
                            ImGui.EndChild();
                            
                            T3Ui.AddHoveredId(targetOp.Id);
                            T3Ui.AddHoveredId(sourceOp.Id);
                        }
                    }
                    ImGui.EndTooltip();
                    ImGui.PopStyleVar();
                }
                else
                {
                    _hoverStartTime = -1;
                }

                _bestMatchYetForCurrentFrame = null;
                _bestMatchDistance = float.PositiveInfinity;
            }

            public static void ResetSnapping()
            {
                BestMatchLastFrame = null;
            }

            public static void RegisterAsPotentialSplit(Symbol.Connection connection, Color color, Vector2 position)
            {
                var distance = Vector2.Distance(position, _mousePosition);
                if (distance > SnapDistance || distance > _bestMatchDistance)
                {
                    return;
                }

                _bestMatchYetForCurrentFrame = new PotentialConnectionSplit()
                                                   {
                                                       Connection = connection,
                                                       PositionOnScreen = position,
                                                       Color = color,
                                                   };
                _bestMatchDistance = distance;
            }
            
            private static readonly ImageOutputCanvas ImageCanvasForTooltips = new ImageOutputCanvas();
            private static readonly EvaluationContext EvaluationContext = new EvaluationContext();
            
            public static PotentialConnectionSplit BestMatchLastFrame;
            private static PotentialConnectionSplit _bestMatchYetForCurrentFrame;
            private static float _bestMatchDistance = float.PositiveInfinity;
            private const int SnapDistance = 50;
            private static Vector2 _mousePosition;
            private static double _hoverStartTime = -1;

            public class PotentialConnectionSplit
            {
                public Vector2 PositionOnScreen;
                public Symbol.Connection Connection;
                public Color Color;
            }
        }
    }
}
