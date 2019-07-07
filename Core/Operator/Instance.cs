using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable
    {
        public abstract Type Type { get; }
        public Guid Id;
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }

        public List<Slot> Outputs { get; set; } = new List<Slot>();
        public List<Instance> Children { get; set; } = new List<Instance>();
        public List<IInputSlot> Inputs { get; set; } = new List<IInputSlot>();

        public void Dispose()
        {
        }

        protected void SetupInputAndOutputsFromType()
        {
            // input identified by base interface
            Type inputSlotType = typeof(IInputSlot);
            var inputInfos = from field in Type.GetFields()
                             where inputSlotType.IsAssignableFrom(field.FieldType)
                             select field;
            foreach (var inputInfo in inputInfos)
            {
                Inputs.Add((IInputSlot)inputInfo.GetValue(this));
            }

            // outputs identified by attribute
            var outputs = (from field in Type.GetFields()
                          let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                          from attr in attributes
                          select field).ToArray();
            foreach (var output in outputs)
            {
                Outputs.Add((Slot)output.GetValue(this));
            }
        }

        public void AddConnection(Symbol.Connection connection, int multiInputIndex)
        {
            var (_, sourceSlot, _, targetSlot) = GetInstancesForConnection(connection);
            targetSlot.AddConnection(sourceSlot, multiInputIndex);
        }

        public void RemoveConnection(Symbol.Connection connection, int index)
        {
            var (_, _, _, targetSlot) = GetInstancesForConnection(connection);
            targetSlot.RemoveConnection(index);
        }

        private (Instance, IConnectableSource, Instance, IConnectableTarget) GetInstancesForConnection(Symbol.Connection connection)
        {
            Instance compositionInstance = this;

            var sourceInstance = compositionInstance.Children.SingleOrDefault(child => child.Id == connection.SourceParentOrChildId);
            IConnectableSource sourceSlot;
            if (sourceInstance != null)
            {
                sourceSlot = sourceInstance.Outputs.Single(output => output.Id == connection.SourceSlotId);
            }
            else
            {
                Debug.Assert(connection.SourceParentOrChildId == Guid.Empty);
                sourceInstance = compositionInstance;
                sourceSlot = sourceInstance.Inputs.Single(input => input.Id == connection.SourceSlotId);
            }

            var targetInstance = compositionInstance.Children.SingleOrDefault(child => child.Id == connection.TargetParentOrChildId);
            IConnectableTarget targetSlot;
            if (targetInstance != null)
            {
                targetSlot = targetInstance.Inputs.Single(e => e.Id == connection.TargetSlotId);
            }
            else
            {
                Debug.Assert(connection.TargetParentOrChildId == Guid.Empty);
                targetInstance = compositionInstance;
                targetSlot = targetInstance.Outputs.Single(e => e.Id == connection.TargetSlotId);
            }

            return (sourceInstance, sourceSlot, targetInstance, targetSlot);
        }
    }

    public class Instance<T> : Instance
    {
        public override Type Type => typeof(T);

        public Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}
