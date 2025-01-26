#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using T3.Core.Logging;

namespace T3.Core.Operator;

public sealed partial class Symbol
{
    public Child AddChild(Symbol symbol, Guid addedChildId, string? name = null, bool isBypassed = false, Action<Child>? modifyAction = null)
    {
        var newChild = new Child(symbol, addedChildId, this, name, isBypassed);
        modifyAction?.Invoke(newChild);

        if (!_children.TryAdd(addedChildId, newChild))
        {
            throw new InvalidOperationException("The ID for symbol child must be unique.");
        }
        
        SymbolPackage.AddDependencyOn(symbol);

        List<Instance> newChildInstances;
        lock (_childrenCreatedFromMe.SyncRoot)
        {
            var count = _childrenCreatedFromMe.Count;
            newChildInstances = new List<Instance>(count);
            for (var index = 0; index < count; index++)
            {
                var child = _childrenCreatedFromMe[index];
                child.AddChildInstances(newChild, newChildInstances);
            }
        }

        
        Animator.CreateUpdateActionsForExistingCurves( newChildInstances);
        return newChild;
    }

    public bool TryGetParentlessInstance([NotNullWhen(true)] out Instance? newInstance)
    {
        newInstance = InstancesOfSelf.FirstOrDefault(x => x.Parent == null);
        if (newInstance != null)
        {
            return true;
        }

        Log.Debug($"Creating parentless instance of {this}");
        
        var newSymbolChildId = Child.CreateIdDeterministically(this, null);
        var newSymbolChild = new Child(this, newSymbolChildId, null, null, false);
        return newSymbolChild.TryCreateNewInstance(null, out newInstance);
    }
}