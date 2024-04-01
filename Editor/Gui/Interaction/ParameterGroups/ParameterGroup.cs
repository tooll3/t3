using System;
using System.Collections.Generic;
using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.Gui.Interaction.ParameterGroups;

/// <summary>
/// Allow manipulation multiple parameters across different symbolChildren, especially for live performances with MidiControllers.
/// </summary>
/// <remarks>
/// Although similar to SnapShots this method allows finer control and setup.
/// On the other hand it requires more time to setup and tweaks.
/// - It uses Variations for serializing parameter configurations. Inconsistencies between Variation-Indices and order and
///   properties stored for undefined control parameters need to be resolve by calling <see cref="ConformVariations"/>.
/// - ParameterGroups are serialized as part of the Playback settings (later to be renamed to project settings).
/// - ParameterGroups are created by Commands like Add, Modify, Delete.
///
/// They can be used by...
/// - applying or blending into one of their variations
/// - activating the group on a midi controller and manipulating individual parameters
/// - maybe later: applying or blending between variations by an operator.
///
/// Parameter manipulation by midi controllers or ops is likely to "break" undo/redo.
///
/// Todo:
/// - Create parameterGroups window
///     - List Groups
///     - Create new Group Button
///     - Add parameter control button
///     - draw Parameter List
/// - Connect to midi connection manager
///     - somehow switch between snapshot and parameter control modes
///       - Make ParameterGroups be part of VariationHandling
///       - Add new VariationHandling.Mode property: Preset|Snapshot|ParamGroups
///       - Update .Mode from PresetWindow
///       - 
///     - add commands to CompatibleMidiDevice
///     - indicate parameters as controlled on midi device (e.g. by activating LEDs)
///     - only register MidiOutConnections for compatible connected midi-devices
/// 
/// Needs clarification:
/// - Should ParameterGroups be its own window (or a tab in variations)?
///   Pro: Use-case might be different because it's mostly relevant for live performing
///   - "Presets" is a good window title.
///   Contra:
///   - If there is a variation mode used by the midi-controller, it it would be awkward, if 1 out of 3 modes would be scattered
///     across multiple windows. And then. How would you activate this mode?
///   - Switching between modes should be possible on Midi-Controllers and should be consistently reflected on the UI.
/// </remarks>
///
/// 
public class ParameterGroup
{
    public Guid Id;
    public string Name;

    /// <summary>
    /// Order and layout of control parameters. Empty slots needs to be filled with null.
    /// </summary>
    public List<GroupParameter> ParameterDefinitions = new();

    public List<Variation> Variations = new();
    
    public class GroupParameter
    {
        public Guid CompositionId;
        public Guid SymbolChildId;
        public Guid ParameterId;
        public float RangeMin;
        public float RangeMax;
        public float DefaultValue;
        public string Name;
    }

    public void ConformVariations()
    {
        //TODO: implement
    }
    
}