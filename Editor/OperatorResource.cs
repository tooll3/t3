using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;

namespace T3.Core.Resource
{
    internal sealed class OpPackage
    {
        public CsProjectFile ParentProject { get; }
        public bool Updated { get; set; }
        private Guid SymbolId { get; }
        private Type _type;

        /// <summary>
        /// Updates symbol definition, instances if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        internal static List<Symbol> UpdateChangedOperatorTypes()
        {
            ModifiedSymbols.Clear();
            foreach (var opResource in Operators)
            {
                if (!opResource.Updated)
                    continue;

                var type = opResource._type;
                if (type == null)
                {
                    Log.Error("Error updatable operator had not exported type");
                    continue;
                }

                if (!SymbolRegistry.Entries.TryGetValue(opResource.SymbolId, out var symbol))
                {
                    Log.Info($"Error replacing symbol type '{opResource.Name}");
                    continue;
                }

                OperatorUpdateStopwatch.Restart();
                symbol.UpdateInstanceType(type);
                opResource.Updated = false;
                OperatorUpdateStopwatch.Stop();
                //Log.Info($"type updating took: {(double)_operatorUpdateStopwatch.ElapsedTicks / Stopwatch.Frequency}s");
                ModifiedSymbols.Add(symbol);
            }

            return ModifiedSymbols;
        }

        private static readonly Stopwatch OperatorUpdateStopwatch = new();
        private static readonly List<Symbol> ModifiedSymbols = new();
    }
}