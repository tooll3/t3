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

        public static bool IsMatchingInput(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                && TempConnection.InputDefinitionId == NotConnected
                && inputDef.DefaultValue.ValueType == _draftConnectionType;
        }

        public static bool IsMatchingOutput(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                && TempConnection.OutputDefinitionId == NotConnected
                && outputDef.ValueType == _draftConnectionType;
        }

        public static bool IsCurrentSourceOutput(SymbolChildUi sourceUi, int outputIndex)
        {
            return TempConnection != null
                && sourceUi.SymbolChild.Id == TempConnection.SourceChildId
                && sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id == TempConnection.OutputDefinitionId;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                && UseSymbolContainer == TempConnection.SourceChildId
                && inputDef.Id == TempConnection.OutputDefinitionId;

        }

        //public static bool IsCurrentSourceOutput(IConnectionSource connectionSource, int outputIndex)
        //{
        //    return TempConnection != null
        //        && connectionSource.SymbolChild.Id == TempConnection.SourceChildId
        //        && connectionSource.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id == TempConnection.OutputDefinitionId;
        //}

        public static bool IsCurrentTargetInput(SymbolChildUi targetUi, int inputIndex)
        {
            return TempConnection != null
                && targetUi.SymbolChild.Id == TempConnection.TargetChildId
                && targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id == TempConnection.InputDefinitionId;
        }


        public static void StartFromOutput(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
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


        public static void StartFromInput(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
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
            Logging.Log.Debug("Start connection from input of type" + _draftConnectionType.Name);
        }




        public static void Update()
        {

        }

        public static void Cancel()
        {
            TempConnection = null;
            _draftConnectionType = null;
        }

        public static void CompleteAtOutput(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
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


        public static void CompleteAtInput(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
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

        private static Type _draftConnectionType = null;
        public static Guid NotConnected = Guid.NewGuid();
        private static Guid UseSymbolContainer = Guid.Empty;
    }
}
