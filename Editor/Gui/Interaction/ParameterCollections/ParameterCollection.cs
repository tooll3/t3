using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Serialization;
// using String = System.String;
// ReSharper disable MemberCanBePrivate.Global

namespace T3.Editor.Gui.Interaction.ParameterCollections;

/**
Allow manipulation multiple parameters across different SymbolChildren, especially for live performances with MidiControllers.
Although similar to SnapShots this method allows finer control and setup.

We assume the following data structure

A SymbolUi has...
- ParameterCollection[] ->
  - with CollectionParameter[] - defining their order and properties
  - with "Scenes" (Variations) - defining their values for activation

On the other hand it requires more time to setup and tweak.
- It uses Variations for serializing parameter configurations. Inconsistencies between Variation-Indices and order and
  properties stored for undefined control parameters need to be resolve by calling <see cref="ConformVariations"/>.
- ParameterGroups are serialized as part of the Playback settings (later to be renamed to project settings).
- ParameterGroups are created by Commands like Add, Modify, Delete.

They can be used by...
- applying or blending into one of their variations
- activating the group on a midi controller and manipulating individual parameters
- maybe later: applying or blending between variations by an operator.

Parameter manipulation by midi controllers or ops is likely to "break" undo/redo.

Todo:
- Create parameterGroups window or panel
    - List Groups
    - Create new Group Button
    - Assign parameter to index. Some Ideas:
        - Maybe drag and drop from control to parameter name (Pick whip?)
        - Draw and drop from parameter name to control button?
        - Context Menu of current parameter op?
        - Context menu at parameter window?
        - Special hotkey when clicking on parameter name
    - Draw Parameter List for selected group
- Add parameter to group by context menu in parameter window.
- Connect to midi connection manager
    - somehow switch between snapshot and parameter control modes
      - Make ParameterGroups be part of VariationHandling
      - Add new VariationHandling.Mode property: Preset|Snapshot|ParamGroups
      - Update .Mode from PresetWindow
      -
    - add commands to CompatibleMidiDevice
    - indicate parameters as controlled on midi device (e.g. by activating LEDs)
    ✔ only register MidiOutConnections for compatible connected midi-devices

Needs clarification:
- ⚠ having this in the UI only will prevent blending to be used in demos.
- ⚠ Careful: Only scalar parameter are useful for collections, but Variations support many 
  other data types. This basically prevents use from using Variation Blending for vectors.
- If a SymbolChild is being removed, it's slot should be freed in the ParameterCollection.
- Should ParameterGroups be its own window (or a tab in variations)?
  Pro: Use-case might be different because it's mostly relevant for live performing
  - "Presets" is a good window title.
  Contra:
  - If there is a variation mode used by the midi-controller, it it would be awkward, if 1 out of 3 modes would be scattered
    across multiple windows. And then. How would you activate this mode?
  - Switching between modes should be possible on Midi-Controllers and should be consistently reflected on the UI.
*/
public class ParameterCollection
{
    public Guid Id = Guid.NewGuid();
    public string Title;

    /// <summary>
    /// Order and layout of control parameters. Empty slots needs to be filled with null.
    /// </summary>
    public List<ParamDefinition> ParameterDefinitions = new();

    public List<Variation> Variations = new();

    public class ParamDefinition
    {
        public Guid CompositionId; // Fixme: This is probably not required.
        public Guid SymbolChildId;
        public Guid InputId;
        public float RangeMin;
        public float RangeMax;
        public float DefaultValue;  // Fixme: Float type is to limited.
        public string Title;

        public ParamDefinition Clone()
        {
            return new ParamDefinition
                       {
                           CompositionId = CompositionId,
                           SymbolChildId = SymbolChildId,
                           InputId = InputId,
                           RangeMin = RangeMin,
                           RangeMax = RangeMax,
                           DefaultValue = DefaultValue,
                           Title = Title,
                       };
        }
    }

    public void ConformVariations()
    {
        //TODO: implement
    }

    #region serialization
    public static ParameterCollection FromJson(Guid symbolId, JToken jToken)
    {
        // if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var compositionSymbol))
        //     return null;
        //
        var idToken = jToken[nameof(Id)];

        var idString = idToken?.Value<string>();
        if (idString == null)
            return null;

        var newCollection = new ParameterCollection
                                {
                                    Id = Guid.Parse(idString),
                                    Title = jToken[nameof(Title)]?.Value<string>() ?? String.Empty,
                                    Variations = new List<Variation>(8),
                                };

        var variationsToken = (JArray)jToken[nameof(Variations)];
        if (variationsToken == null)
            return newCollection;

        foreach (var variationToken in variationsToken)
        {
            newCollection.Variations.Add(Variation.FromJson(symbolId, variationToken));
        }

        return newCollection;
    }

    public void ToJson(JsonTextWriter writer)
    {
        writer.WriteStartObject();
        {
            writer.WriteValue(nameof(Id), Id);
            if (!string.IsNullOrEmpty(Title))
            {
                writer.WriteObject(nameof(Title), Title);
            }

            writer.WritePropertyName(nameof(ParameterDefinitions));
            writer.WriteStartArray();
            {
                foreach (var d in ParameterDefinitions)
                {
                    writer.WriteStartObject();
                    writer.WriteValue(nameof(d.SymbolChildId), d.SymbolChildId);
                    writer.WriteValue(nameof(d.InputId), d.InputId);
                    
                    
                    
                    if (!string.IsNullOrEmpty(Title))
                    {
                        writer.WriteObject(nameof(Title), Title);
                    }
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            
            writer.WritePropertyName(nameof(Variations));
            writer.WriteStartArray();
            {
                foreach (var v in Variations)
                {
                    v.ToJson(writer);
                }
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
    #endregion

    public ParameterCollection Clone()
    {
        return
            new ParameterCollection
                {
                    Id = Id,
                    Title = Title,
                    ParameterDefinitions = ParameterDefinitions.Select(d => d.Clone()).ToList(),
                    Variations = Variations.Select(v => v.Clone()).ToList(),

                };
    }
}