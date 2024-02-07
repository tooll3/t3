using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Resource
{
    public class OperatorResource : AbstractResource, IUpdateable
    {
        public Assembly OperatorAssembly { get; set; }
        public bool Updated { get; set; }
        private Guid SymbolId { get; }

        public OperatorResource(uint id, string nameWithId, Assembly operatorAssembly, UpdateDelegate updateHandler)
            : base(id, nameWithId)
        {
            _updateHandler = updateHandler;
            OperatorAssembly = operatorAssembly;
            SymbolId = Guid.Parse(nameWithId);  
            _operators.Add(this);
        }

        public delegate void UpdateDelegate(OperatorResource resource, string path);

        private readonly UpdateDelegate _updateHandler;

        public void Update(string path)
        {
            _updateHandler?.Invoke(this, path);
        }

        /// <summary>
        /// Updates symbol definition, instances if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        public static List<Symbol> UpdateChangedOperatorTypes()
        {
            _modifiedSymbols.Clear();
            
            // In various situations like saving backup, the _operators list could be modified.
            // This could lead to crashes
            lock (_operators)
            {
                foreach (var opResource in _operators)
                {
                    if (!opResource.Updated)
                        continue;

                    var type = opResource.OperatorAssembly.ExportedTypes.FirstOrDefault();
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
                    
                    _operatorUpdateStopwatch.Restart();
                    symbol.UpdateInstanceType(type);
                    opResource.Updated = false;
                    _operatorUpdateStopwatch.Stop();
                    //Log.Info($"type updating took: {(double)_operatorUpdateStopwatch.ElapsedTicks / Stopwatch.Frequency}s");
                    _modifiedSymbols.Add(symbol);
                    
                }
            }
            return _modifiedSymbols;
        }
        
        public void RemoveOperatorEntry(uint resourceId)
        {
            var resources = ResourceManager.ResourcesById;
            if (!resources.TryGetValue(resourceId, out var entry))
                return;
            
            _operators.Remove(entry as OperatorResource);
            resources.Remove(resourceId);
        }        
                
        private static readonly Stopwatch _operatorUpdateStopwatch = new();
        private static readonly List<Symbol> _modifiedSymbols = new();
        private static readonly List<OperatorResource> _operators = new(1000);
    }
}