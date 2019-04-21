using System;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new connections. Provides accessors for highlighting matching input slots.
    /// </summary>
    public static class DraftConnection
    {
        public static Symbol.Connection TempConnection = null;
        //public Symbol.Connection _draftConnectionType = null;
        private static SymbolChildUi _draftConnectionSource = null;
        private static int _draftConnectionIndex = 0;
        private static Type _draftConnectionType = null;

        public static bool IsInputMatchingDraftConnection(Symbol.InputDefinition inputDef)
        {
            return inputDef.DefaultValue.ValueType == _draftConnectionType;
        }

        public static bool IsOutputMatchingDraftConnection(Symbol.InputDefinition outputDef)
        {
            return outputDef.DefaultValue.ValueType == _draftConnectionType;
        }

        public static bool IsDraftConnectionSource(SymbolChildUi childUi, int outputIndex)
        {
            return _draftConnectionSource == childUi && _draftConnectionIndex == outputIndex;
        }


        //public static void StartNewConnection(Symbol.Connection newConnection)
        //{
        //    TempConnection = newConnection;
        //}

        public static void StartFromOutput(SymbolChildUi ui, int outputIndex)
        {
            TempConnection = new Symbol.Connection(
                sourceChildId: ui.SymbolChild.Id,
                outputDefinitionId: ui.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id,
                targetChildId: Guid.Empty,
                inputDefinitionId: Guid.Empty
            );
            _draftConnectionSource = ui;

        }

        public static void Update()
        {

        }

        public static void Complete()
        {
            TempConnection = null;
            _draftConnectionSource = null;
        }
    }
}
