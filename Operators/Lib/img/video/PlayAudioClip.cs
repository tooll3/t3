using System.Runtime.InteropServices;
using System;
using System.IO;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Audio;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;

namespace Lib.img.video
{
    [Guid("c2b2758a-5b3e-465a-87b7-c6a13d3fba48")]
    public class PlayAudioClip : Instance<PlayAudioClip>, IStatusProvider
    {
        [Output(Guid = "93B2B489-A522-439E-AC9E-8D47C073D721", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Command> Result = new();
        
        
        public PlayAudioClip()
        {
            Result.UpdateAction = Update;
        }
            
        private void Update(EvaluationContext context)
        {
            var url = Path.GetValue(context);
            var isTimeDirty = TimeInSecs.DirtyFlag.IsDirty;
            var timeParameter = TimeInSecs.GetValue(context);
            
            if (!TimeInSecs.IsConnected && isTimeDirty)
            {
                _startRunTimeInSecs = Playback.RunTimeInSecs - timeParameter;
            }
                      
            // TODO: This is currently disabled until we figure out how to provide symbol package for constructor.
            
            // if(_audioClip == null || _audioClip.FilePath != url)
            // {
            //     if(!File.Exists(url))
            //     {
            //         _errorMessageForStatus = $"File not found: {url}";
            //         return;
            //     }
            //     
            //     _audioClip = new AudioClip
            //                      {
            //                          FilePath = url,
            //                          StartTime = 0,
            //                          EndTime = 0,
            //                          IsSoundtrack = true,
            //                          LengthInSeconds = 0,
            //                          Volume = Volume.GetValue(context),
            //                          Id = Guid.NewGuid(),
            //                      };
            //     _startRunTimeInSecs = Playback.RunTimeInSecs;
            //     _errorMessageForStatus = null;
            // }

            if (_audioClip != null && IsPlaying.GetValue(context))
            {
                var targetTime = TimeInSecs.IsConnected
                                     ? timeParameter
                                     : Playback.RunTimeInSecs - _startRunTimeInSecs;
                
                if(IsLooping.GetValue(context) && targetTime > _audioClip.LengthInSeconds)
                {
                    targetTime %= _audioClip.LengthInSeconds;
                }
                
                //Log.Debug($" Playing at {targetTime:0.0}", this);
                AudioEngine.UseAudioClip(_audioClip,  targetTime);
                _audioClip.Volume =  Volume.GetValue(context);
            }
        }

        private double _startRunTimeInSecs;
        private AudioClip _audioClip;

        IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel()
        {
            return string.IsNullOrEmpty(_errorMessageForStatus) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        string IStatusProvider.GetStatusMessage()
        {
            return _errorMessageForStatus;
        }
        
        private string _errorMessageForStatus;

        
        [Input(Guid = "e0c927f9-5528-49c5-b457-5aea51f51628")]
        public readonly InputSlot<string> Path = new();
        

        [Input(Guid = "7ABB80B8-5EA3-48F3-9F28-19DACA9B8648")]
        public readonly InputSlot<float> TimeInSecs = new();

        [Input(Guid = "B8610A84-EA18-486C-8DBE-208C19A9DB28")]
        public readonly InputSlot<float> Volume = new();
        
        [Input(Guid = "86CF4C08-242D-4491-871E-EACCDB628238")]
        public readonly InputSlot<bool> IsPlaying = new();
        
        [Input(Guid = "989C84C4-F085-4839-89FD-BCF0F33A04E8")]
        public readonly InputSlot<bool> IsLooping = new();
    }
}