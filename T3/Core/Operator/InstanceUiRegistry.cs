using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public class InstanceUiRegistry
    {
        /*
         * Usage: 
         * var allUiEntries = InstanceUiRegistry.Instance.UiEntries;
         * var uiEntriesForSymbol = allUiEntries[instance.Symbol.Id];
         * var instanceUi = uiEntriesForSymbol[instance.Id];
         * imgui directly modifies the values in instanceUi -> all instances are updated automatically
         */
        public static InstanceUiRegistry Instance => _instance ?? (_instance = new InstanceUiRegistry());
        // symbol id -> (instance id -> instance ui entry)
        public Dictionary<Guid, Dictionary<Guid, SymbolChildUi>> UiEntries { get; } = new Dictionary<Guid, Dictionary<Guid, SymbolChildUi>>();

        private static InstanceUiRegistry _instance;
    }
}