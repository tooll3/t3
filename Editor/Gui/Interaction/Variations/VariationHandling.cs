using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Operators.Utils;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Midi;
using T3.Editor.Gui.Interaction.Midi.CompatibleDevices;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Windows.Variations;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Interaction.Variations;

/// <summary>
/// Handles the live integration of variation model to the user interface.
/// </summary>
/// <remarks>
/// Variations are a sets of symbolChild.input-parameters combinations defined for an Symbol.
/// These input slots can also include the symbols out inputs which thus can be used for defining
/// and applying "presets" to instances of that symbol.
///
/// Most variations will modify(!) the parent symbol. This is great while working within a single symbol
/// and tweaking an blending parameters. However it's potentially unintended (or dangerous) if the
/// modified symbol has many instances. That's why applying symbol-variations is not allowed for Symbols
/// in the lib-namespace.  
/// </remarks>
public static class VariationHandling
{
    public static SymbolVariationPool ActivePoolForSnapshots { get; private set; }
    public static Instance ActiveInstanceForSnapshots  { get; private set; }
        
    public static SymbolVariationPool ActivePoolForPresets { get; private set; }
    public static Instance ActiveInstanceForPresets  { get; private set; }

    public static void Init()
    {
        // Scan for output devices (e.g. to update LEDs etc.)
        MidiOutConnectionManager.Init();


    }
        
    /// <summary>
    /// Update variation handling
    /// </summary>
    public static void Update()
    {
        // Sync with composition selected in UI
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        if (primaryGraphWindow == null)
            return;


        var singleSelectedInstance = NodeSelection.GetSelectedInstance();
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
                
            var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
            if (activeCompositionInstance == null)
                return;
                
            ActiveInstanceForSnapshots = activeCompositionInstance;
                
                
            // Prevent variations for library operators
            if (activeCompositionInstance.Symbol.Namespace.StartsWith("lib."))
            {
                ActivePoolForSnapshots = null;
            }
            else
            {
                ActivePoolForSnapshots = GetOrLoadVariations(activeCompositionInstance.Symbol.Id);
            }

            if (!NodeSelection.IsAnythingSelected())
            {
                ActiveInstanceForPresets = ActiveInstanceForSnapshots;
            }
        }

