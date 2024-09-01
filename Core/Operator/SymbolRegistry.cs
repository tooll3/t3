#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using T3.Core.Model;

namespace T3.Core.Operator
{
    public static class SymbolRegistry //: IDisposable
    {
        // todo - it would be nice to get rid of this global dictionary
        // this exists so instances can have access to their slots from their empty constructor - they need to be initialized with symbols
        // it would be nicer and simpler if instances just had an Initialize() abstract method
        internal static readonly ConcurrentDictionary<Type, Symbol> SymbolsByType = new();

        public static bool TryGetSymbol(Guid symbolId, [NotNullWhen(true)] out Symbol? symbol)
        {
            foreach(var package in SymbolPackage.AllPackages)
            {
                if (package.TryGetSymbol(symbolId, out symbol))
                    return true;
            }
            symbol = null;
            return false;
        }
    }
}