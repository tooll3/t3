using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="ConnectionLine"/>s. 
    /// It provides accessors for highlighting matching input slots and methods that need to be
    /// called when connections are completed or aborted.
    /// </summary>
    public static class ConnectionMaker
    {
        public static Symbol.Connection TempConnection = null;

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.TargetSlotId == NotConnectedId
                   //&& inputDef.DefaultValue.ValueType == _draftConnectionType;
                   && _draftConnectionType == valueType;
        }

        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.SourceSlotId == NotConnectedId
                   && _draftConnectionType == valueType;
        }

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                   && TempConnection.SourceParentOrChildId == sourceUi.SymbolChild.Id
                   && TempConnection.SourceSlotId == outputDef.Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            return TempConnection != null
                   && TempConnection.TargetParentOrChildId == targetUi.SymbolChild.Id
                   && TempConnection.TargetSlotId == inputDef.Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                   && TempConnection.SourceParentOrChildId == UseSymbolContainerId
                   && TempConnection.SourceSlotId == inputDef.Id;
        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                   && TempConnection.TargetParentOrChildId == UseSymbolContainerId
                   && TempConnection.TargetSlotId == outputDef.Id;
        }

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            var existingConnections = FindConnectionsFromOutputSlot(parentSymbol, sourceUi, outputDef);
            TempConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                   sourceSlotId: outputDef.Id,
                                                   targetParentOrChildId: NotConnectedId,
                                                   targetSlotId: NotConnectedId);
            _draftConnectionType = outputDef.ValueType;
            _isDisconnectinFromInput = false;
        }

        private static bool _isDisconnectinFromInput;
        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputDef, multiInputIndex);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, multiInputIndex));

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnectedId,
                                                       targetSlotId: NotConnectedId);
                _isDisconnectinFromInput = true;
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnectedId,
                                                       sourceSlotId: NotConnectedId,
                                                       targetParentOrChildId: targetUi.SymbolChild.Id,
                                                       targetSlotId: inputDef.Id);
                _isDisconnectinFromInput = false;
            }

            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            TempConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                   sourceSlotId: inputDef.Id,
                                                   targetParentOrChildId: NotConnectedId,
                                                   targetSlotId: NotConnectedId);
            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainerId
                                                                        && c.TargetSlotId == outputDef.Id);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, 0));

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnectedId,
                                                       targetSlotId: NotConnectedId);
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnectedId,
                                                       sourceSlotId: NotConnectedId,
                                                       targetParentOrChildId: UseSymbolContainerId,
                                                       targetSlotId: outputDef.Id);
            }

            _draftConnectionType = outputDef.ValueType;
        }

        public static void Update()
        {
        }

        public static void Cancel()
        {
            TempConnection = null;
            _draftConnectionType = null;
        }

        public static void CompleteAtInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input, int multiInputIndex = 0,
                                               bool insertMultiInput = false)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
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

            TempConnection = null;
        }

        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                      sourceSlotId: output.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void InitSymbolBrowserAtPosition(SymbolBrowser symbolBrowser, Vector2 canvasPosition)
        {
            if (TempConnection == null)
                return;

            if (_isDisconnectinFromInput)
            {
                TempConnection = null;
                return;
            }
                 
            
            if (TempConnection.TargetParentOrChildId == NotConnectedId)
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                       sourceSlotId: TempConnection.SourceSlotId,
                                                       targetParentOrChildId: UseDraftChildId,
                                                       targetSlotId: Guid.Empty);
                symbolBrowser.OpenAt(canvasPosition, _draftConnectionType, null);
            }
            else if (TempConnection.SourceParentOrChildId == NotConnectedId)
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: UseDraftChildId,
                                                       sourceSlotId: Guid.Empty,
                                                       targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                       targetSlotId: TempConnection.TargetSlotId);
                symbolBrowser.OpenAt(canvasPosition, null, _draftConnectionType);
            }
        }

        public static void CompleteConnectionIntoBuiltNode(Symbol parentSymbol, SymbolChild newOp, Symbol.InputDefinition inputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: newOp.Id,
                                                      targetSlotId: inputDef.Id);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteConnectionFromBuiltNode(Symbol parentSymbol, SymbolChild newOp, Symbol.OutputDefinition outputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: newOp.Id,
                                                      sourceSlotId: outputDef.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                      sourceSlotId: inputDef.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: UseSymbolContainerId,
                                                      targetSlotId: outputDef.Id);
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }

        private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputId = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
            return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
                                                         && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        }

        private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            var outputId = output.Id;
            return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
                                                         && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        }

        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var inputId = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
            return parentSymbol.Connections.Find(c => c.TargetSlotId == inputId
                                                      && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
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
        /// This is a cached value to highlight matching inputs or outputs
        /// </summary>
        private static Type _draftConnectionType = null;

        /// <summary>
        /// A spectial Id the flags a connection as incomplete because either the source or the target is not yet connected.
        /// </summary>
        public static Guid NotConnectedId = Guid.NewGuid();

        /// <summary>
        /// A special Id that indicates that the source of target of a connection is not a child but an input or output node
        /// </summary>
        public static Guid UseSymbolContainerId = Guid.Empty;

        /// <summary>
        /// A special id indicating that the connection is ending in the <see cref="SymbolBrowser"/>
        /// </summary>
        public static Guid UseDraftChildId = Guid.NewGuid();
    }
}