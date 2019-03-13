using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using Tooll.Core.PullVariant;

namespace T3Tests
{
    public class OperatorDefinitionManager : IDisposable
    {
        public static OperatorDefinitionManager Instance => _instance ?? (_instance = new OperatorDefinitionManager());
        public Dictionary<Guid, Symbol> Definitions { get; } = new Dictionary<Guid, Symbol>();

        public void Dispose()
        {
            foreach (var entry in Definitions)
            {
                entry.Value.Dispose();
            }
        }


        private static OperatorDefinitionManager _instance;
    }

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

        private readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly List<InstanceDefinition> _children = new List<InstanceDefinition>();

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }
    }

    public class InstanceDefinition
    {
        public Guid SymbolId { get; set; }
        public Guid InstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Symbol Symbol { get; internal set; }
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
    }

    public class InputDefinition : InstanceDefinition
    {
        // relevance: required, relevant, optional
        public Type Type { get; set; }
    }

    public abstract class InputAnnotation
    {
        public abstract void RenderImgui();
    }

    public class FloatInputAnnotation : InputAnnotation
    {
        public override void RenderImgui()
        {
        }
    }

    public class Instance : IDisposable
    {
        public Instance Parent { get; internal set; }

        public void Dispose()
        {
        }

        public List<Instance> Operators { get; } = new List<Instance>();
    }
}