        CompatibleMidiDeviceHandling.UpdateConnectedDevices();
        SmoothVariationBlending.UpdateBlend();
    }

    public static SymbolVariationPool GetOrLoadVariations(Guid symbolId)
    {
        if (_variationPoolForOperators.TryGetValue(symbolId, out var variationForComposition))
        {
            return variationForComposition;
        }

        var newOpVariation = SymbolVariationPool.InitVariationPoolForSymbol(symbolId);
        _variationPoolForOperators[newOpVariation.SymbolId] = newOpVariation;
        return newOpVariation;
    }


    private static readonly Dictionary<Guid, SymbolVariationPool> _variationPoolForOperators = new();

    public static void ActivateOrCreateSnapshotAtIndex(int activationIndex)
    {
        if (ActivePoolForSnapshots == null)
        {
            Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
            return;
        }
            
        if(SymbolVariationPool.TryGetSnapshot(activationIndex, out var existingVariation))
        {
            ActivePoolForSnapshots.Apply(ActiveInstanceForSnapshots, existingVariation);
            return;
        } 
            
        CreateOrUpdateSnapshotVariation(activationIndex);
        ActivePoolForSnapshots.UpdateActiveStateForVariation(activationIndex);
    }

    public static void SaveSnapshotAtIndex(int activationIndex)
    {
        if (ActivePoolForSnapshots == null)
        {
            Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
            return;
        }

        CreateOrUpdateSnapshotVariation(activationIndex);
        ActivePoolForSnapshots.UpdateActiveStateForVariation(activationIndex);
    }

    public static void RemoveSnapshotAtIndex(int activationIndex)
    {
        if (ActivePoolForSnapshots == null)
            return;
            
        //ActivePoolForSnapshots.DeleteVariation
        if (SymbolVariationPool.TryGetSnapshot(activationIndex, out var snapshot))
        {
            ActivePoolForSnapshots.DeleteVariation(snapshot);
        }
        else
        {
            Log.Warning($"No preset to delete at index {activationIndex}");
        }
    }

    public static void StartBlendingSnapshots(int[] indices)
    {
        Log.Warning($"StartBlendingSnapshots {indices.Length} not implemented");
    }

    public static void StartBlendingTowardsSnapshot(int index)
    {
        if (ActiveInstanceForSnapshots == null || ActivePoolForSnapshots == null)
        {
            Log.Warning("Can't blend without active composition or variation pool");
            return;
        }

        if (SymbolVariationPool.TryGetSnapshot(index, out var variation))
        {
            _blendTowardsIndex = index;
            ActivePoolForSnapshots.BeginBlendTowardsSnapshot(ActiveInstanceForSnapshots, variation, 0);
        }
    }

    private static int _blendTowardsIndex = -1;

    public static void UpdateBlendingTowardsProgress(int index, float midiValue)
    {
        if (ActiveInstanceForSnapshots == null || ActivePoolForSnapshots == null)
        {
            Log.Warning("Can't blend without active composition or variation pool");
            return;
        }

        if (_blendTowardsIndex == -1)
        {
            return;
        }
            
        if (SymbolVariationPool.TryGetSnapshot(_blendTowardsIndex, out var variation))
        {
            //_blendTargetVariation = variation;
            var normalizedValue = midiValue/127.0f;
            SmoothVariationBlending.StartBlendTo(variation, normalizedValue);
        }
        else
        {
            SmoothVariationBlending.Stop();
        }
    }


    /// <summary>
    /// Smooths blending between variations to avoid glitches by low 127 midi resolution steps 
    /// </summary>
    private static class SmoothVariationBlending
    {
        public static void StartBlendTo(Variation variation, float normalizedBlendWeight)
        {
            if (variation != _targetVariation)
            {
                _dampedWeight = normalizedBlendWeight;
                _targetVariation = variation;
            }
            _targetWeight = normalizedBlendWeight;
            UpdateBlend();
        } 
            
        public static void UpdateBlend()
        {
            if (_targetVariation == null)
                return;

            _dampedWeight = MathUtils.SpringDamp(_targetWeight,
                                                 _dampedWeight,
                                                 ref _dampingVelocity,
                                                 200f, (float)Playback.LastFrameDuration);

            if (!(MathF.Abs(_dampingVelocity) > 0.0005f))
                return;
                
            ActivePoolForSnapshots.BeginBlendTowardsSnapshot(ActiveInstanceForSnapshots, _targetVariation, _dampedWeight);
        }

        public static void Stop()
        {
            _targetVariation = null;
                
        }
            
        private static float _targetWeight;
        private static float _dampedWeight;
        private static float _dampingVelocity;
        private static Variation _targetVariation;
            
    } 
        
        
    public static void StopBlendingTowards()
    {
        _blendTowardsIndex = -1;
        ActivePoolForSnapshots.ApplyCurrentBlend();
        SmoothVariationBlending.Stop();
    }
        
    public static void UpdateBlendValues(int obj, float value)
    {
        //Log.Warning($"BlendValuesUpdate {obj} not implemented");
    }

    public static void SaveSnapshotAtNextFreeSlot(int obj)
    {
        //Log.Warning($"SaveSnapshotAtNextFreeSlot {obj} not implemented");
    }

    private const int AutoIndex=-1;
    public static Variation CreateOrUpdateSnapshotVariation(int activationIndex = AutoIndex )
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
            
        newVariation.PosOnCanvas = VariationBaseCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForSnapshots.Variations);
        if (activationIndex != AutoIndex)
            newVariation.ActivationIndex = activationIndex;

        newVariation.State = Variation.States.Active;
        ActivePoolForSnapshots.SaveVariationsToFile();
        return newVariation;
    }
        
    // TODO: Implement undo/redo!
    public static void RemoveInstancesFromVariations(List<Instance> instances, List<Variation> variations)
    {
            
        if (ActivePoolForSnapshots == null || ActiveInstanceForSnapshots == null)
        {
            return;
        }

        foreach (var variation in variations)
        {
            foreach (var instance in instances)
            {
                if (!variation.ParameterSetsForChildIds.ContainsKey(instance.SymbolChildId))
                    continue;

                variation.ParameterSetsForChildIds.Remove(instance.SymbolChildId);
            }
        }
        ActivePoolForSnapshots.SaveVariationsToFile();
    }

    private static void AddSnapshotEnabledChildrenToList(Instance instance, List<Instance> list)
    {
        var compositionUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
        foreach (var childInstance in instance.Children)
        {
            var symbolChildUi = compositionUi.ChildUis.SingleOrDefault(cui => cui.Id == childInstance.SymbolChildId);
            Debug.Assert(symbolChildUi != null);
                
            if (symbolChildUi.SnapshotGroupIndex == 0)
                continue;

            list.Add(childInstance);
        }
    }

    private static IEnumerable<Instance> GetSnapshotEnabledChildren(Instance instance)
    {
        var compositionUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
        foreach (var childInstance in instance.Children)
        {
            var symbolChildUi = compositionUi.ChildUis.SingleOrDefault(cui => cui.Id == childInstance.SymbolChildId);
            Debug.Assert(symbolChildUi != null);
                
            if (symbolChildUi.SnapshotGroupIndex == 0)
                continue;

            yield return childInstance;
        }
    }
        
    private static readonly List<Instance> _affectedInstances = new(100);
}