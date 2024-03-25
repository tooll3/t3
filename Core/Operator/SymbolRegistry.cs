using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public static class SymbolRegistry //: IDisposable
    {
        // todo - it would be nice to get rid of this global dictionary
        public static IReadOnlyDictionary<Guid, Symbol> Entries => EntriesEditable;
        public static readonly ConcurrentDictionary<Guid, Symbol> EntriesEditable = new(-1, 1000);

        // this exists so instances can have access to their slots from their empty constructor - they need to be initialized with symbols
        internal static readonly ConcurrentDictionary<Type, Symbol> SymbolsByType = new();

        //         public void Dispose()
        //         {
        //             foreach (var entry in Entries)
        //             {
        //                 entry.Value.Dispose();
        //             }
        //         }
    }
}