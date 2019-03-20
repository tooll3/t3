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

        Instance CreateInstance()
        {
            return null;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

        public readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<Connection> _connections = new List<Connection>();
        public readonly List<InstanceDefinition> _children = new List<InstanceDefinition>();

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }
    }

    public class InstanceDefinition
    {
        public Guid InstanceId { get; set; }
        public Symbol Symbol { get; internal set; }
    }

    public class InputDefinition : InstanceDefinition
    {
        // relevance: required, relevant, optional
        public Type Type { get; set; }
    }
}