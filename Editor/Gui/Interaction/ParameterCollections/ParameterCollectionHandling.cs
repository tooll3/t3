using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Midi;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Interaction.ParameterCollections;

/// <summary>
/// Implements the interaction for adding, removing and activating ParameterCollections.
/// </summary>
/// <remarks>
/// We assume the following data structure
///
/// A SymbolUi has...
/// - ParameterCollection[] ->
///   - with CollectionParameter[] - defining their order and properties
///   - with "Scenes" (Variations) - defining their values for activation
/// 
/// ⚠ having this in the UI only will prevent blending to be used in demos.
/// 
/// </remarks>
public static class ParameterCollectionHandling
{
    private static Instance _activeCompositionInstance;
    private static Instance _singleSelectedInstance;

    public static void Update()
    {
        _singleSelectedInstance = NodeSelection.GetSelectedInstance();
        
        // Sync with composition selected in UI
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        if (primaryGraphWindow == null)
            return;
        
        _activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
    }
    
    public static void AddParameterToNewOrActiveCollection(SymbolUi compositionUi, IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input)
    {
        ParameterCollection c;

        if (compositionUi.ParamCollections.Count == 0)
        {
            c = new ParameterCollection();
            compositionUi.ParamCollections.Add(c);
        }
        else
        {
            c = compositionUi.ParamCollections[0];
        }

        var newParameterDefinition = new ParameterCollection.ParamDefinition
                                         {
                                             CompositionId = compositionUi.Symbol.Id,
                                             SymbolChildId = symbolChildUi.Id,
                                             InputId = inputSlot.Input.InputDefinition.Id,
                                             RangeMin = 0,
                                             RangeMax = 1, // TODO: implement
                                             DefaultValue = 0,
                                             Title = null // TODO: Clarify: initialize with name of blank to allow overriding vs. input name
                                         };
        c.ParameterDefinitions.Add(newParameterDefinition);
    }

    public static bool TryGetActiveCollections(out ParameterCollection symbolUiParamCollection)
    {
        symbolUiParamCollection = null;
        
        if (_activeCompositionInstance == null)
        {
            Log.Warning("Can't apply controller change without composition");
            return false;
        }

        if (!SymbolUiRegistry.Entries.TryGetValue(_activeCompositionInstance.Symbol.Id, out var symbolUi))
        {
            Log.Warning("Can't get Ui for composition?");
            return false;
        }

        // TODO: Implement multiple collections
        if (symbolUi.ParamCollections.Count == 0)
        {
            Log.Warning($"Composition {_activeCompositionInstance.Symbol} doesn't have parameter collections");
            return false;
        }
        
        symbolUiParamCollection = symbolUi.ParamCollections[0];
        return true;
    }

    public static bool TryGetCollectionForParameter(SymbolUi compositionUi, IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input,
                                                    out ParameterCollection collection)
    {
        foreach (var c in compositionUi.ParamCollections)
        {
            if (c.ParameterDefinitions.Any(def =>
                                               def.SymbolChildId == symbolChildUi.Id &&
                                               def.InputId == input.InputDefinition.Id))
            {
                collection = c;
                return true;
            }
        }

        collection = null;
        return false;
    }
    
    
    public static bool TryGetInputForParamDef(ParameterCollection.ParamDefinition paramDef, out IInputSlot input)
    {
        input = null;
        
        // Now apply typed value to parameter...
        var child = _activeCompositionInstance.Children.FirstOrDefault(child => child.SymbolChildId == paramDef.SymbolChildId);
        if (child == null)
        {
            Log.Debug($"Can't find matching child for collection parameter");
            return false;
        }

        input = child.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == paramDef.InputId);
        if (input == null)
        {
            Log.Debug($"Can't find matching input for collection parameter");
            return false;
        }

        return true;
    }
    
    public static void TryApplyControllerChange(int controllerIndex, float normalizedValue)
    {
        if (!TryGetActiveCollections(out var paramCollection))
            return;
        
        // TODO: Implement controller bank shifting to support more than 8 controllers
        if (controllerIndex >= paramCollection.ParameterDefinitions.Count )
        {
            Log.Debug($"No parameter on controller index {controllerIndex}");
            return;
        }

        var paramDef = paramCollection.ParameterDefinitions[controllerIndex];
        
        if (!TryGetInputForParamDef(paramDef, out var input))
            return;

        if (input is not InputSlot<float> floatInput)
        {
            Log.Debug($"Can't set non float parameter");
            return;
        }

        Log.Debug($"Setting {input} to {normalizedValue}");
        floatInput.SetTypedInputValue(normalizedValue);
    }


}