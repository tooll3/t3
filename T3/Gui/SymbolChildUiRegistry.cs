using System;
using System.Collections.Generic;

namespace T3.Gui
{
    public static class SymbolChildUiRegistry
    {
        // symbol id -> (symbol child id -> symbol child ui entry)
        public static Dictionary<Guid, Dictionary<Guid, SymbolChildUi>> Entries { get; } = new Dictionary<Guid, Dictionary<Guid, SymbolChildUi>>();
    }
}