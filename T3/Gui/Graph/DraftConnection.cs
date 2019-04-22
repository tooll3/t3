using System;
using System.Collections.Generic;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new connections. Provides accessors for highlighting matching input slots.
    /// </summary>
    public static class DraftConnection
    {
        public static Symbol.Connection TempConnection = null;

        //public static bool IsMatchingInputType(Symbol.InputDefinition inputDef)
        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnection != null
                && TempConnection.InputDefinitionId == NotConnected
                //&& inputDef.DefaultValue.ValueType == _draftConnectionType;
                && _draftConnectionType == valueType;
        }

        //public static bool IsMatchingOutputType(Symbol.OutputDefinition outputDef)
        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnection != null
                && TempConnection.OutputDefinitionId == NotConnected
                && _draftConnectionType == valueType;
        }

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, int outputIndex)
        {
            return TempConnection != null
                && TempConnection.SourceChildId == sourceUi.SymbolChild.Id
                && TempConnection.OutputDefinitionId == sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, int inputIndex)
        {
            return TempConnection != null
                && TempConnection.TargetChildId == targetUi.SymbolChild.Id
                && TempConnection.InputDefinitionId == targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                && TempConnection.SourceChildId == UseSymbolContainer
                && TempConnection.OutputDefinitionId == inputDef.Id;

        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                && TempConnection.TargetChildId == UseSymbolContainer
                && TempConnection.InputDefinitionId == outputDef.Id;
        }

        //public static bool IsCurrentSourceOutput(IConnectionSource connectionSource, int outputIndex)
        //{
        //    return TempConnection != null
        //        && connectionSource.SymbolChild.Id == TempConnection.SourceChildId
        //        && connectionSource.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id == TempConnection.OutputDefinitionId;
        //}

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputDef = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex];
            var existingConnections = FindConnectionsFromOutput(parentSymbol, sourceUi, outputIndex);
            if (existingConnections.Count > 1)
            {
                foreach (var c in existingConnections)
                {
                    parentSymbol.RemoveConnection(c);
                }

                TempConnection = new Symbol.Connection(
                    sourceChildId: NotConnected,
                    outputDefinitionId: NotConnected,
                    targetChildId: existingConnections[0].TargetChildId,
                    inputDefinitionId: existingConnections[0].InputDefinitionId
                );

            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceChildId: sourceUi.SymbolChild.Id,
                    outputDefinitionId: outputDef.Id,
                    targetChildId: NotConnected,
                    inputDefinitionId: NotConnected
                );
            }
            _draftConnectionType = outputDef.ValueType;
            Logging.Log.Debug("Start connection from output of type" + _draftConnectionType.Name);

        }


        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var existingConnection = FindConnectionToInput(parentSymbol, targetUi, inputIndex);
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            if (existingConnection != null)
            {
                parentSymbol.RemoveConnection(existingConnection);

                TempConnection = new Symbol.Connection(
                    sourceChildId: existingConnection.SourceChildId,
                    outputDefinitionId: existingConnection.OutputDefinitionId,
                    targetChildId: NotConnected,
                    inputDefinitionId: NotConnected
                );
            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceChildId: NotConnected,
                    outputDefinitionId: NotConnected,
                    targetChildId: targetUi.SymbolChild.Id,
                    inputDefinitionId: inputDef.Id
                );
            }
            _draftConnectionType = inputDef.DefaultValue.ValueType;
            Logging.Log.Debug("Start connection from input of type" + _draftConnectionType.Name);
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
                sourceChildId: UseSymbolContainer,
                outputDefinitionId: inputDef.Id,
                targetChildId: NotConnected,
                inputDefinitionId: NotConnected
            );
            //}
            _draftConnectionType = inputDef.DefaultValue.ValueType;
        }


        public static void StartFromOutputNode(Symbol.OutputDefinition outputDef)
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
                sourceChildId: NotConnected,
                outputDefinitionId: NotConnected,
                targetChildId: UseSymbolContainer,
                inputDefinitionId: outputDef.Id
            );
            //}
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
                sourceChildId: TempConnection.SourceChildId,
                outputDefinitionId: TempConnection.OutputDefinitionId,
                targetChildId: targetUi.SymbolChild.Id,
                inputDefinitionId: targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var newConnection =
                new Symbol.Connection(
                sourceChildId: sourceUi.SymbolChild.Id,
                outputDefinitionId: sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id,
                targetChildId: TempConnection.TargetChildId,
                inputDefinitionId: TempConnection.InputDefinitionId
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            var newConnection =
                new Symbol.Connection(
                sourceChildId: UseSymbolContainer,
                outputDefinitionId: inputDef.Id,
                targetChildId: TempConnection.TargetChildId,
                inputDefinitionId: TempConnection.InputDefinitionId
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }


        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var newConnection =
                new Symbol.Connection(
                sourceChildId: TempConnection.SourceChildId,
                outputDefinitionId: TempConnection.OutputDefinitionId,
                targetChildId: UseSymbolContainer,
                inputDefinitionId: outputDef.Id
            );
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }



        private static List<Symbol.Connection> FindConnectionsFromOutput(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputId = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
            return parentSymbol.Connections.FindAll(c =>
                c.OutputDefinitionId == outputId &&
                c.SourceChildId == sourceUi.SymbolChild.Id);
        }


        private static Symbol.Connection FindConnectionToInput(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var inputId = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
            return parentSymbol.Connections.Find(c =>
                c.InputDefinitionId == inputId &&
                c.TargetChildId == targetUi.SymbolChild.Id);
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
