using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Windows.Variations;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Interaction.Variations;

/// <summary>
/// Applies actions on variations to the currently active pool.
/// </summary>
/// <remarks>
/// Variations are a sets of symbolChild.input-parameters combinations defined for an Symbol.
/// These input slots can also include the symbols out inputs which thus can be used for defining
/// and applying "presets" to instances of that symbol.
///
/// Most variations will modify(!) the parent symbol. This is great while working within a single symbol
/// and tweaking and blending parameters. However it's potentially unintended (or dangerous) if the
/// modified symbol has many instances. That's why applying symbol-variations is not allowed for Symbols
/// in the lib-namespace.  
/// </remarks>
internal static class VariationHandling
{
    public static SymbolVariationPool ActivePoolForSnapshots { get; private set; }
    public static Instance ActiveInstanceForSnapshots { get; private set; }

    public static SymbolVariationPool ActivePoolForPresets { get; private set; }
    public static Instance ActiveInstanceForPresets { get; private set; }

    /// <summary>
    /// Update variation handling
    /// </summary>
    public static void Update()
    {
        // Sync with composition selected in UI
        var primaryGraphWindow = GraphWindow.Focused;
        if (primaryGraphWindow == null)
            return;

        var nodeSelection = primaryGraphWindow.GraphCanvas.NodeSelection;
        var singleSelectedInstance = nodeSelection.GetSelectedInstanceWithoutComposition();
        
        if (singleSelectedInstance != null)
        {
            var selectedSymbolId = singleSelectedInstance.Symbol.Id;
            ActivePoolForPresets = GetOrLoadVariations(selectedSymbolId);
            ActivePoolForSnapshots = GetOrLoadVariations(singleSelectedInstance.Parent.Symbol.Id);
            ActiveInstanceForPresets = singleSelectedInstance;
            ActiveInstanceForSnapshots = singleSelectedInstance.Parent;
        }
        else
        {
            ActivePoolForPresets = null;

            var activeCompositionInstance = primaryGraphWindow.CompositionOp;
            
            ActiveInstanceForSnapshots = activeCompositionInstance;

            // Prevent variations for library operators
            if (activeCompositionInstance.Symbol.Namespace.StartsWith("Lib."))
            {
                ActivePoolForSnapshots = null;
            }
            else
            {
                ActivePoolForSnapshots = GetOrLoadVariations(activeCompositionInstance.Symbol.Id);
            }

            if (!nodeSelection.IsAnythingSelected())
            {
                ActiveInstanceForPresets = ActiveInstanceForSnapshots;
            }
        }
        
        BlendActions.SmoothVariationBlending.UpdateBlend();
    }

    public static SymbolVariationPool GetOrLoadVariations(Guid symbolId)
    {
        if (_variationPoolForOperators.TryGetValue(symbolId, out var variationForComposition))
        {
            return variationForComposition;
        }

        var newOpVariationPool = new SymbolVariationPool(symbolId);
        _variationPoolForOperators[newOpVariationPool.SymbolId] = newOpVariationPool;
        return newOpVariationPool;
    }

    private const int AutoIndex = -1;

    public static Variation CreateOrUpdateSnapshotVariation(int activationIndex = AutoIndex)
    {
        // Only allow for snapshots.
        if (ActivePoolForSnapshots == null || ActiveInstanceForSnapshots == null)
        {
            return null;
        }

        // Delete previous snapshot for that index.
        if (activationIndex != AutoIndex && SymbolVariationPool.TryGetSnapshot(activationIndex, out var existingVariation))
        {
            ActivePoolForSnapshots.DeleteVariation(existingVariation);
        }

        _affectedInstances.Clear();

        AddSnapshotEnabledChildrenToList(ActiveInstanceForSnapshots, _affectedInstances);

        var newVariation = ActivePoolForSnapshots.CreateVariationForCompositionInstances(_affectedInstances);
        if (newVariation == null)
            return null;

        newVariation.PosOnCanvas = VariationBaseCanvas.FindFreePositionForNewThumbnail(ActivePoolForSnapshots.AllVariations);
        if (activationIndex != AutoIndex)
            newVariation.ActivationIndex = activationIndex;

        newVariation.State = Variation.States.Active;
        ActivePoolForSnapshots.SaveVariationsToFile();
        return newVariation;
    }

    // TODO: Implement undo/redo!
    public static void RemoveInstancesFromVariations(IEnumerable<Guid> symbolChildIds, IReadOnlyList<Variation> variations)
    {
        if (ActivePoolForSnapshots == null || ActiveInstanceForSnapshots == null)
        {
            return;
        }

        foreach (var id in symbolChildIds)
        {
            foreach (var variation in variations)
            {
                if (!variation.ParameterSetsForChildIds.ContainsKey(id))
                    continue;

                variation.ParameterSetsForChildIds.Remove(id);
            }
        }

        ActivePoolForSnapshots.SaveVariationsToFile();
    }

    private static void AddSnapshotEnabledChildrenToList(Instance instance, List<Instance> list)
    {
        var compositionUi = instance.GetSymbolUi();
        foreach (var childInstance in instance.Children.Values)
        {
            var symbolChildUi = compositionUi.ChildUis[childInstance.SymbolChildId];            // Debug.Assert(symbolChildUi != null);

            if (!symbolChildUi.EnabledForSnapshots)
                continue;

            list.Add(childInstance);
        }
    }

    // private static IEnumerable<Instance> GetSnapshotEnabledChildren(Instance instance)
    // {
    //     var compositionUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
    //     foreach (var childInstance in instance.Children)
    //     {
    //         var symbolChildUi = compositionUi.ChildUis.SingleOrDefault(cui => cui.Id == childInstance.SymbolChildId);
    //         Debug.Assert(symbolChildUi != null);
    //
    //         if (symbolChildUi.SnapshotGroupIndex == 0)
    //             continue;
    //
    //         yield return childInstance;
    //     }
    // }
    
    private static readonly Dictionary<Guid, SymbolVariationPool> _variationPoolForOperators = new();
    private static readonly List<Instance> _affectedInstances = new(100);
}