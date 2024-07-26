#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator.Slots;

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

        var newChildInstances = new List<Instance>(_instancesOfSelf.Count);

        foreach (var instance in _instancesOfSelf)
        {
            if (newChild.TryCreateNewInstance(instance, out var newChildInstance))
                newChildInstances.Add(newChildInstance);
        }

        Animator.CreateUpdateActionsForExistingCurves(newChildInstances);
        return newChild;
    }

    public bool TryCreateParentlessInstance([NotNullWhen(true)] out Instance? newInstance)
    {
        var newSymbolChildId = Child.CreateIdDeterministically(this, null);
        var newSymbolChild = new Child(this, newSymbolChildId, null, null, false);
        return newSymbolChild.TryCreateNewInstance(null, out newInstance);
    }
}