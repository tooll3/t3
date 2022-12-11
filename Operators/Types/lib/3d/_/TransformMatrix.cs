using System;
using SharpDX;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_17324ce1_8920_4653_ac67_c211ad507a81 
{
    public class TransformMatrix : Instance<TransformMatrix>
    {
        [Output(Guid = "751E97DE-C418-48C7-823E-D4660073A559")]
        public readonly Slot<SharpDX.Vector4[]> Result = new Slot<SharpDX.Vector4[]>();
        

        [Output(Guid = "ECA8121B-2A7F-4ECC-9143-556DCF78BA33")]
        public readonly Slot<SharpDX.Vector4[]> ResultInverted = new Slot<SharpDX.Vector4[]>();
        
        public TransformMatrix()
        {
            Result.UpdateAction = Update;
            ResultInverted.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            float yaw = MathUtil.DegreesToRadians(r.Y);
            float pitch = MathUtil.DegreesToRadians(r.X);
            float roll = MathUtil.DegreesToRadians(r.Z);
            var t = Translation.GetValue(context);
            var objectToParentObject = Matrix.Transformation(scalingCenter: Vector3.Zero, scalingRotation: Quaternion.Identity, scaling: new Vector3(s.X, s.Y, s.Z), rotationCenter: Vector3.Zero,
                                                             rotation: Quaternion.RotationYawPitchRoll(yaw, pitch, roll), translation: new Vector3(t.X, t.Y, t.Z));
            
            // transpose all as mem layout in hlsl constant buffer is row based
            objectToParentObject.Transpose();
            
            if (Invert.GetValue(context))
            {
                objectToParentObject.Invert(); 
            }
            
            _matrix[0] = objectToParentObject.Row1;
            _matrix[1] = objectToParentObject.Row2;
            _matrix[2] = objectToParentObject.Row3;
            _matrix[3] = objectToParentObject.Row4;
            Result.Value = _matrix;

            var invertedMatrix = Matrix.Invert(objectToParentObject);
            
            _invertedMatrix[0] = invertedMatrix.Row1;
            _invertedMatrix[1] = invertedMatrix.Row2;
            _invertedMatrix[2] = invertedMatrix.Row3;
            _invertedMatrix[3] = invertedMatrix.Row4;
            ResultInverted.Value = _invertedMatrix;
            
        }

        private SharpDX.Vector4[] _matrix = new SharpDX.Vector4[4];
        private SharpDX.Vector4[] _invertedMatrix = new SharpDX.Vector4[4];
        
        
        
        [Input(Guid = "3B817E6C-F532-4A8C-A2FF-A00DC926EEB2")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "5339862D-5A18-4D0C-B908-9277F5997563")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "58B9DFB6-0596-4F0D-BAF6-7FB3AE426C94")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "566F1619-1DE0-4B41-B167-7FC261730D62")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();
        
        [Input(Guid = "E19808D8-6D73-4638-B5F2-DDDDC49AD815")]
        public readonly InputSlot<bool> Invert = new InputSlot<bool>();        
    }
}
