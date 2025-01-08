#nullable enable
using System.Diagnostics.CodeAnalysis;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.UiModel;

// ReSharper disable ForCanBeConvertedToForeach

namespace T3.Editor.Gui.Graph.Helpers;

/// <summary>
/// each GraphWindow class should have its own Structure object, and some of this logic should
/// be moved out where applicable (i.e. dealing with packages, selecting root instances, etc.) 
/// </summary>
internal sealed class Structure
{
    private readonly Func<Instance> _getRootInstance;

    public Structure(Func<Instance> getRootInstance)
    {
        _getRootInstance = getRootInstance;
    }

    public Instance? GetInstanceFromIdPath(IReadOnlyList<Guid> compositionPath)
    {
        return TryGetInstanceFromIdPath(compositionPath, out var instance) ? instance : null;
    }

    public List<string> GetReadableInstancePath(IReadOnlyList<Guid>? path, bool includeLeave = true)
    {
        if (path == null || (includeLeave && path.Count == 0) || (!includeLeave && path.Count == 1))
            return ["Path empty"];

        var instance = GetInstanceFromIdPath(path);

        if (instance == null)
            return ["Path invalid"];

        var newList = new List<string>();

        var isFirst = true;

        while (true)
        {
            var parent = instance.Parent;
            if (parent == null)
            {
                break;
            }

            if (!includeLeave && isFirst)
            {
                isFirst = false;
                instance = parent;
                continue;
            }

            isFirst = false;

            var parentSymbolUi = parent.GetSymbolUi();
            var childUisWithThatType = parentSymbolUi.ChildUis.Values
                                                     .Where(c => c.SymbolChild.Symbol == instance.Symbol)
                                                     .ToList();
            var indexLabel = "";

            var symbolUiChild = childUisWithThatType.Single(c => c.Id == instance.SymbolChildId);

            if (childUisWithThatType.Count > 1)
            {
                var index = childUisWithThatType.IndexOf(symbolUiChild);
                indexLabel = $"#{index}";
            }

            var readableNameSuffice = !string.IsNullOrEmpty(symbolUiChild?.SymbolChild.Name)
                                          ? $" ({symbolUiChild.SymbolChild.Name})"
                                          : "";

            newList.Insert(0, instance.Symbol.Name + indexLabel + readableNameSuffice);

            instance = parent;
        }

        return newList;
    }

    public static ITimeClip? GetCompositionTimeClip(Instance? compositionOp)
    {
        if (compositionOp == null)
        {
            Log.Error("Can't get time clip from null composition op");
            return null;
        }

        foreach (var clipProvider in compositionOp.Outputs.OfType<ITimeClipProvider>())
        {
            return clipProvider.TimeClip;
        }

        return null;
    }

    /// <summary>
    /// This is slow and should be refactored into something else
    /// </summary>
    public static IEnumerable<ITimeClip> GetAllTimeClips(Instance compositionOp)
    {
        foreach (var child in compositionOp.Children.Values)
        {
            foreach (var clipProvider in child.Outputs.OfType<ITimeClipProvider>())
            {
                yield return clipProvider.TimeClip; //CHANGE
            }
        }
    }

    public static bool TryGetUiAndInstanceInComposition(Guid id,
                                                        Instance compositionOp,
                                                        [NotNullWhen(true)] out SymbolUi.Child? childUi,
                                                        [NotNullWhen(true)] out Instance? instance)
    {
        if (!compositionOp.Children.TryGetValue(id, out instance))
        {
            Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
            childUi = null;
            return false;
        }

        childUi = instance.GetChildUi();
        if (childUi == null)
        {
            Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
            return false;
        }

        return true;
    }

    public static IEnumerable<Symbol> CollectDependingSymbols(Symbol symbol)
    {
        var symbolId = symbol.Id;
        foreach (var s in EditorSymbolPackage.AllSymbols)
        {
            foreach (var child in s.Children.Values)
            {
                if (child.Symbol.Id != symbolId)
                    continue;

                yield return s;
                break;
            }
        }
    }

    public static Dictionary<Guid, int> CollectSymbolUsageCounts()
    {
        var results = new Dictionary<Guid, int>();

        foreach (var s in EditorSymbolPackage.AllSymbols)
        {
            foreach (var child in s.Children.Values)
            {
                results.TryGetValue(child.Symbol.Id, out var currentCount);
                results[child.Symbol.Id] = currentCount + 1;
            }
        }

        return results;
    }

    internal static HashSet<Guid> CollectConnectedChildren(Symbol.Child child, Instance composition, HashSet<Guid> set = null)
    {
        set ??= [];

        set.Add(child.Id);
        var compositionSymbol = composition.Symbol;
        var connectedChildren = (from con in compositionSymbol.Connections
                                 where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
                                 from sourceChild in compositionSymbol.Children.Values
                                 where con.SourceParentOrChildId == sourceChild.Id
                                       && con.TargetParentOrChildId == child.Id
                                 select sourceChild).Distinct().ToArray();

        foreach (var connectedChild in connectedChildren)
        {
            set.Add(connectedChild.Id);
            CollectConnectedChildren(connectedChild, composition, set);
        }

        return set;
    }

