using System;
using System.Collections.Generic;
using NAudio.Midi;
using T3.Core.Operator;
using T3.Gui.Interaction.PresetSystem.Midi;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetControl
{
    public class PresetSystem
    {
        public PresetSystem()
        {
            // Scan for output devices (e.g. to update LEDs etc.)
            MidiOutConnectionManager.Init();
            
            // Get input devices
            _inputDevices = new List<IControllerInputDevice>()
                                {
                                    new NanoControl8(),
                                    new ApcMiniDevice(),
                                };
            
            // Adding dummy configuration
            _presetConfigurationForCompositions[Guid.Empty] = new PresetConfiguration();
        }
        
        public void Update()
        {
            foreach (var inputDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(inputDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;
                
                inputDevice.Update(this, midiIn, ConfigForActiveComposition);
            }
        }
        
        
        public void InitializeForComposition(Guid symbolId)
        {
            _presetConfigurationForCompositions[symbolId] = new PresetConfiguration(); // TODO: this should be deserialized
        }

        public PresetConfiguration ConfigForActiveComposition
        {
            get
            {
                _presetConfigurationForCompositions.TryGetValue(_activeCompositionId, out var config);
                return config;
            }
        }

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private Dictionary<Guid, PresetConfiguration> _presetConfigurationForCompositions =
            new Dictionary<Guid, PresetConfiguration>();

        public Instance ActiveComposition;
    }
    
    public interface IControllerInputDevice
    {
        void Update(PresetSystem presetSystem, MidiIn midiIn, PresetConfiguration config);
        int GetProductNameHash();
    }

}