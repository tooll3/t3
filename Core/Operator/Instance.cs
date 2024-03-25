using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable, IGuidPathContainer
    {
        public abstract Type Type { get; }

        private Guid _symbolChildId = Guid.Empty;
        public Guid SymbolChildId
        {
            get => _symbolChildId;
            internal set
            {
                if(_symbolChildId != Guid.Empty)
                    throw new InvalidOperationException("Can't change SymbolChildId after it has been set");
                
                _symbolChildId = value;
            }
        }

        public SymbolChild? SymbolChild => Parent?.Symbol.Children[SymbolChildId];

        private Instance _parent;

        public Instance Parent
        {
            get => _parent;
            internal set
            {
                _parent = value;
                _resourceFoldersDirty = true;
            }
        }

        public Symbol Symbol => SymbolRegistry.SymbolsByType[Type];

        public readonly List<ISlot> Outputs = new();
        public readonly Dictionary<Guid, Instance> Children = new();
        public readonly List<IInputSlot> Inputs = new();
        public bool IsCopy = false;

        public IReadOnlyList<SymbolPackage> AvailableResourcePackages
        {
            get
            {
                GatherResourcePackages(this, ref _availableResourcePackages);
                return _availableResourcePackages;
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
            var symbol = SymbolRegistry.SymbolsByType[Type];
            var assemblyInfo = symbol.SymbolPackage.AssemblyInformation;
            var operatorTypeInfo = assemblyInfo.OperatorTypeInfo[symbol.Id];
            foreach (var input in operatorTypeInfo.Inputs)
            {
                var attribute = input.Attribute;
                var inputSlot = (IInputSlot)input.Field.GetValue(this);
                inputSlot!.Parent = this;
                inputSlot.Id = attribute.Id;
                inputSlot.MappedType = attribute.MappedType;
                Inputs.Add(inputSlot);
            }

            // outputs identified by attribute
            foreach (var output in operatorTypeInfo.Outputs)
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
            if (!TryGetTargetSlot(connection, out var targetSlot))
            {
                return;
            }

            targetSlot.RemoveConnection(index);
        }

        private bool TryGetSourceSlot(Symbol.Connection connection, out ISlot sourceSlot)
        {
            var compositionInstance = this;

            // Get source Instance
            Instance sourceInstance = null;
            var gotSourceInstance = false;
            
            var sourceParentOrChildId = connection.SourceParentOrChildId;
            
            foreach(var child in compositionInstance.Children.Values)
            {
                if (child.SymbolChildId != sourceParentOrChildId)
                    continue;
                
                sourceInstance = child;
                gotSourceInstance = true;
                break;
            }

            // Evaluate correctness of slot source Instance
            var connectionBelongsToThis = sourceParentOrChildId == Guid.Empty;
            if (!gotSourceInstance && !connectionBelongsToThis)
            {
                Log.Error($"Connection in {this} has incorrect source slot: {sourceParentOrChildId}");
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
            
            var targetParentOrChildId = connection.TargetParentOrChildId;

            // Get target Slot
            var targetSlotList = compositionInstance.Children.TryGetValue(targetParentOrChildId, out var targetInstance) 
                                     ? targetInstance.Inputs.Cast<ISlot>() 
                                     : compositionInstance.Outputs;

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
                System.Diagnostics.Debug.Assert(connection.TargetParentOrChildId == Guid.Empty);
            }
            #endif

            return gotTargetSlot;
        }

        private static void GatherResourcePackages(Instance instance, ref List<SymbolPackage> resourceFolders)
        {
            if (!instance._resourceFoldersDirty)
                return;
            
            instance._resourceFoldersDirty = false;
            if(resourceFolders != null)
                resourceFolders.Clear();
            else
                resourceFolders = [];
            
            while (instance != null)
            {
                var package = instance.Symbol.SymbolPackage;
                if (!resourceFolders.Contains(package))
                {
                    resourceFolders.Add(package);
                }

                instance = instance._parent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetFilePath(string relativePath, out string absolutePath)
        {
            return ResourceManager.TryResolvePath(relativePath, AvailableResourcePackages, out absolutePath, out _);
        }

        public IReadOnlyList<Guid> InstancePath => BuildIdPathForInstance(this);

        static List<Guid> BuildIdPathForInstance(Instance instance)
        {
            var result = new List<Guid>(6);
            while (instance != null)
            {
                result.Insert(0, instance.SymbolChildId);
                instance = instance.Parent;
            }

            return result;
        }

        private List<SymbolPackage> _availableResourcePackages;
        private bool _resourceFoldersDirty = true;
    }

    public class Instance<T> : Instance where T : Instance
    {
        public sealed override Type Type { get; } = typeof(T);

        protected Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}