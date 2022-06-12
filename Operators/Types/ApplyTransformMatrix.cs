using System;
using SharpDX;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3dae14a8_3d0b_432f_b951_bdd7afd7e5f8
{
    public class ApplyTransformMatrix : Instance<ApplyTransformMatrix>
    {
        [Output(Guid = "bd6cd982-c99e-4366-906a-2f0280cf32de")]
        public readonly Slot<Command> Output = new Slot<Command>();
        
        public ApplyTransformMatrix()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
           

            // var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            // var r = Rotation.GetValue(context);
            // float yaw = MathUtil.DegreesToRadians(r.Y);
            // float pitch = MathUtil.DegreesToRadians(r.X);
            // float roll = MathUtil.DegreesToRadians(r.Z);
            // var t = Translation.GetValue(context);
            // var objectToParentObject = Matrix.Transformation(scalingCenter: Vector3.Zero, scalingRotation: Quaternion.Identity, scaling: new Vector3(s.X, s.Y, s.Z), rotationCenter: Vector3.Zero,
            //                                                  rotation: Quaternion.RotationYawPitchRoll(yaw, pitch, roll), translation: new Vector3(t.X, t.Y, t.Z));

            var r = MatrixRows.GetValue(context);
            var transformMatrix = new Matrix(r[0].X, r[0].Y, r[0].Z, r[0].W,
                                             r[1].X, r[1].Y, r[1].Z, r[1].W,
                                             r[2].X, r[2].Y, r[2].Z, r[2].W,
                                             r[3].X, r[3].Y, r[3].Z, r[3].W);
            
            var previousWorldTobject = context.ObjectToWorld;
            context.ObjectToWorld = Matrix.Multiply(transformMatrix, context.ObjectToWorld);
            Command.GetValue(context);
            context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "aad90ca4-5085-4b90-bf58-b87bdc852d62")]
        public readonly InputSlot<Command> Command = new();
        
        // [Input(Guid = "69e8942b-9a61-401f-974a-74ae788c7870")]
        // public readonly InputSlot<System.Numerics.Vector3> Translation = new();
        //
        // [Input(Guid = "ca2b74e5-cd9b-4a09-99ed-ca4f85b829c4")]
        // public readonly InputSlot<System.Numerics.Vector3> Rotation = new();
        //
        // [Input(Guid = "c6f953a6-1263-459c-8e0c-ca22f18ea3a7")]
        // public readonly InputSlot<System.Numerics.Vector3> Scale = new();
        //
        // [Input(Guid = "9492a7d1-92cc-456f-9751-2cda4c89e8ea")]
        // public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "86FA7A95-C6DF-4BB8-A447-E92882A99037")]
        public readonly InputSlot<SharpDX.Vector4[]> MatrixRows = new();



    }
}