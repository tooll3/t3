using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Gui.Interaction.PresetControl.Midi;

namespace T3.Gui.Interaction.PresetControl
{
    /*
    T3.UIFrameUpdate()
     ControlInterfaceManager.Update()
     - GetActiveComposition
     - each Interface
       Interface.Update(this) 
        UiGraphInterface.Update(Manager)
        - UpdateParameterVisualization
        - UpdatePresetStatusVisualization
        - CheckForCommands
           
           Command.Execute(Manager)
    */

    public class PresetSystem
    {
        public PresetSystem()
        {
            _inputDevices = new List<IControllerInputDevice>()
                                {
                                    new NanoControl8(),
                                };
        }
        
        public void Update()
        {
            foreach (var inputDevice in _inputDevices)
            {
                inputDevice.Update(this);
            }
        }

        private readonly List<IControllerInputDevice> _inputDevices;

        private Dictionary<Guid, PresetConfiguration> _presetConfigurationForCompositions =
            new Dictionary<Guid, PresetConfiguration>();

        public Instance ActiveComposition;
    }



    
    public interface IControllerInputDevice
    {
        void Update(PresetSystem manager);
    }

    public class PresetConfiguration
    {
    }
}