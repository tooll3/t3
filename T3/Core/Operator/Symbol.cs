using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public class Symbol : IDisposable
    {
        public Guid Id { get; set; }
        public string SourcePath { get; set; }
        public string SymbolName { get; set; }
        public string Namespace { get; set; }

        public Symbol()
        {
        }

        public class Connection
        {
            public Guid SourceInstanceId { get; }
            public Guid SourceSlotId { get; }
            public Guid TargetInstanceId { get; }
            public Guid TargetSlotId { get; }

            public Connection(Guid sourceInstanceId, Guid sourceSlotId, Guid targetInstanceId, Guid targetSlotId)
            {
                SourceInstanceId = sourceInstanceId;
                SourceSlotId = sourceSlotId;
                TargetInstanceId = targetInstanceId;
                TargetSlotId = targetSlotId;
            }
        }

        public Instance CreateInstance()
        {
            var newInstance = new Instance() { Symbol = this };

            foreach (var childInstanceDef in _children)
            {
                var childInstance = new Instance()
                                    {
                                        Id = childInstanceDef.InstanceId,
                                        Parent = newInstance,
                                        Symbol = childInstanceDef.Symbol
                                    };
                newInstance.Children.Add(childInstance);
            }

            _instancesOfSymbol.Add(newInstance);

            return newInstance;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

        public readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<Connection> _connections = new List<Connection>();
        public readonly List<SymbolChild> _children = new List<SymbolChild>();

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }
    }

    public class SymbolChild
    {
        public Guid InstanceId { get; set; }
        public Symbol Symbol { get; internal set; }
    }

    public class InputDefinition : SymbolChild
    {
        // relevance: required, relevant, optional
        public Type Type { get; set; }
    }
}