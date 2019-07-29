using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="ConnectionLine"/>. It provides accessors for highlighting matching input slots.
    /// </summary>
    public static class BuildingConnections
    {
        public static Symbol.Connection TempConnection = null;

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.TargetSlotId == NotConnected
                   //&& inputDef.DefaultValue.ValueType == _draftConnectionType;
                   && _draftConnectionType == valueType;
        }

        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.SourceSlotId == NotConnected
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
                   && TempConnection.SourceParentOrChildId == UseSymbolContainer
                   && TempConnection.SourceSlotId == inputDef.Id;
        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                   && TempConnection.TargetParentOrChildId == UseSymbolContainer
                   && TempConnection.TargetSlotId == outputDef.Id;
        }

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            var existingConnections = FindConnectionsFromOutputSlot(parentSymbol, sourceUi, outputDef);
            TempConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                   sourceSlotId: outputDef.Id,
                                                   targetParentOrChildId: NotConnected,
                                                   targetSlotId: NotConnected);
            _draftConnectionType = outputDef.ValueType;
        }

        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputDef, multiInputIndex);

            if (existingConnection != null)
            {
                parentSymbol.RemoveConnection(existingConnection, multiInputIndex);

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnected,
                                                       targetSlotId: NotConnected);
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnected,
                                                       sourceSlotId: NotConnected,
                                                       targetParentOrChildId: targetUi.SymbolChild.Id,
                                                       targetSlotId: inputDef.Id);
            }

            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            TempConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainer,
                                                   sourceSlotId: inputDef.Id,
                                                   targetParentOrChildId: NotConnected,
                                                   targetSlotId: NotConnected);
            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainer
                                                                        && c.TargetSlotId == outputDef.Id);

            if (existingConnection != null)
            {
                parentSymbol.RemoveConnection(existingConnection);

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnected,
                                                       targetSlotId: NotConnected);
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnected,
                                                       sourceSlotId: NotConnected,
                                                       targetParentOrChildId: UseSymbolContainer,
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
            // divide by 2 to get correct insertion index in existing connections
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, multiInputIndex/2));
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

        public static void BuildNodeAtTarget(BuildingNodes nodeBuilding, Vector2 canvasPosition)
        {
            nodeBuilding.OpenAt(canvasPosition);
            //Cancel();
            TempConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                   sourceSlotId: TempConnection.SourceSlotId,
                                                   targetParentOrChildId: UseDraftOperator,
                                                   targetSlotId: Guid.Empty);
        }

        public static void CompleteConnectionToBuiltNode(Symbol parentSymbol, SymbolChild newOp, Symbol.InputDefinition inputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: newOp.Id,
                                                      targetSlotId: inputDef.Id);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainer,
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
                                                      targetParentOrChildId: UseSymbolContainer,
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
            return parentSymbol.Connections.FindAll(c => c.TargetSlotId == inputId
                                                         && c.TargetParentOrChildId == targetUi.SymbolChild.Id)[multiInputIndex];
        }

        /// <summary>
        /// This is a cached value to highlight matching inputs or outputs
        /// </summary>
        private static Type _draftConnectionType = null;

        /// <summary>
        /// A spectial Id the flags a connection as incomplete because either the source or the target is not yet connected.
        /// </summary>
        public static Guid NotConnected = Guid.NewGuid();

        /// <summary>
        /// A special Id that indicates that the source of target of a connection is not a child but an input or output node
        /// </summary>
        public static Guid UseSymbolContainer = Guid.Empty;

        /// <summary>
        /// A special id indicating that the connection is ending in the <see cref="BuildingNodes"/>
        /// </summary>
        public static Guid UseDraftOperator = Guid.NewGuid();
    }
}