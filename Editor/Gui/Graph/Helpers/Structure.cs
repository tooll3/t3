using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Helpers;

internal static class Structure
{
    public static Instance GetInstanceFromIdPath(List<Guid> compositionPath)
    {
        return OperatorUtils.GetInstanceFromIdPath(T3Ui.UiSymbolData.RootInstance, compositionPath);
    }

    public static List<string> GetReadableInstancePath(List<Guid> path, bool includeLeave= true)
    {
        if (path == null || (includeLeave && path.Count == 0) || (!includeLeave && path.Count == 1)) 
            return new List<string> { "Path empty" };

        var instance = GetInstanceFromIdPath(path);
        
        if (instance == null)
            return new List<string> { "Path invalid" };

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

            var parentSymbolUi = SymbolUiRegistry.Entries[parent.Symbol.Id];
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

    public static ITimeClip GetCompositionTimeClip(Instance compositionOp)
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
                yield return clipProvider.TimeClip;
            }
        }
    }

    public static bool TryGetUiAndInstanceInComposition(Guid id, Instance compositionOp, out SymbolChildUi childUi, out Instance instance)
    {
        instance = compositionOp.Children.SingleOrDefault(child => child.SymbolChildId == id);
        if (instance == null)
        {
            Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
            childUi = null;
            return false;
        }

        childUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id].ChildUis.SingleOrDefault(ui => ui.Id == id);
        if (childUi == null)
        {
            Log.Assert($"Can't select child with id {id} in composition {compositionOp}");
            return false;
        }

        return true;
    }

    public static IEnumerable<Symbol> CollectDependingSymbols(Symbol symbol)
    {
        foreach (var s in SymbolRegistry.Entries.Values)
        {
            foreach (var ss in s.Children)
            {
                if (ss.Symbol.Id != symbol.Id)
                    continue;

                yield return s;
                break;
            }
        }
    }

    public static Dictionary<Guid, int> CollectSymbolUsageCounts()
    {
        var results = new Dictionary<Guid, int>();

        foreach (var s in SymbolRegistry.Entries.Values)
        {
            foreach (var child in s.Children)
            {
                results.TryGetValue(child.Symbol.Id, out var currentCount);
                results[child.Symbol.Id] = currentCount + 1;
            }
        }

        return results;
    }

    public static HashSet<Guid> CollectRequiredSymbolIds(Symbol symbol, HashSet<Guid> all = null)
    {
        all ??= new HashSet<Guid>();

        foreach (var symbolChild in symbol.Children)
        {
            if (all.Contains(symbolChild.Symbol.Id))
                continue;

            all.Add(symbolChild.Symbol.Id);
            CollectRequiredSymbolIds(symbolChild.Symbol, all);
        }

        return all;
    }

    public static HashSet<Guid> CollectConnectedChildren(SymbolChild child, HashSet<Guid> set = null)
    {
        set ??= new HashSet<Guid>();

        set.Add(child.Id);
        var compositionSymbol = GraphCanvas.Current.CompositionOp.Symbol;
        var connectedChildren = (from con in compositionSymbol.Connections
                                 where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
                                 from sourceChild in compositionSymbol.Children
                                 where con.SourceParentOrChildId == sourceChild.Id
                                       && con.TargetParentOrChildId == child.Id
                                 select sourceChild).Distinct().ToArray();

        foreach (var connectedChild in connectedChildren)
        {
            set.Add(connectedChild.Id);
            CollectConnectedChildren(connectedChild, set);
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

        if (all.Contains(slot))
            return;

        all.Add(slot);

        if (slot is IInputSlot)
        {
            if (!slot.IsConnected)
                return;

            CollectSlotDependencies(slot.GetConnection(0), all);
        }
        else if (slot.IsConnected)
        {
            CollectSlotDependencies(slot.GetConnection(0), all);
        }
        else
        {
            var parentInstance = slot.Parent;
            foreach (var input in parentInstance.Inputs)
            {
                if (input.IsConnected)
                {
                    if (input.IsMultiInput)
                    {
                        var multiInput = (IMultiInputSlot)input;

                        foreach (var entry in multiInput.GetCollectedInputs())
                        {
                            CollectSlotDependencies(entry, all);
                        }
                    }
                    else
                    {
                        var target = input.GetConnection(0);
                        CollectSlotDependencies(target, all);
                    }
                }
                else if ((input.DirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                {
                    input.DirtyFlag.Invalidate();
                }
            }
        }
    }

    public static IEnumerable<Instance> CollectParentInstances(Instance compositionOp, bool includeChildInstance = false)
    {
        var parents = new List<Instance>();
        var op = compositionOp;
        if (includeChildInstance)
            parents.Add(op);

        while (op.Parent != null)
        {
            op = op.Parent;
            parents.Insert(0, op);
        }

        return parents;
    }
}