using System;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6
{
    public class Camera : Instance<Camera>, ICamera
    {
        [Output(Guid = "2E1742D8-9BA3-4236-A0CD-A2B02C9F5924", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        public Camera()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            float fov = MathUtil.DegreesToRadians(Fov.GetValue(context));
            float aspectRatio = AspectRatio.GetValue(context);
            if (aspectRatio < 0.0001f)
            {
                aspectRatio = (float)context.RequestedResolution.Width / context.RequestedResolution.Height;
            }
            System.Numerics.Vector2 clip = NearFarClip.GetValue(context);
            Matrix cameraToClipSpace = Matrix.PerspectiveFovRH(fov, aspectRatio, clip.X, clip.Y);

            var positionValue = Position.GetValue(context);
            Vector3 eye = new Vector3(positionValue.X, positionValue.Y, positionValue.Z);
            var targetValue = Target.GetValue(context);
            Vector3 target = new Vector3(targetValue.X, targetValue.Y, targetValue.Z);
            var upValue = Up.GetValue(context);
            Vector3 up = new Vector3(upValue.X, upValue.Y, upValue.Z);
            Matrix worldToCamera = Matrix.LookAtRH(eye, target, up);

            //var worldToCamera = Matrix.LookAtLH(position, target, new Vector3(0, 1, 0));
            var rollRotation = Matrix.RotationAxis(new Vector3(0, 0, 1), -(float)Roll.GetValue(context));

            var pOffset = PositionOffset.GetValue(context);
            var additionalTranslation = Matrix.Translation(pOffset.X, pOffset.Y, pOffset.Z);

            var rOffset = RotationOffset.GetValue(context);
            var additionalRotation = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(rOffset.Y),
                                                                 MathUtil.DegreesToRadians(rOffset.X),
                                                                 MathUtil.DegreesToRadians(rOffset.Z));
            
            
            var worldToCamera2= worldToCamera * rollRotation * additionalTranslation * additionalRotation;
            
            var prevWorldToCamera = context.WorldToCamera;
            var prevCameraToClipSpace = context.CameraToClipSpace;
            
            context.WorldToCamera = worldToCamera2;
            context.CameraToClipSpace = cameraToClipSpace;
            
            Command.GetValue(context);
            
            context.CameraToClipSpace = prevCameraToClipSpace;
            context.WorldToCamera = prevWorldToCamera;
        }

        // Implement ICamera 
        public System.Numerics.Vector3 CameraPosition
        {
            get { return Position.Value;} 
            set { Animator.UpdateVector3InputValue(Position, value); }
        }

        public System.Numerics.Vector3 CameraTarget
        {
            get { return Target.Value;} 
            set { Animator.UpdateVector3InputValue(Target, value); }
        }

        public float CameraRoll
        {
            get { return Roll.Value;} 
            set { Animator.UpdateFloatInputValue(Roll, value); }

        }

        [Input(Guid = "047B8FAE-468C-48A7-8F3A-5FAC8DD5B3C6")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();
        [Input(Guid = "313596CC-3854-436B-89DA-5FD40164CE76")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();
        [Input(Guid = "A7ACB25C-D60C-43A6-B1DF-2CD5C6E183F3")]
        public readonly InputSlot<System.Numerics.Vector3> Target = new InputSlot<System.Numerics.Vector3>();
        [Input(Guid = "E6DFBFB9-EFED-4C17-8860-9C1A1CA2FA38")]
        public readonly InputSlot<System.Numerics.Vector3> Up = new InputSlot<System.Numerics.Vector3>();
        [Input(Guid = "F66E91A1-B991-48C3-A8C9-33BCAD0C2F6F")]
        public readonly InputSlot<float> AspectRatio = new InputSlot<float>();
        [Input(Guid = "199D4CE0-AAB1-403A-AD42-216EF1061A0E")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarClip = new InputSlot<System.Numerics.Vector2>();
        [Input(Guid = "7BDE5A5A-CE82-4903-92FF-14E540A605F0")]
        public readonly InputSlot<float> Fov = new InputSlot<float>();
        [Input(Guid = "764CA304-FC86-48A9-9C82-A04FAC7EADB2")]
        public readonly InputSlot<float> Roll = new InputSlot<float>();
        
        
        [Input(Guid = "FEE19916-846F-491A-A2EE-1E7B1AC8E533")]
        public readonly InputSlot<System.Numerics.Vector3> PositionOffset = new InputSlot<System.Numerics.Vector3>();

        
        [Input(Guid = "D4D0F046-297B-440A-AEF8-C2F0426EF4F5")]
        public readonly InputSlot<System.Numerics.Vector3> RotationOffset = new InputSlot<System.Numerics.Vector3>();
    }
}