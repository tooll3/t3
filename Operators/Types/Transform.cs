using System;
using SharpDX;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_284d2183_197d_47fd_b130_873cced78b1c
{
    public class Transform : Instance<Transform>, ITransformable
    {
        [Output(Guid = "2D329133-29B9-4F56-B5A6-5FF7D83638FA", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        // implementation of ITransformable
        System.Numerics.Vector3 ITransformable.Translation { get => Translation.Value; set => Translation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => Rotation.Value; set => Rotation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Scale { get => Scale.Value; set => Scale.SetTypedInputValue(value); }
        public Action<ITransformable, EvaluationContext> TransformCallback { get; set; }

        
        public Transform()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
           
            TransformCallback?.Invoke(this, context);

            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            float yaw = MathUtil.DegreesToRadians(r.Y);
            float pitch = MathUtil.DegreesToRadians(r.X);
            float roll = MathUtil.DegreesToRadians(r.Z);
            var t = Translation.GetValue(context);
            var objectToParentObject = Matrix.Transformation(Vector3.Zero, Quaternion.Identity, new Vector3(s.X, s.Y, s.Z), Vector3.Zero,
                                                             Quaternion.RotationYawPitchRoll(yaw, pitch, roll), new Vector3(t.X, t.Y, t.Z));
            
            var previousWorldTobject = context.ObjectToWorld;
            context.ObjectToWorld = Matrix.Multiply(objectToParentObject, context.ObjectToWorld);
            Command.GetValue(context);
            context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "DCD066CE-AC44-4E76-85B3-78821245D9DC")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();
        
        [Input(Guid = "B4A8C16D-5A0F-4867-AE03-92A675ABE709")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "712ADB09-D249-4C91-86DB-3FEDF6B05971")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "DA4CD6C8-2307-45DA-9258-49C578025AA8")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "A7B1E667-BCE3-4E76-A5B1-0955C118D0FC")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();


    }
}