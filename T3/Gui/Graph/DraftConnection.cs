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
                && TempConnection.InputDefinitionId == Guid.Empty
                && inputDef.DefaultValue.ValueType == _draftConnectionType;
        }

        public static bool IsMatchingOutput(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                && TempConnection.OutputDefinitionId == Guid.Empty
                && outputDef.ValueType == _draftConnectionType;
        }

        public static bool IsCurrentSourceOutput(SymbolChildUi sourceUi, int outputIndex)
        {
            return TempConnection != null
                && sourceUi.SymbolChild.Id == TempConnection.SourceChildId
                && sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id == TempConnection.OutputDefinitionId;
        }

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
                    sourceChildId: Guid.Empty,
                    outputDefinitionId: Guid.Empty,
                    targetChildId: existingConnections[0].TargetChildId,
                    inputDefinitionId: existingConnections[0].InputDefinitionId
                );

            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceChildId: sourceUi.SymbolChild.Id,
                    outputDefinitionId: outputDef.Id,
                    targetChildId: Guid.Empty,
                    inputDefinitionId: Guid.Empty
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
                    targetChildId: Guid.Empty,
                    inputDefinitionId: Guid.Empty
                );
            }
            else
            {
                TempConnection = new Symbol.Connection(
                    sourceChildId: Guid.Empty,
                    outputDefinitionId: Guid.Empty,
                    targetChildId: targetUi.SymbolChild.Id,
                    inputDefinitionId: inputDef.Id
                );
            }
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

    }
}
