#nullable enable
using System.Diagnostics;
using T3.Core.Animation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Helpers;

// todo - make this non-static to be able to edit multiple projects at once
// each GraphWindow class should have its own Structure object, and some of this logic should be moved out where applicable
// (i.e. dealing with packages, selecting root instasnce, etc
internal class Structure
{
    private readonly Func<Instance> _getRootInstance;

    public Structure(Func<Instance> getRootInstance)
    {
        _getRootInstance = getRootInstance;
    }
    
    public Instance? GetInstanceFromIdPath(List<Guid> compositionPath)
    {
        return TryGetInstanceFromIdPath(_getRootInstance(), compositionPath, out var instance) ? instance : null;
    }

    public List<string> GetReadableInstancePath(List<Guid>? path, bool includeLeave= true)
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
            var childUisWithThatType = parentSymbolUi.ChildUis.FindAll(c => c.SymbolChild.Symbol == instance.Symbol);
            var indexLabel = "";

            var symbolUiChild = childUisWithThatType.SingleOrDefault(c => c.Id == instance.SymbolChildId);

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

    public ITimeClip GetCompositionTimeClip(Instance compositionOp)
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
        foreach (var child in compositionOp.Children)
        {
            foreach (var clipProvider in child.Outputs.OfType<ITimeClipProvider>())
            {
                yield return clipProvider.TimeClip;//CHANGE
            }
        }
    }

    public bool TryGetUiAndInstanceInComposition(Guid id, Instance compositionOp, out SymbolChildUi? childUi, out Instance? instance)
    {
        instance = compositionOp.Children.SingleOrDefault(child => child.SymbolChildId == id);
        if (instance == null)
        {
            Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
            childUi = null;
            return false;
        }

        childUi = compositionOp.GetSymbolChildUiWithId(id);
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
            foreach (var child in s.Children)
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
            foreach (var child in s.Children)
            {
                results.TryGetValue(child.Symbol.Id, out var currentCount);
                results[child.Symbol.Id] = currentCount + 1;
            }
        }

        return results;
    }

    public static HashSet<Symbol> CollectRequiredSymbols(Symbol symbol, HashSet<Symbol> all = null)
    {
        all ??= new HashSet<Symbol>();

        foreach (var symbolChild in symbol.Children)
        {
            if (!all.Add(symbolChild.Symbol))
                continue;

            CollectRequiredSymbols(symbolChild.Symbol, all);
        }

        return all;
    }

    public HashSet<Guid> CollectConnectedChildren(SymbolChild child, Instance composition, HashSet<Guid> set = null)
    {
        set ??= new HashSet<Guid>();

        set.Add(child.Id);
        var compositionSymbol = composition.Symbol;
        var connectedChildren = (from con in compositionSymbol.Connections
                                 where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
                                 from sourceChild in compositionSymbol.Children
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
    public static void CollectSlotDependencies(ISlot slot, HashSet<ISlot> all)
    {
        if (slot == null)
        {
            Log.Warning("skipping null slot");
            return;
        }

        if (!all.Add(slot))
            return;

        if (slot.TryGetFirstConnection(out var firstConnection))
        {
            CollectSlotDependencies(firstConnection, all);
        }
        else
        {
            var parentInstance = slot.Parent;
            foreach (var input in parentInstance.Inputs)
            {
                if (slot.TryGetFirstConnection(out var inputFirstConnection))
                {
                    if (input.TryGetAsMultiInput(out var multiInput))
                    {
                        foreach (var entry in multiInput.GetCollectedInputs())
                        {
                            CollectSlotDependencies(entry, all);
                        }
                    }
                    else
                    {
                        CollectSlotDependencies(inputFirstConnection, all);
                    }
                }
                else if ((input.DirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                {
                    input.DirtyFlag.Invalidate();
                }
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

    public static bool TryGetInstanceFromIdPath(Instance rootInstance, IReadOnlyCollection<Guid>? childPath, out Instance? instance)
    {
        if (childPath == null || childPath.Count == 0)
        {
            instance = null;
            return false;
        }
        
        var rootId = rootInstance.SymbolChildId;
        
        if(childPath.Count == 1 && childPath.First() == rootId)
        {
            instance = rootInstance;
            return true;
        }

        // if the path ends in the root, we have been given an incorrect path
        // this is because a composition cannot have a child of itself
        if (childPath.Last() == rootId)
        {
            instance = null;
            return false;
        }
        
        instance = rootInstance;
        var foundRootId = false;
        foreach (var childId in childPath)
        {
            // Ignore root - skip parts of the path 
            // we give this as a courtesy to the caller to not have to have all paths provided start with the root
            if (!foundRootId)
            {
                foundRootId |= childId == rootId;
                continue;
            }

            if (!instance!.TryGetChildInstance(childId, false, out instance, out _))
            {
                instance = null;
                return false;
            }
        }
        
        if(!foundRootId)
        {
            instance = null;
            Log.Error("Did not find root in path provided - check your path begins with the root provided");
            return false;
        }

        return true;
    }
}