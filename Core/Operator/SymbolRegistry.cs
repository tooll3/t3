using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public static class SymbolRegistry //: IDisposable
    {
        public static Dictionary<Guid, Symbol> Entries { get; } = new();

//         public void Dispose()
//         {
//             foreach (var entry in Entries)
//             {
//                 entry.Value.Dispose();
//             }
//         }
    }
}