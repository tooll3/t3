using System.Collections.Generic;
using NAudio.Midi;
using T3.Gui.Interaction.PresetSystem.InputCommands;
using T3.Gui.Interaction.PresetSystem.Model;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public class ApcMini : AbstractMidiDevice
    {
        public ApcMini()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>
                                             {
                                                 new CommandTriggerCombination(typeof(ApplyPresetCommand), InputModes.Default, new[] { SceneTrigger1To64 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                                 new CommandTriggerCombination(typeof(SavePresetCommand), InputModes.Save, new[] { SceneTrigger1To64 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                                 new CommandTriggerCombination(typeof(RemovePresetCommand), InputModes.Delete, new[] { SceneTrigger1To64 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                                 
                                                 new CommandTriggerCombination(typeof(ActivateGroupCommand), InputModes.Default, new[] { ChannelButtons1To8 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                             };
            
            ModeButtons = new List<ModeButton>
                              {
                                  new ModeButton(Shift, InputModes.Save),
                                  new ModeButton(SceneLaunch1ClipStop, InputModes.Delete),
                              };
        }


        
        public override void Update(PresetSystem presetSystem, MidiIn midiIn, CompositionContext context)
        {
            _updateCount++;
            base.Update(presetSystem, midiIn, context);
            if (context == null)
                return;

            var midiOut = MidiOutConnectionManager.GetConnectedController(_productNameHash);
            if (midiOut == null)
                return;
            
            UpdateRangeLeds(midiOut, SceneTrigger1To64,
                            mappedIndex =>
                            {
                                var address = context.GetAddressFromButtonIndex(mappedIndex);
                                var p = context.TryGetPresetAt(address);
                                var color = ApcButtonColor.Off;
                                if (p == null)
                                    return (int)color;

                                switch (p.State)
                                {
                                    case Preset.States.Undefined:
                                        color = ApcButtonColor.Off;
                                        break;
                                    case Preset.States.InActive:
                                        color = ApcButtonColor.Green;
                                        break;
                                    case Preset.States.Active:
                                        color = ApcButtonColor.Red;
                                        break;
                                    case Preset.States.Modified:
                                        color = ApcButtonColor.YellowBlinking;
                                        break;
                                }

                                return WarningColor(mappedIndex, (int)color);
                            });

            UpdateRangeLeds(midiOut, ChannelButtons1To8,
                            mappedIndex =>
                            {
                                var g = context.GetGroupAtIndex(mappedIndex);
                                var isUndefined = g == null;
                                var color1 = isUndefined
                                                 ? ApcButtonColor.Off
                                                 : g.Id == context.ActiveGroupId
                                                     ? ApcButtonColor.Red
                                                     : ApcButtonColor.Off;
                                return (int)color1;
                            });

            // UpdateRangeLeds(midiOut, SceneLaunch1To8,
            //                 mappedIndex =>
            //                 {
            //                     var g1 = context.GetGroupAtIndex(mappedIndex);
            //                     var isUndefined1 = g1 == null;
            //                     var color2 = isUndefined1
            //                                      ? ApcButtonColor.Off
            //                                      : g1.Id == context.ActiveGroupId
            //                                          ? ApcButtonColor.Red
            //                                          : ApcButtonColor.Off;
            //                     return (int)color2;
            //                 });
        }

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }
        
        private int WarningColor(int index, int orgColor)
        {
            var indicatedStatus = (_updateCount + index / 8) % 30 < 4;
            if (!indicatedStatus)
            {
                return orgColor;
            }

            if (ActiveMode == InputModes.Save)
            {
                return (int)ApcButtonColor.Yellow;
            }
            else if (ActiveMode == InputModes.Delete)
            {
                return (int)ApcButtonColor.Red;
            }

            return orgColor;
        }

        private int _updateCount = 0;        
        
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

        private static readonly ButtonRange ChannelButtons1To8 = new ButtonRange(64, 71);
        private static readonly ButtonRange ButtonUp = new ButtonRange(64);
        private static readonly ButtonRange ButtonDown = new ButtonRange(65);
        private static readonly ButtonRange ButtonLeft = new ButtonRange(66);
        private static readonly ButtonRange ButtonRight = new ButtonRange(67);
        private static readonly ButtonRange ButtonVolume = new ButtonRange(68);
        private static readonly ButtonRange ButtonPan = new ButtonRange(69);
        private static readonly ButtonRange ButtonSend = new ButtonRange(70);
        private static readonly ButtonRange ButtonDevice = new ButtonRange(71);

        private static readonly ButtonRange SceneLaunch1To8 = new ButtonRange(82, 89);
        private static readonly ButtonRange SceneLaunch1ClipStop = new ButtonRange(82);
        private static readonly ButtonRange SceneLaunch2ClipSolo = new ButtonRange(83);
        private static readonly ButtonRange SceneLaunch3ClipRecArm = new ButtonRange(84);
        private static readonly ButtonRange SceneLaunch4ClipMute = new ButtonRange(85);
        private static readonly ButtonRange SceneLaunch5ClipSelect = new ButtonRange(86);
        private static readonly ButtonRange SceneLaunch8ClipStopAll = new ButtonRange(89);

        private static readonly ButtonRange Shift = new ButtonRange(98);
    }
}