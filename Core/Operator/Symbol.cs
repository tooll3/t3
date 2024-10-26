using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Core.Operator;

/// <summary>
/// Represents the definition of an operator. It can include:
/// - <see cref="Child"/>s that references other Symbols
/// - <see cref="Connection"/>s that connect these children
/// </summary>
/// <remarks>
/// - There can be multiple <see cref="Instance"/>s of a symbol.
/// </remarks>
public sealed partial class Symbol : IDisposable, IResource
{
    #region Saved Properties
    public readonly Guid Id;
    public IReadOnlyDictionary<Guid, Child> Children => _children;
    public IEnumerable<Instance> InstancesOfSelf => _childrenCreatedFromMe.SelectMany(x => x.Instances);
    public readonly List<Connection> Connections = [];

    /// <summary>
    /// Inputs of this symbol. input values are the default values (exist only once per symbol)
    /// </summary>
    public readonly List<InputDefinition> InputDefinitions = new();
    public readonly List<OutputDefinition> OutputDefinitions = new();
        
    #endregion Saved Properties

    public string Name => _instanceType.Name;
    public string Namespace => _instanceType.Namespace ?? SymbolPackage.AssemblyInformation.Name;
    public Animator Animator { get; } = new();
    public PlaybackSettings PlaybackSettings { get; set; } = new();
        
    public SymbolPackage SymbolPackage { get; set; }
    IResourcePackage IResource.OwningPackage => SymbolPackage;
        
    private Type _instanceType;
    public Type InstanceType
    {
        get => _instanceType;
        private set
        {
            if (_instanceType != null)
            {
                SymbolRegistry.SymbolsByType.Remove(_instanceType, out var symbol);
                if (symbol != this)
                {
                    throw new InvalidOperationException($"Symbol type was not correctly removed from registry. Was this the result of a failed compilation? Symbol found: {symbol}");
                }
            }
                
            _instanceType = value;
            SymbolRegistry.SymbolsByType[value] = this;
        }
    }

    public bool IsGeneric => _instanceType.IsGenericTypeDefinition;

    public Symbol(Type instanceType, Guid symbolId, SymbolPackage symbolPackage)
    {
        Id = symbolId;

        UpdateTypeWithoutUpdatingDefinitionsOrInstances(instanceType, symbolPackage);

        if (instanceType == typeof(object))
            return;

        if (symbolPackage != null)
        {
            UpdateInstanceType();
        }
    }

    internal void UpdateTypeWithoutUpdatingDefinitionsOrInstances(Type instanceType, SymbolPackage symbolPackage)
    {
        SymbolPackage = symbolPackage; // we re-assign this here because symbols can be moved from one package to another
        InstanceType = instanceType;
        NeedsTypeUpdate = true;
    }

    public void Dispose()
    {
        for (var index = 0; index < _childrenCreatedFromMe.Count; index++)
        {
            var child = _childrenCreatedFromMe[index];
            child.Dispose();
            _childrenCreatedFromMe.RemoveAt(index);
        }
    }

