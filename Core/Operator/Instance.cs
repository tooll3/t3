using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable, IGuidPathContainer
    {
        public abstract Type Type { get; }
        public Guid SymbolChildId { get; set; }
        public Instance Parent { get; internal set; }
        public abstract Symbol Symbol { get; }

        public List<ISlot> Outputs { get; set; } = new();
        public List<Instance> Children { get; set; } = new();
        public List<IInputSlot> Inputs { get; set; } = new();

        protected internal ResourceFileWatcher ResourceFileWatcher => Symbol.SymbolPackage.ResourceFileWatcher;

        private List<string> _resourceFolders = null;

        public IReadOnlyList<string> ResourceFolders
        {
            get
            {
                if (_resourceFolders != null)
                    return _resourceFolders;

                GatherResourceFolders(this, out _resourceFolders);
                return _resourceFolders;
            }
        }

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
            var assemblyInfo = Symbol.SymbolPackage.AssemblyInformation;
            foreach (var input in assemblyInfo.InputFields[Type])
            {
                var inputSlot = (IInputSlot)input.Field.GetValue(this);
                inputSlot!.Parent = this;
                inputSlot.Id = input.Attribute.Id;
                inputSlot.MappedType = input.Attribute.MappedType;
                Inputs.Add(inputSlot);
            }

            // outputs identified by attribute
            foreach (var output in assemblyInfo.OutputFields[Type])
            {
                var slot = (ISlot)output.Field.GetValue(this);
                slot!.Parent = this;
                slot.Id = output.Attribute.Id;
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
            Instance sourceInstance = null;
            var gotSourceInstance = false;
            
            foreach(var child in compositionInstance.Children)
            {
                if (child.SymbolChildId != connection.SourceParentOrChildId)
                    continue;
                
                sourceInstance = child;
                gotSourceInstance = true;
                break;
            }

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
            sourceSlot = null;
            var gotSourceSlot = false;
            
            foreach(var slot in sourceSlotList)
            {
                if (slot.Id != connection.SourceSlotId)
                    continue;
                
                sourceSlot = slot;
                gotSourceSlot = true;
                break;
            }
            
            return gotSourceSlot;
        }

        private bool TryGetTargetSlot(Symbol.Connection connection, out ISlot targetSlot)
        {
            var compositionInstance = this;

            // Get target Instance

            Instance targetInstance = null;
            bool gotTargetInstance = false;
            
            foreach(var child in compositionInstance.Children)
            {
                if (child.SymbolChildId != connection.TargetParentOrChildId)
                    continue;
                
                targetInstance = child;
                gotTargetInstance = true;
                break;
            }

            // Get target Slot
            var targetSlotList = gotTargetInstance ? targetInstance.Inputs.Cast<ISlot>() : compositionInstance.Outputs;

            targetSlot = null;
            var gotTargetSlot = false;
            foreach(var slot in targetSlotList)
            {
                if (slot.Id != connection.TargetSlotId)
                    continue;
                
                targetSlot = slot;
                gotTargetSlot = true;
                break;
            }

            #if DEBUG
            if (!gotTargetInstance)
            {
                Debug.Assert(connection.TargetParentOrChildId == Guid.Empty);
            }
            #endif

            return gotTargetSlot;
        }

        private static void GatherResourceFolders(Instance instance, out List<string> resourceFolders)
        {
            resourceFolders = [instance.ResourceFileWatcher.WatchedFolder];

            while (instance.Parent != null)
            {
                instance = instance.Parent;
                var resourceFolder = instance.ResourceFileWatcher.WatchedFolder;

                if (!resourceFolders.Contains(resourceFolder))
                    resourceFolders.Add(resourceFolder);
            }
        }

        public IList<Guid> InstancePath => OperatorUtils.BuildIdPathForInstance(this).ToArray();
    }

    public class Instance<T> : Instance where T : Instance
    {
        public override Type Type { get; } = typeof(T);
        public override Symbol Symbol => _typeSymbol;

        // ReSharper disable once StaticMemberInGenericType
        #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private static Symbol _typeSymbol; // this is set with reflection in Symbol.UpdateType()
        #pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        protected Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}