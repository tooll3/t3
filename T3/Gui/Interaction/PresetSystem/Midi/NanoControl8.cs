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
                                                 new CommandTriggerCombination(new[] { ManagerSet, NanoButtonR1To8 }, typeof(SavePresetCommand), this),
                                                 new CommandTriggerCombination(new[] { NanoButtonR1To8 }, typeof(ApplyPresetCommand), this),
                                             };
        }

        private static readonly ButtonRange ButtonRewind = new ButtonRange(43);
        private static readonly ButtonRange ButtonFastForward = new ButtonRange(44);
        private static readonly ButtonRange ButtonStop = new ButtonRange(42);
        private static readonly ButtonRange ButtonPlay = new ButtonRange(41);
        private static readonly ButtonRange ButtonRecord = new ButtonRange(45);
        private static readonly ButtonRange ButtonLoop = new ButtonRange(46);
        private static readonly ButtonRange TextPrevious = new ButtonRange(58);
        private static readonly ButtonRange TrackNext = new ButtonRange(59);
        private static readonly ButtonRange ManagerSet = new ButtonRange(60);
        private static readonly ButtonRange ManagerNext = new ButtonRange(61);
        private static readonly ButtonRange ManagerPrevious = new ButtonRange(62);

        private static readonly ButtonRange NanoButtonSlider1To8 = new ButtonRange(0, 0 + 7);
        private static readonly ButtonRange NanoButtonKnob1To8 = new ButtonRange(16, 16 + 7);
        private static readonly ButtonRange NanoButtonSolo1To8 = new ButtonRange(32, 32 + 7);
        private static readonly ButtonRange NanoButtonMute1To8 = new ButtonRange(48, 48 + 7);
        private static readonly ButtonRange NanoButtonR1To8 = new ButtonRange(64, 64 + 7);

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