    /// <summary>
    /// Scan all slots required for updating a Slot.
    /// This can be used for invalidation and cycle checking. 
    /// </summary>
    private static HashSet<ISlot> CollectSlotDependencies(ISlot slot, HashSet<ISlot>? all = null)
    {
        all ??= [];

        var stack = new Stack<ISlot>();
        stack.Push(slot);

        while (stack.Count > 0)
        {
            var currentSlot = stack.Pop();

            if (!all.Add(currentSlot))
                continue;

            if (currentSlot.TryGetFirstConnection(out var firstConnection))
            {
                stack.Push(firstConnection);
            }
            else
            {
                var op = currentSlot.Parent;
                var opInputs = op.Inputs;

                for (int i = 0; i < opInputs.Count; i++)
                {
                    var input = opInputs[i];

                    // Skip if not connected
                    if (!input.TryGetFirstConnection(out var connectedCompInputSlot))
                        continue;

                    if (input.TryGetAsMultiInput(out var multiInput))
                    {
                        var collectedInputs = multiInput.GetCollectedInputs();
                        for (var j = 0; j < collectedInputs.Count; j++)
                        {
                            stack.Push(collectedInputs[j]);
                        }
                    }
                    else
                    {
                        stack.Push(connectedCompInputSlot);
                    }
                }
            }
        }

        return all;
    }

    /** Returns true if connecting the outputSlot to an input of the op with a symbolChildId would result in a cycle */
    internal static bool CheckForCycle(ISlot outputSlot, Guid targetOpId)
    {
        var linkedSlots = CollectSlotDependencies(outputSlot);
        foreach (var linkedSlot in linkedSlots)
        {
            if (linkedSlot.Parent.SymbolChildId != targetOpId)
                continue;

            return true;
        }

        return false;
    }

    /** Returns true if connecting the outputSlot to an input of the op with a symbolChildId would result in a cycle */
    internal static bool CheckForCycle(Instance sourceInstance, Guid targetOpId)
    {
        var linkedSlots = new HashSet<ISlot>();
        foreach (var inputSlot in sourceInstance.Inputs)
        {
            CollectSlotDependencies(inputSlot, linkedSlots);
        }

        //var linkedSlots = CollectSlotDependencies(outputSlot);
        foreach (var linkedSlot in linkedSlots)
        {
            if (linkedSlot.Parent.SymbolChildId != targetOpId)
                continue;

            return true;
        }

        return false;
    }

    internal static bool CheckForCycle(Symbol compositionSymbol, Symbol.Connection connection)
    {
        var dependingSourceItemIds = new HashSet<Guid>();

        CollectDependentChildren(connection.SourceParentOrChildId);

        return dependingSourceItemIds.Contains(connection.TargetParentOrChildId);

        void CollectDependentChildren(Guid sourceChildId)
        {
            if (sourceChildId == Guid.Empty || !dependingSourceItemIds.Add(sourceChildId))
                return;

            // find all connections into child...
            foreach (var c in compositionSymbol.Connections)
            {
                if (sourceChildId == Guid.Empty || c.TargetParentOrChildId != sourceChildId)
                    continue;

                CollectDependentChildren(c.SourceParentOrChildId);
            }
        }
    }

    public static IEnumerable<Instance> CollectParentInstances(Instance compositionOp)
    {
        var parents = new List<Instance>();
        var op = compositionOp;

        while (op.Parent != null)
        {
            op = op.Parent;
            parents.Insert(0, op);
        }

        return parents;
    }

    private bool TryGetInstanceFromIdPath([NotNullWhen(true)] IReadOnlyList<Guid>? childPath, [NotNullWhen(true)] out Instance? instance)
    {
        if (childPath == null || childPath.Count == 0)
        {
            instance = null;
            return false;
        }

        var rootInstance = _getRootInstance();
        var rootId = rootInstance.SymbolChildId;

        if (childPath[0] != rootId)
        {
            instance = null;
            Log.Warning("Can't access instance after root changed.");
            return false;
            //throw new ArgumentException("Path does not start with the root instance");
        }

        var pathCount = childPath.Count;

        if (pathCount == 1)
        {
            instance = rootInstance;
            return true;
        }

        instance = rootInstance;
        for (int i = 1; i < pathCount; i++)
        {
            if (!instance.Children.TryGetValue(childPath[i], out instance))
            {
                Log.Error("Did not find instance in path provided.\n" + Environment.StackTrace);
                instance = null;
                return false;
            }
        }

        return true;
    }
}