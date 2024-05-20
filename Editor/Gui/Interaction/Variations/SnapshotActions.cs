using T3.Core.Logging;
using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.Gui.Interaction.Variations;

internal static class SnapshotActions
{
    public static void ActivateOrCreateSnapshotAtIndex(int activationIndex)
    {
        if (VariationHandling.ActivePoolForSnapshots == null)
        {
            Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
            return;
        }

        if (SymbolVariationPool.TryGetSnapshot(activationIndex, out var existingVariation))
        {
            VariationHandling.ActivePoolForSnapshots.Apply(VariationHandling.ActiveInstanceForSnapshots, existingVariation);
            return;
        }

        VariationHandling.CreateOrUpdateSnapshotVariation(activationIndex);
        VariationHandling.ActivePoolForSnapshots.UpdateActiveStateForVariation(activationIndex);
    }

    public static void SaveSnapshotAtIndex(int activationIndex)
    {
        if (VariationHandling.ActivePoolForSnapshots == null)
        {
            Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
            return;
        }

        VariationHandling.CreateOrUpdateSnapshotVariation(activationIndex);
        VariationHandling.ActivePoolForSnapshots.UpdateActiveStateForVariation(activationIndex);
    }

    public static void RemoveSnapshotAtIndex(int activationIndex)
    {
        if (VariationHandling.ActivePoolForSnapshots == null)
            return;

        //ActivePoolForSnapshots.DeleteVariation
        if (SymbolVariationPool.TryGetSnapshot(activationIndex, out var snapshot))
        {
            VariationHandling.ActivePoolForSnapshots.DeleteVariation(snapshot);
        }
        else
        {
            Log.Warning($"No preset to delete at index {activationIndex}");
        }
    }

    public static void SaveSnapshotAtNextFreeSlot(int obj)
    {
        //Log.Warning($"SaveSnapshotAtNextFreeSlot {obj} not implemented");
    }
}