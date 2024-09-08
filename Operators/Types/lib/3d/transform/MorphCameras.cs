using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_e3ff58e2_847a_4c97_947c_cfbcf8f9c79d
{
    public class MorphCameras : Instance<MorphCameras>
,IStatusProvider
    {
        [Output(Guid = "d0a6f926-c4ed-4cc9-917d-942f8c34fd65")]
        public readonly Slot<Command> Output = new();

        public MorphCameras()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                var cameraInputs = CameraReferences.GetCollectedTypedInputs();
                var cameraCount = cameraInputs.Count;

                var floatIndex = Index.GetValue(context).Clamp(0, cameraCount - 1.0001f);
                var index = (int)floatIndex;

                ICamera camA;
                ICamera camB;
                
                CameraReferences.DirtyFlag.Clear();
                if (cameraCount == 0)
                {
                    _lastErrorMessage = "No cameras connected?";
                    return;
                }

                if (cameraCount == 1)
                {
                    if (cameraInputs[0].GetValue(context) is ICamera cam)
                    {
                        camA = cam;
                        camB = cam;
                    }
                    else
                    {
                        _lastErrorMessage = "That's not a camera";
                        return;
                    }
                }
                else
                {
                    if (cameraInputs[index].GetValue(context) is ICamera camA_
                        && cameraInputs[index + 1].GetValue(context) is ICamera camB_)
                    {
                        _lastErrorMessage = null;
                        camA = camA_;
                        camB = camB_;
                    }
                    else
                    {
                        _lastErrorMessage = "Can't access cameras.";
                        return;
                    }
                }
                
                if (context.BypassCameras)
                {
                    Command.GetValue(context);
                    return;
                }

                var blend = floatIndex - index;
                var blendedCamDef = CameraDefinition.Blend(camA.CameraDefinition, camB.CameraDefinition, blend);

                blendedCamDef.BuildProjectionMatrices(out var camToClipSpace, out var worldToCamera);

                // Set properties and evaluate sub tree
                var prevWorldToCamera = context.WorldToCamera;
                var prevCameraToClipSpace = context.CameraToClipSpace;

                context.WorldToCamera = worldToCamera;
                context.CameraToClipSpace = camToClipSpace;

                Command.GetValue(context);

                context.CameraToClipSpace = prevCameraToClipSpace;
                context.WorldToCamera = prevWorldToCamera;
            }
            catch (Exception e)
            {
                _lastErrorMessage = "Didn't work " + e.Message;
            }
        }


        [Input(Guid = "C7EE5D97-86C1-442F-91D0-B60E6CFE24C7")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "FF2ED90B-38BD-4BA8-AF07-23BE87EABCC3")]
        public readonly MultiInputSlot<Object> CameraReferences = new();

        [Input(Guid = "3B71FDBF-CB2D-45F1-84DD-7AC66763E6AE")]
        public readonly InputSlot<float> Index = new();

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _lastErrorMessage;
        }

        private string _lastErrorMessage;
    }
}