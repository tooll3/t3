using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using T3.Gui.Interaction.PresetSystem.InputCommands;
using T3.Gui.Interaction.PresetSystem.Model;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public class ApcMiniDevice : MidiDevice
    {
        public ApcMiniDevice()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>()
                                             {
                                                 new CommandTriggerCombination(new[] { Shift, SceneTrigger1To64 }, typeof(SavePresetCommand), this),
                                                 new CommandTriggerCombination(new[] { SceneTrigger1To64 }, typeof(ApplyPresetCommand), this),
                                                 new CommandTriggerCombination(new[] { ChannelButtons1To8 }, typeof(ActivateGroupCommand), this),
                                             };
        }

        public override void Update(PresetSystem presetSystem, MidiIn midiIn, PresetContext context)
        {
            base.Update(presetSystem, midiIn, context);
            if (context == null)
                return;

            var midiOut = MidiOutConnectionManager.GetConnectedController(_productNameHash);
            if (midiOut == null)
                return;

            UpdatePresetLeds(midiOut, context);
            UpdateGroupLeds(midiOut, context);
        }

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }
        

        private void UpdatePresetLeds(MidiOut midiOut, PresetContext config)
        {
            var pageOffset = _pageIndex * PagePresetCount;

            for (var index = 0; index < PagePresetCount; index++)
            {
                var apcButtonRow = index / PresetColumns + 1;
                var apcButtonColumn = index % PresetColumns;
                var apcButtonIndex = apcButtonRow * PresetColumns + apcButtonColumn;

                var presetIndex = index + pageOffset;
                var isCurrentIndex = presetIndex == _currentPresetIndex;
                var address = new PresetAddress(apcButtonColumn, apcButtonRow);
                var p = config.TryGetPresetAt(address);

                var isValid = p != null;
                if (isValid)
                {
                    var colorForComplete = true ? ApcButtonColor.Green : ApcButtonColor.Yellow;
                    var colorForPlaceholders = p.IsPlaceholder ? ApcButtonColor.Yellow : colorForComplete;
                    SendColor(midiOut, apcButtonIndex, colorForPlaceholders);
                }
                else
                {
                    var colorForEmpty = isCurrentIndex ? ApcButtonColor.RedBlinking : ApcButtonColor.Off;
                    SendColor(midiOut, apcButtonIndex, colorForEmpty);
                }
            }
        }

        private void UpdateGroupLeds(MidiOut midiOut, PresetContext config)
        {
            foreach(var buttonIndex in ChannelButtons1To8.Indices())
            {
                var mappedIndex = ChannelButtons1To8.GetMappedIndex(buttonIndex);
                var g = config.GetGroupAtIndex(mappedIndex);
                
                var isUndefined = g == null;
                var color = isUndefined
                                ? ApcButtonColor.Off
                                : g.Id == config.ActiveGroupId
                                    ? ApcButtonColor.Red
                                    : ApcButtonColor.Off;
                
                SendColor(midiOut, buttonIndex, color);
            }
        }

        private static void SendColor(MidiOut midiOut, int apcControlIndex, ApcButtonColor colorCode)
        {
            if (CacheControllerColors[apcControlIndex] == (int)colorCode)
                 return;

            const int defaultChannel=1; 
            var noteOnEvent = new NoteOnEvent(0, defaultChannel, apcControlIndex, (int)colorCode, 50);
            midiOut.Send(noteOnEvent.GetAsShortMessage());

            //Previous implementation from T2
            //midiOut.Send(MidiMessage.StartNote(apcControlIndex, (int)colorCode, 1).RawData);
            //midiOut.Send(MidiMessage.StopNote(apcControlIndex, 0, 1).RawData);
            CacheControllerColors[apcControlIndex] = (int)colorCode;
        }
        
        private static readonly int[] CacheControllerColors = Enumerable.Repeat((int)ApcButtonColor.Undefined, 256).ToArray();// new ApcButtonColor[256];

        private static readonly int _currentPresetIndex = 0;
        private static readonly int _pageIndex = 0;

        private const int PresetRows = 7;
        private const int PresetColumns = 8;
        private const int PagePresetCount = PresetColumns * PresetRows;

        private readonly int _productNameHash = "APC MINI".GetHashCode();

        private enum ApcButtonColor
        {
            Undefined = -1,
            Off,
            Green,
            GreenBlinking,
            Red,
            RedBlinking,
            Yellow,
            YellowBlinking,
        }

        private static readonly ButtonRange SceneTrigger1To64 = new ButtonRange(0, 63);
        private static readonly ButtonRange Sliders1To9 = new ButtonRange(48, 48 + 8);

        private static readonly ButtonRange ButtonUp = new ButtonRange(64);
        private static readonly ButtonRange ButtonDown = new ButtonRange(65);
        private static readonly ButtonRange ButtonLeft = new ButtonRange(66);
        private static readonly ButtonRange ButtonRight = new ButtonRange(67);
        private static readonly ButtonRange ButtonVolume = new ButtonRange(68);
        private static readonly ButtonRange ButtonPan = new ButtonRange(69);
        private static readonly ButtonRange ButtonSend = new ButtonRange(70);
        private static readonly ButtonRange ButtonDevice = new ButtonRange(71);
        
        private static readonly ButtonRange ChannelButtons1To8 = new ButtonRange(64,71);

        private static readonly ButtonRange SceneLaunch1ClipStop = new ButtonRange(82);
        private static readonly ButtonRange SceneLaunch2ClipStop = new ButtonRange(83);
        private static readonly ButtonRange SceneLaunch3ClipStop = new ButtonRange(84);
        private static readonly ButtonRange SceneLaunch4ClipStop = new ButtonRange(85);
        private static readonly ButtonRange SceneLaunch5ClipStop = new ButtonRange(86);
        private static readonly ButtonRange SceneLaunch6ClipStop = new ButtonRange(87);
        private static readonly ButtonRange SceneLaunch7ClipStop = new ButtonRange(88);
        private static readonly ButtonRange SceneLaunch8ClipStop = new ButtonRange(89);

        private static readonly ButtonRange Shift = new ButtonRange(98);
    }
}