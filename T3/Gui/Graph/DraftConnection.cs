using System;
using System.Collections.Generic;
using T3.Core.Logging;
using T3.Core.Operator;


namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="ConnectionLine"/>. It provides accessors for highlighting matching input slots.
    /// </summary>
    public static class DraftConnection
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

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, int outputIndex)
        {
            return TempConnection != null
                && TempConnection.SourceSymbolChildId == sourceUi.SymbolChild.Id
                && TempConnection.SourceSlotId == sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, int inputIndex)
        {
            return TempConnection != null
                && TempConnection.TargetSymboldChildId == targetUi.SymbolChild.Id
                && TempConnection.TargetSlotId == targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                && TempConnection.SourceSymbolChildId == UseSymbolContainer
                && TempConnection.SourceSlotId == inputDef.Id;

        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                && TempConnection.TargetSymboldChildId == UseSymbolContainer
                && TempConnection.TargetSlotId == outputDef.Id;
        }


        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputDef = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex];
            var existingConnections = FindConnectionsFromOutputSlot(parentSymbol, sourceUi, outputIndex);
            TempConnection = new Symbol.Connection(
                sourceSymbolChildId: sourceUi.SymbolChild.Id,
                sourceSlotId: outputDef.Id,
                targetSymbolChildId: NotConnected,
                targetSlotId: NotConnected
            );
            _draftConnectionType = outputDef.ValueType;
        }


        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputIndex);
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            if (existingConnection != null)
            {
                parentSymbol.RemoveConnection(existingConnection);

                TempConnection = new Symbol.Connection(
                    sourceSymbolChildId: existingConnection.SourceSymbolChildId,
                    sourceSlotId: existingConnection.SourceSlotId,
                    targetSymbolChildId: NotConnected,
                    targetSlotId: NotConnected
                );
            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceSymbolChildId: NotConnected,
                    sourceSlotId: NotConnected,
                    targetSymbolChildId: targetUi.SymbolChild.Id,
                    targetSlotId: inputDef.Id
                );
            }
            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }


        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            // Fixme: Relinking existing connections should be possible
            //var existingConnection = FindConnectionToInput(parentSymbol, targetUi, inputIndex);
            //var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            //if (existingConnection != null)
            //{
            //    parentSymbol.RemoveConnection(existingConnection);

            //    TempConnection = new Symbol.Connection(
            //        sourceChildId: existingConnection.SourceChildId,
            //        outputDefinitionId: existingConnection.OutputDefinitionId,
            //        targetChildId: NotConnected,
            //        inputDefinitionId: NotConnected
            //    );
            //}
            //else
            //{
            TempConnection = new Symbol.Connection(
                sourceSymbolChildId: UseSymbolContainer,
                sourceSlotId: inputDef.Id,
                targetSymbolChildId: NotConnected,
                targetSlotId: NotConnected
            );
            //}
            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }


        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c =>
                 c.TargetSymboldChildId == UseSymbolContainer
                 && c.TargetSlotId == outputDef.Id
                 );

            if (existingConnection != null)
            {
                parentSymbol.RemoveConnection(existingConnection);

                TempConnection = new Symbol.Connection(
                    sourceSymbolChildId: existingConnection.SourceSymbolChildId,
                    sourceSlotId: existingConnection.SourceSlotId,
                    targetSymbolChildId: NotConnected,
                    targetSlotId: NotConnected
                );
            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceSymbolChildId: NotConnected,
                    sourceSlotId: NotConnected,
                    targetSymbolChildId: UseSymbolContainer,
                    targetSlotId: outputDef.Id
                );
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

        public static void CompleteAtInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var newConnection =
                new Symbol.Connection(
                sourceSymbolChildId: TempConnection.SourceSymbolChildId,
                sourceSlotId: TempConnection.SourceSlotId,
                targetSymbolChildId: targetUi.SymbolChild.Id,
                targetSlotId: targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var newConnection =
                new Symbol.Connection(
                sourceSymbolChildId: sourceUi.SymbolChild.Id,
                sourceSlotId: sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id,
                targetSymbolChildId: TempConnection.TargetSymboldChildId,
                targetSlotId: TempConnection.TargetSlotId
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            var newConnection =
                new Symbol.Connection(
                sourceSymbolChildId: UseSymbolContainer,
                sourceSlotId: inputDef.Id,
                targetSymbolChildId: TempConnection.TargetSymboldChildId,
                targetSlotId: TempConnection.TargetSlotId
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var newConnection =
                new Symbol.Connection(
                sourceSymbolChildId: TempConnection.SourceSymbolChildId,
                sourceSlotId: TempConnection.SourceSlotId,
                targetSymbolChildId: UseSymbolContainer,
                targetSlotId: outputDef.Id
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }



        private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputId = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
            return parentSymbol.Connections.FindAll(c =>
                c.SourceSlotId == outputId &&
                c.SourceSymbolChildId == sourceUi.SymbolChild.Id);
        }


        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var inputId = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
            return parentSymbol.Connections.Find(c =>
                c.TargetSlotId == inputId &&
                c.TargetSymboldChildId == targetUi.SymbolChild.Id);
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
        /// 
        /// </summary>
        private static Guid UseSymbolContainer = Guid.Empty;
    }
}
