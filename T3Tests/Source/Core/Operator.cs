using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3Tests
{
    public class OperatorDefinitionManager : IDisposable
    {
        public static OperatorDefinitionManager Instance => _instance ?? (_instance = new OperatorDefinitionManager());
        public Dictionary<Guid, OperatorDefinition> Definitions { get; } = new Dictionary<Guid, OperatorDefinition>();

        public void Dispose()
        {
            foreach (var entry in Definitions)
            {
                entry.Value.Dispose();
            }
        }


        private static OperatorDefinitionManager _instance;
    }

    public class OperatorDefinition : IDisposable
    {
        public string SourcePath { get; set; }
        public string TypeName { get; set; }

        public OperatorDefinition()
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

        Operator CreateInstance()
        {
            return null;
        }
        void DeleteInstance(Operator op)
        {
            _instances.Remove(op);
        }

        public T GetInputMinValue<T>(Guid inputId)
        {
            T value = default(T);



            return value;
        }
        public T GetInputMaxValue<T>(Guid inputId)
        {
            T value = default(T);



            return value;
        }

        private readonly List<Operator> _instances = new List<Operator>();
        private readonly List<Guid> _operators = new List<Guid>();
        private readonly List<Connection> _connections = new List<Connection>();

        public void Dispose()
        {
            _instances.ForEach(instance => instance.Dispose());
        }
    }

    public class Operator : IDisposable
    {
        public string Name { get; set; } = string.Empty;
        public OperatorDefinition Definition { get; internal set; }
        public Operator Parent { get; internal set; }

        public void Dispose()
        {
            
        }

        public List<Operator> Operators { get; } = new List<Operator>();
    }
}
