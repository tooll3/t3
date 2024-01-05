using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;

namespace T3.Core.Resource
{
    internal sealed class OperatorResource : AbstractResource, IUpdateable
    {
        public CsProjectFile ParentProject { get; }
        public bool Updated { get; set; }
        private Guid SymbolId { get; }
        private Type _type;

        public OperatorResource(uint id, Guid symbolId, Type type, CsProjectFile parentProject, UpdateDelegate updateHandler)
            : base(id, symbolId.ToString())
        {
            _updateHandler = updateHandler;
            ParentProject = parentProject;
            SymbolId = symbolId;
            _type = type;
            
            lock(Operators)
                Operators.Add(this);
        }

        public delegate void UpdateDelegate(OperatorResource resource, string path);

        private readonly UpdateDelegate _updateHandler;

        public void Update(string path)
        {
            _updateHandler?.Invoke(this, path);
        }

        internal void RefreshType()
        {
            var gotType = ParentProject.Assembly.TryGetType(SymbolId, out _type);
            if (!gotType)
            {
                Log.Warning($"Failed to refresh type for {Name} - was it deleted?");
            }
        }

        /// <summary>
        /// Updates symbol definition, instances if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        internal static List<Symbol> UpdateChangedOperatorTypes()
        {
            ModifiedSymbols.Clear();
            lock (Operators)
            {
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
            }

            return ModifiedSymbols;
        }
                
        private static readonly Stopwatch OperatorUpdateStopwatch = new();
        private static readonly List<Symbol> ModifiedSymbols = new();
        private static readonly List<OperatorResource> Operators = new(2000);
    }
}