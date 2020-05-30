using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable
    {
        public abstract Type Type { get; }
        public Guid SymbolChildId { get; set; }
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }

        public List<ISlot> Outputs { get; set; } = new List<ISlot>();
        public List<Instance> Children { get; set; } = new List<Instance>();
        public List<IInputSlot> Inputs { get; set; } = new List<IInputSlot>();

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
        }

        protected void SetupInputAndOutputsFromType()
        {
            // input identified by base interface
            var inputInfos = from field in Type.GetFields()
                             where typeof(IInputSlot).IsAssignableFrom(field.FieldType)
                             select field;
            foreach (var inputInfo in inputInfos)
            {
                var customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false);
                Debug.Assert(customAttributes.Length == 1);
                var attribute = (InputAttribute)customAttributes[0];
                var inputSlot = (IInputSlot)inputInfo.GetValue(this);
                inputSlot.Parent = this;
                inputSlot.Id = attribute.Id;
                Inputs.Add(inputSlot);
            }

            // outputs identified by attribute
            var outputs = (from field in Type.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           from attr in attributes
                           select (field, (OutputAttribute)attributes[0])).ToArray();
            foreach (var (output, outputAttribute) in outputs)
            {
                var slot = (ISlot)output.GetValue(this);
                slot.Parent = this;
                slot.Id = outputAttribute.Id;
                Outputs.Add(slot);
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

        private (Instance, ISlot, Instance, ISlot) GetInstancesForConnection(Symbol.Connection connection)
        {
            Instance compositionInstance = this;

            var sourceInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.SourceParentOrChildId);
            ISlot sourceSlot;
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

            var targetInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.TargetParentOrChildId);
            ISlot targetSlot;
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

    public class Instance<T> : Instance where T : Instance
    {
        public override Type Type { get; } = typeof(T);

        protected Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}
