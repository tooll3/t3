using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.InputCommands;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public class NanoControl8 : MidiDevice
    {
        public NanoControl8()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>()
                                             {
                                                 new CommandTriggerCombination(new[] { ManagerSet, NanoControlR1To8 }, typeof(SavePresetCommand), this),
                                                 new CommandTriggerCombination(new[] { NanoControlR1To8 }, typeof(ApplyPresetCommand), this),
                                             };
        }

        private static readonly ControllerRange ControlRewind = new ControllerRange(43);
        private static readonly ControllerRange ControlFastForward = new ControllerRange(44);
        private static readonly ControllerRange ControlStop = new ControllerRange(42);
        private static readonly ControllerRange ControlPlay = new ControllerRange(41);
        private static readonly ControllerRange ControlRecord = new ControllerRange(45);
        private static readonly ControllerRange ControlLoop = new ControllerRange(46);
        private static readonly ControllerRange TextPrevious = new ControllerRange(58);
        private static readonly ControllerRange TrackNext = new ControllerRange(59);
        private static readonly ControllerRange ManagerSet = new ControllerRange(60);
        private static readonly ControllerRange ManagerNext = new ControllerRange(61);
        private static readonly ControllerRange ManagerPrevious = new ControllerRange(62);

        private static readonly ControllerRange NanoControlSlider1To8 = new ControllerRange(0, 0 + 7);
        private static readonly ControllerRange NanoControlKnob1To8 = new ControllerRange(16, 16 + 7);
        private static readonly ControllerRange NanoControlSolo1To8 = new ControllerRange(32, 32 + 7);
        private static readonly ControllerRange NanoControlMute1To8 = new ControllerRange(48, 48 + 7);
        private static readonly ControllerRange NanoControlR1To8 = new ControllerRange(64, 64 + 7);

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }

        public override PresetConfiguration.PresetAddress GetAddressForIndex(int index)
        {
            return PresetConfiguration.PresetAddress.NotAnAddress;
        }

        private readonly int _productNameHash = "Korg NanoControl".GetHashCode(); //Todo: this needs the correct product name
    }
}