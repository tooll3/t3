using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public static class SymbolRegistry //: IDisposable
    {
        public static IReadOnlyDictionary<Guid, Symbol> Entries => EntriesEditable;
        public static readonly ConcurrentDictionary<Guid, Symbol> EntriesEditable = new(-1, 1000);

        //         public void Dispose()
        //         {
        //             foreach (var entry in Entries)
        //             {
        //                 entry.Value.Dispose();
        //             }
        //         }
    }
}