using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public class SymbolRegistry : IDisposable
    {
        public static SymbolRegistry Instance => _instance ?? (_instance = new SymbolRegistry());
        public Dictionary<Guid, Symbol> Definitions { get; } = new Dictionary<Guid, Symbol>();

        public void Dispose()
        {
            foreach (var entry in Definitions)
            {
                entry.Value.Dispose();
            }
        }

        private static SymbolRegistry _instance;
    }
}