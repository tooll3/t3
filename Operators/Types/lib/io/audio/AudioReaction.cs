using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.Logging;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Log = T3.Core.Logging.Log;

namespace T3.Operators.Types.Id_03477b9a_860e_4887_81c3_5fe51621122c
{
    public class AudioReaction : Instance<AudioReaction>
    {
        [Output(Guid = "E1749C60-41F0-4E8F-9317-909EF31EEEF1", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Level = new();

        [Output(Guid = "8CA411D4-10CE-4B90-AEBA-3E405EBECBA3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> WasHit = new();
        
        [Output(Guid = "829fe194-4b8a-4d54-bde1-a0e023a9a198", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> HitCount = new();
        
        public AudioReaction()
        {
            Level.UpdateAction = Update;
            WasHit.UpdateAction = Update;
            HitCount.UpdateAction = Update;
        }

        private double _lastEvalTime;
        
        private void Update(EvaluationContext context)
        {
            if (context.IntVariables.TryGetValue("__MotionBlurPass", out var motionBlurPass))
            {
                if (motionBlurPass > 0)
                {
                    //Log.Debug($"Skip motion blur pass {motionBlurPass}");
                    return;
                }
            } 
            
            if (Math.Abs(context.LocalFxTime - _lastEvalTime) < 0.001f)
            {
                return;
            }

            _lastEvalTime = PlaybackTimeInSecs;
            var timeSinceLastHit = _lastEvalTime - _lastHitTime;
            if (timeSinceLastHit < -1)
            {
                _lastHitTime = timeSinceLastHit;
            }
            
            // if (!string.IsNullOrEmpty(context.Playback.Settings?.AudioInputDeviceName) && !WasapiAudioInput.DevicesInitialized)
            // {
            //     WasapiAudioInput.Initialize(context.Playback.Settings);
            // }

            if (MathUtils.WasTriggered(Reset.GetValue(context), ref _reset))
            {
                _hitCount = 0;
                AccumulatedLevel = 0;
            }


            var mode = (InputModes)InputBand.GetValue(context).Clamp(0, Enum.GetNames(typeof(InputModes)).Length - 1);

            var bins2 = mode switch
                            {
                                InputModes.RawFft                => AudioAnalysis.FftGainBuffer,
                                InputModes.NormalizedFft         => AudioAnalysis.FftNormalizedBuffer,
                                InputModes.FrequencyBands        => AudioAnalysis.FrequencyBands,
                                InputModes.FrequencyBandsPeaks   => AudioAnalysis.FrequencyBandPeaks,
                                InputModes.FrequencyBandsAttacks => AudioAnalysis.FrequencyBandAttacks,
                                _                                => null
                            };

            var bins = bins2 == null ? _emptyList : bins2.ToList();
            
            var threshold = Threshold.GetValue(context);
            var minTimeBetweenHits = MinTimeBetweenHits.GetValue(context);
            
            var windowCenter = WindowCenter.GetValue(context).Clamp(0, 1);
            var windowWidth = WindowWidth.GetValue(context).Clamp(0, 1);
            var windowEdge = WindowEdge.GetValue(context).Clamp(0.0001f, 1);
            var amplitude = Amplitude.GetValue(context);
            var bias = Bias.GetValue(context);

            var wasHit = false;
            
            Sum = 0f;
            if (bins != null && bins.Count > 0)
            {
                var bandCount = bins.Count;

                for (var binIndex = 0; binIndex < bins.Count; binIndex++)
                {
                    var binValue = bins[binIndex];
                    var f = (float)binIndex / (bandCount - 1);
                    var factor = 1 - (MathF.Abs((f - windowCenter) / windowEdge) - windowWidth / windowEdge).Clamp(0, 1);
                    Sum += binValue * factor;
                }

                Sum /= (bandCount * (windowWidth + windowEdge / 2));
                
                var couldBeHit = Sum > threshold;

                if (couldBeHit != _isHitActive)
                {
                    // changed this to >= minTimeBetweenHits to enable more steady beats
                    if (!_isHitActive && timeSinceLastHit >= minTimeBetweenHits)
                    {
                        wasHit = true;
                        _isHitActive = couldBeHit;
                        _lastHitTime = _lastEvalTime;
                        _hitCount++;
                    }
                    else
                    {
                        _isHitActive = false;
                        _isHitActive = couldBeHit;
                    }
                }

                AccumulatedLevel +=  MathF.Pow((Sum * 2) / threshold, 2) * 0.001 * amplitude;
                _dampedTimeBetweenHits = MathUtils.Lerp((float)timeSinceLastHit, _dampedTimeBetweenHits, 0.94f);
            }

            float v;
            
            AccumulationActive = false;
            
            switch ((OutputModes)Output.GetValue(context).Clamp(0, Enum.GetNames(typeof(OutputModes)).Length - 1))
            {
                case OutputModes.Pulse:
                    v = MathF.Pow((1 - (float)timeSinceLastHit).Clamp(0, 1), bias) * amplitude;
                    break;
                
                case OutputModes.TimeSinceHit:
                    v = MathF.Pow((float)timeSinceLastHit, bias) * amplitude;
                    break;
                
                case OutputModes.Count:
                    v = _hitCount * amplitude;
                    break;
                
                case OutputModes.Level:
                    v = MathF.Pow(Sum, bias) * amplitude;
                    break;
                
                case OutputModes.AccumulatedLevel:
                    AccumulationActive = true;
                    const double wrapToFloatPrecision = 10000;
                    v = (float)(AccumulatedLevel % wrapToFloatPrecision);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            
            WasHit.Value = wasHit;
            HitCount.Value = _hitCount;

            if (float.IsInfinity(v) || float.IsNaN(v))
            {
                v = 0;
            }
            Level.Value = v;
            ActiveBins = bins;
            
            // Only update once
            WasHit.DirtyFlag.Clear();
            HitCount.DirtyFlag.Clear();
            Level.DirtyFlag.Clear();
        }

        public float Sum { get; private set; }   
        public float SumFactor { get; private set; }
        private bool _reset;
        private int _hitCount;
        private bool _isHitActive;
        public double AccumulatedLevel { get; private set; }
        private float _dampedTimeBetweenHits;
        
        public List<float> ActiveBins { get; private set; }
        
        private double _lastHitTime;
        
        public enum InputModes
        {
            RawFft,
            NormalizedFft,
            FrequencyBands,
            FrequencyBandsPeaks,
            FrequencyBandsAttacks,
        }

        public enum OutputModes
        {
            Pulse,
            TimeSinceHit,
            Count,
            Level,
            AccumulatedLevel,
        }
        
        
        [Input(Guid = "44409811-1a0f-4be6-83ea-b2f040ebf08b", MappedType = typeof(InputModes))]
        public readonly InputSlot<int> InputBand = new();

        [Input(Guid = "F3D7C7FD-4280-4FB4-9F9A-C39B28D1A72B")]
        public readonly InputSlot<float> WindowCenter = new();

        [Input(Guid = "F920A79D-C946-4A78-9E68-F64FC3C4A696")]
        public readonly InputSlot<float> WindowEdge = new();
        
        [Input(Guid = "1D3A1132-E8B9-4C04-99BF-80F6F1530309")]
        public readonly InputSlot<float> WindowWidth = new();
        
        [Input(Guid = "02F71A92-D5C8-4DD7-AF5F-DA12330F60EB")]
        public readonly InputSlot<float> Threshold = new();

        [Input(Guid = "AECE14B8-88E0-4817-8204-B79159DB8102")]
        public readonly InputSlot<float> MinTimeBetweenHits = new();
        
        
        [Input(Guid = "80294096-AE64-471A-BCF1-622684E06D56", MappedType = typeof(OutputModes))]
        public readonly InputSlot<int> Output = new();
        
        [Input(Guid = "B1C43D3B-F425-4D62-9D48-07CCB5D08707")]
        public readonly InputSlot<float> Amplitude = new();

        [Input(Guid = "1139B245-76B4-4F51-A263-EE1A1371073F")]
        public readonly InputSlot<float> Bias = new();

        [Input(Guid = "D138C961-B163-428A-9479-2130F1E46314")]
        public readonly InputSlot<bool> Reset = new();
        
        private static readonly List<float> _emptyList = new();
        public double PlaybackTimeInSecs =>
            (Playback.Current.IsLive) ? Playback.RunTimeInSecs : Playback.Current.TimeInSecs;

        /// <summary>
        /// This is used only for visualization
        /// </summary>
        public double TimeSinceLastHit => _lastEvalTime - _lastHitTime;

        public bool AccumulationActive;
    }
}