    public int GetMultiInputIndexFor(Connection con)
    {
        return Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                        && c.TargetSlotId == con.TargetSlotId)
                          .FindIndex(cc => cc == con); // todo: fix this mess! connection rework!
    }


    public void SortInputSlotsByDefinitionOrder()
    {
        for (var index = 0; index < _childrenCreatedFromMe.Count; index++)
        {
            _childrenCreatedFromMe[index].SortInputSlotsByDefinitionOrder();
        }
    }
        
    public override string ToString() => $"{Namespace}.[{Name}]";

    public bool IsTargetMultiInput(Connection connection)
    {
        return Children.TryGetValue(connection.TargetParentOrChildId, out var child) 
               && child.Inputs.TryGetValue(connection.TargetSlotId, out var targetSlot) 
               && targetSlot.IsMultiInput;
    }

    /// <summary>
    /// Add connection to symbol and its instances
    /// </summary>
    /// <remarks>All connections of a symbol are stored in a single List, from which sorting of multi-inputs
    /// is define. That why inserting connections for those requires to first find the correct index within that
    /// larger list. 
    /// </remarks>
    public void AddConnection(Connection connection, int multiInputIndex = 0)
    {
        var isMultiInput = IsTargetMultiInput(connection);

        // Check if another connection is already existing to the target input, ignoring multi inputs for now
        var connectionsAtInput = Connections.FindAll(c =>
                                                         (c.TargetParentOrChildId == connection.TargetParentOrChildId
                                                          || c.TargetParentOrChildId == Guid.Empty) &&
                                                         c.TargetSlotId == connection.TargetSlotId);

        if (multiInputIndex > connectionsAtInput.Count)
        {
            // todo - solve is to ensure that the multi-input slots aren't cleared of quantity when recompiling, or rather are populated in order
            Log.Error($"Trying to add a connection at the index {multiInputIndex}. Out of bound of the {connectionsAtInput.Count} existing connections.");
            return;
        }

        if (!isMultiInput)
        {
            // Replace existing on single inputs
            if (connectionsAtInput.Count > 0)
            {
                RemoveConnection(connectionsAtInput[0]);
            }

            Connections.Add(connection);
        }
        else
        {
            var insertBefore = multiInputIndex < connectionsAtInput.Count;
            if (insertBefore)
            {
                // Use the target index to find the existing successor among the connections
                var existingConnection = connectionsAtInput[multiInputIndex];

                // ReSharper disable once PossibleUnintendedReferenceComparison
                var insertIndex = Connections.FindIndex(c => c == existingConnection);

                Connections.Insert(insertIndex, connection);
            }
            else
            {
                if (connectionsAtInput.Count == 0)
                {
                    Connections.Add(connection);
                }
                else
                {
                    var existingConnection = connectionsAtInput[^1];

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    var insertIndex = Connections.FindIndex(c => c == existingConnection);
                    Connections.Insert(insertIndex + 1, connection);
                }
            }
        }

        for (var index = 0; index < _childrenCreatedFromMe.Count; index++)
        {
            _childrenCreatedFromMe[index].AddConnectionToInstances(connection, multiInputIndex);
        }
    }

    public void RemoveConnection(Connection connection, int multiInputIndex = 0)
    {
        var targetParentOrChildId = connection.TargetParentOrChildId;
        var targetSlotId = connection.TargetSlotId;

        List<Connection> connectionsAtInput = new();
            
        var connections = Connections;

        foreach (var potentialConnection in connections)
        {
            if (potentialConnection.TargetParentOrChildId == targetParentOrChildId &&
                potentialConnection.TargetSlotId == targetSlotId)
            {
                connectionsAtInput.Add(potentialConnection);
            }
        }

        var connectionsAtInputCount = connectionsAtInput.Count;
        if (connectionsAtInputCount == 0 || multiInputIndex >= connectionsAtInputCount)
        {
            Log.Error($"Trying to remove a connection that doesn't exist. Index {multiInputIndex} of {connectionsAtInput.Count}");
            return;
        }

        var existingConnection = connectionsAtInput[multiInputIndex];

        // ReSharper disable once PossibleUnintendedReferenceComparison
        bool removed = false;
        var connectionCount = connections.Count;
        for (var index = 0; index < connectionCount; index++)
        {
            if (connections[index] == existingConnection)
            {
                connections.RemoveAt(index);
                removed = true;
                break;
            }
        }

        if (!removed)
        {
            Log.Warning($"Failed to remove connection.");
            return;
        }

        foreach (var child in _childrenCreatedFromMe)
        {
            child.RemoveConnectionFromInstances(connection, multiInputIndex);
        }
    }

    public void CreateOrUpdateActionsForAnimatedChildren()
    {
        foreach (var instance in InstancesOfSelf)
        {
            Animator.CreateUpdateActionsForExistingCurves(instance.Children.Values);
        }
    }

    internal void CreateAnimationUpdateActionsForSymbolInstances()
    {
        var foundParentsOfMyInstances = new HashSet<Symbol>();
        foreach (var instance in InstancesOfSelf)
        {
            var parent = instance.Parent;
            if (parent != null)
            {
                var parentSymbol = parent.Symbol;
                if (foundParentsOfMyInstances.Add(parentSymbol))
                {
                    parentSymbol.CreateOrUpdateActionsForAnimatedChildren();
                }
            }
        }
    }

    public bool RemoveChild(Guid childId)
    {
        // first remove all connections to or from the child
        Connections.RemoveAll(c => c.SourceParentOrChildId == childId || c.TargetParentOrChildId == childId);
            
        var removedFromSymbol = _children.Remove(childId, out var symbolChild);

        foreach (var me in _childrenCreatedFromMe)
        {
            me.DestroyChildInstances(symbolChild);
        }

        if (removedFromSymbol)
        {
            SymbolPackage.RemoveDependencyOn(symbolChild.Symbol);
        }

        return removedFromSymbol;
    }

    public InputDefinition GetInputMatchingType(Type type)
    {
        foreach (var inputDefinition in InputDefinitions)
        {
            if (type == null || inputDefinition.DefaultValue.ValueType == type)
                return inputDefinition;
        }

        return null;
    }

    public OutputDefinition GetOutputMatchingType(Type type)
    {
        foreach (var outputDefinition in OutputDefinitions)
        {
            if (type == null || outputDefinition.ValueType == type)
                return outputDefinition;
        }

        return null;
    }

    public void InvalidateInputInAllChildInstances(IInputSlot inputSlot)
    {
        var childId = inputSlot.Parent.SymbolChildId;
        var inputId = inputSlot.Id;
        InvalidateInputInAllChildInstances(inputId, childId);
    }

    public void InvalidateInputInAllChildInstances(Guid inputId, Guid childId)
    {
        foreach (var parent in _childrenCreatedFromMe)
        {
            parent.InvalidateInputInChildren(inputId, childId);
        }
    }

    /// <summary>
    /// Invalidates all instances of a symbol input (e.g. if that input's default was modified)
    /// </summary>
    public void InvalidateInputDefaultInInstances(IInputSlot inputSlot)
    {
        var inputId = inputSlot.Id;
        for (var index = 0; index < _childrenCreatedFromMe.Count; index++)
        {
            var child = _childrenCreatedFromMe[index];
            child.InvalidateInputDefaultInInstances(inputId);
        }
    }

    private bool NeedsTypeUpdate { get; set; } = true;
    //private IEnumerable<Instance> _instancesOfSelf => _childrenCreatedFromMe.SelectMany(x => x.Instances);
    private ConcurrentDictionary<Guid, Child> _children = new();
    private readonly SynchronizedCollection<Child> _childrenCreatedFromMe = new();
}