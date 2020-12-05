using System.Collections.Generic;
using NAudio.Midi;
using T3.Gui.Interaction.PresetControl;
using T3.Gui.Interaction.PresetSystem.InputCommands;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public class ApcMiniDevice : MidiDevice
    {
        public ApcMiniDevice()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>()
                                             {
                                                 new CommandTriggerCombination(new[] { ControlUp, SceneTrigger1To64 }, typeof(SavePresetCommand), this),
                                                 new CommandTriggerCombination(new[] { SceneTrigger1To64 }, typeof(ApplyPresetCommand), this),
                                             };
        }

        public override void Update(PresetControl.PresetSystem presetSystem, MidiIn midiIn, PresetConfiguration config)
        {
            base.Update(presetSystem, midiIn, config);

            var midiOut = MidiOutConnectionManager.GetConnectedController(_productNameHash);
            if (midiOut == null)
                return;

            UpdatePresetLeds(midiOut, config);
            UpdatePageLeds(midiOut);
        }

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }

        public override PresetConfiguration.PresetAddress GetAddressForIndex(int index)
        {
            return SceneTrigger1To64.IncludesIndex(index) 
                       ? new PresetConfiguration.PresetAddress(index% 8, index/8) 
                       : PresetConfiguration.PresetAddress.NotAnAddress;
        }

        private void UpdatePresetLeds(MidiOut midiOut, PresetConfiguration config)
        {
            var pageOffset = _pageIndex * PagePresetCount;

            for (var index = 0; index < PagePresetCount; index++)
            {
                var apcButtonRow = index / PresetColumns + 1;
                var apcButtonColumn = index % PresetColumns;
                var apcButtonIndex = apcButtonRow * PresetColumns + apcButtonColumn;

                var presetIndex = index + pageOffset;
                var isCurrentIndex = presetIndex == _currentPresetIndex;
                var address = new PresetConfiguration.PresetAddress(apcButtonColumn, apcButtonRow);
                var p = config.TryGetPreset(address);

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

        private void UpdatePageLeds(MidiOut midiOut)
        {
            for (var i = 0; i < PresetColumns; i++)
            {
                var isActivePresetInPage = i == _currentPresetIndex / PagePresetCount;
                var colorForInactivePage =
                    isActivePresetInPage ? ApcButtonColor.RedBlinking : ApcButtonColor.Off;

                var colorForActivePage = i == _pageIndex ? ApcButtonColor.Red : colorForInactivePage;
                SendColor(midiOut, i, colorForActivePage);
            }
        }

        private static void SendColor(MidiOut midiOut, int apcControlIndex, ApcButtonColor colorCode)
        {
            if (_cacheControllerColors[apcControlIndex] == colorCode)
                 return;
            
            //TODO: The midi connection is unreliable sometimes drops signals  
            midiOut.Send(MidiMessage.StartNote(apcControlIndex, (int)colorCode, 1).RawData);
            midiOut.Send(MidiMessage.ChangeControl(apcControlIndex, (int)colorCode, 1).RawData);    // A lame attempt to increase stability
            midiOut.Send(MidiMessage.ChangeControl(apcControlIndex, (int)colorCode, 1).RawData);
            midiOut.Send(MidiMessage.StopNote(apcControlIndex, 0, 1).RawData);
            _cacheControllerColors[apcControlIndex] = colorCode;
        }
        
        private static readonly ApcButtonColor[] _cacheControllerColors = new ApcButtonColor[256];

        private static readonly int _currentPresetIndex = 0;
        private static readonly int _pageIndex = 0;

        private const int PresetRows = 7;
        private const int PresetColumns = 8;
        private const int PagePresetCount = PresetColumns * PresetRows;

        private readonly int _productNameHash = "APC MINI".GetHashCode();

        private enum ApcButtonColor
        {
            Off,
            Green,
            GreenBlinking,
            Red,
            RedBlinking,
            Yellow,
            YellowBlinking,
        }

        private static readonly ControllerRange SceneTrigger1To64 = new ControllerRange(0, 63);
        private static readonly ControllerRange Sliders1To9 = new ControllerRange(48, 48 + 8);

        private static readonly ControllerRange ControlUp = new ControllerRange(64);
        private static readonly ControllerRange ControlDown = new ControllerRange(64);
        private static readonly ControllerRange ControlLeft = new ControllerRange(64);
        private static readonly ControllerRange ControlRight = new ControllerRange(64);
        private static readonly ControllerRange ControlVolume = new ControllerRange(64);
        private static readonly ControllerRange ControlPan = new ControllerRange(64);
        private static readonly ControllerRange ControlSend = new ControllerRange(64);
        private static readonly ControllerRange ControlDevice = new ControllerRange(64);

        private static readonly ControllerRange SceneLaunch1ClipStop = new ControllerRange(82);
        private static readonly ControllerRange SceneLaunch2ClipStop = new ControllerRange(83);
        private static readonly ControllerRange SceneLaunch3ClipStop = new ControllerRange(84);
        private static readonly ControllerRange SceneLaunch4ClipStop = new ControllerRange(85);
        private static readonly ControllerRange SceneLaunch5ClipStop = new ControllerRange(86);
        private static readonly ControllerRange SceneLaunch6ClipStop = new ControllerRange(87);
        private static readonly ControllerRange SceneLaunch7ClipStop = new ControllerRange(88);
        private static readonly ControllerRange SceneLaunch8ClipStop = new ControllerRange(89);

        private static readonly ControllerRange Shift = new ControllerRange(98);
    }
}