using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable, IGuidPathContainer
    {
        public abstract Type Type { get; }
        public Guid SymbolChildId { get; set; }
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }

        public List<ISlot> Outputs { get; set; } = new();
        public List<Instance> Children { get; set; } = new();
        public List<IInputSlot> Inputs { get; set; } = new();

        public Action<EvaluationContext> KeepUpdatedAction;
        
        /// <summary>
        /// get input without GC allocations 
        /// </summary>
        public IInputSlot GetInput(Guid guid)
        {
            //return Inputs.SingleOrDefault(input => input.Id == guid);
            foreach (var i in Inputs)
            {
                if (i.Id == guid)
                    return i;
            }

            return null;
        }
        
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
                inputSlot.MappedType = attribute.MappedType;
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

        public bool TryAddConnection(Symbol.Connection connection, int multiInputIndex)
        {
            var gotSource = TryGetSourceSlot(connection, out var sourceSlot);
            var gotTarget = TryGetTargetSlot(connection, out var targetSlot);

            if (!gotSource || !gotTarget)
                return false;
            
            targetSlot.AddConnection(sourceSlot, multiInputIndex);
            sourceSlot.DirtyFlag.Invalidate();
            return true;
        }

        public void RemoveConnection(Symbol.Connection connection, int index)
        {
            var success = TryGetTargetSlot(connection, out var targetSlot);
            if (!success)
                return;
            targetSlot.RemoveConnection(index);
        }

        private bool TryGetSourceSlot(Symbol.Connection connection, out ISlot sourceSlot)
        {
            var compositionInstance = this;

            // Get source Instance
            var sourceInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.SourceParentOrChildId);
            var gotSourceInstance = sourceInstance != null;

            // Evaluate correctness of slot source Instance
            var connectionBelongsToThis = connection.SourceParentOrChildId == Guid.Empty;
            if (!gotSourceInstance && !connectionBelongsToThis)
            {
                Log.Error($"Connection has incorrect source slot: {connection.SourceParentOrChildId}");
                sourceSlot = null;
                return false;
            }

            // Get source Slot
            var sourceSlotList = gotSourceInstance ? sourceInstance.Outputs : compositionInstance.Inputs.Cast<ISlot>();
            sourceSlot = sourceSlotList.SingleOrDefault(slot => slot.Id == connection.SourceSlotId);
            return sourceSlot is not null;
        }

        private bool TryGetTargetSlot(Symbol.Connection connection, out ISlot targetSlot)
        {
            var compositionInstance = this;
              
            // Get target Instance
            var targetInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.TargetParentOrChildId);
            var gotTargetInstance = targetInstance is not null;

            // Get target Slot
            var targetSlotList = gotTargetInstance ? targetInstance.Inputs.Cast<ISlot>() : compositionInstance.Outputs;
            targetSlot = targetSlotList.SingleOrDefault(slot => slot.Id == connection.TargetSlotId);
            var gotTargetSlot = targetSlot is not null;
            
            #if DEBUG
            if (!gotTargetInstance)
            {
                Debug.Assert(connection.TargetParentOrChildId == Guid.Empty);
            }
            #endif
            
            return gotTargetSlot;
        }

        public IList<Guid> InstancePath => OperatorUtils.BuildIdPathForInstance(this).ToArray();
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
