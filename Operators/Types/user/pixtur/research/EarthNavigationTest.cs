using System;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_b3f34926_e536_439b_b47b_2ab89a0bc94d 
{
    public class EarthNavigationTest : Instance<EarthNavigationTest>
    {
        [Output(Guid = "cab5c207-a997-4934-8c53-5a9f740284e0")]
        public readonly Slot<SharpDX.Vector4[]> Result = new Slot<SharpDX.Vector4[]>();
        
        public EarthNavigationTest()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {

            var activateControl = ActivateControl.GetValue(context);
            var controlsChanged = ForwardBackwards.DirtyFlag.IsDirty;
            
            if (controlsChanged)
            {
                ActivateControl.TypedInputValue.Value = true;
                ActivateControl.Input.IsDefault = false;
                ActivateControl.DirtyFlag.Invalidate();
            }

            if (controlsChanged || activateControl)
            {
                var forward = ForwardBackwards.GetValue(context);
                Longitude.TypedInputValue.Value += forward;
                Longitude.Input.IsDefault = false;
                Longitude.DirtyFlag.Invalidate();
            }
            
            var longitude = Longitude.GetValue(context);
            var latitude = Latitude.GetValue(context);
            var height = HeightAboveSurface.GetValue(context);
            var radius = EarthRadius.GetValue(context);
            
            var orientation = Orientation.GetValue(context);

            var m = Matrix.Identity;
            m *= Matrix.RotationY(-latitude / 180 * MathF.PI);
            m *= Matrix.RotationX(longitude / 180 * MathF.PI);
            
            m *= Matrix.RotationZ(orientation / 180 * MathF.PI);

            m *= Matrix.Translation(0,0,-height - radius);
            
            // var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            // var r = Rotation.GetValue(context);
            // var yaw = MathUtil.DegreesToRadians(r.Y);
            // var pitch = MathUtil.DegreesToRadians(r.X);
            // var roll = MathUtil.DegreesToRadians(r.Z);
            // var t = Translation.GetValue(context);
            // var objectToParentObject = Matrix.Transformation(scalingCenter: Vector3.Zero, scalingRotation: Quaternion.Identity, scaling: new Vector3(s.X, s.Y, s.Z), rotationCenter: Vector3.Zero,
            //                                                  rotation: Quaternion.RotationYawPitchRoll(yaw, pitch, roll), translation: new Vector3(t.X, t.Y, t.Z));
            
            // transpose all as mem layout in hlsl constant buffer is row based
            m.Transpose();
            
            _matrix[0] = m.Row1;
            _matrix[1] = m.Row2;
            _matrix[2] = m.Row3;
            _matrix[3] = m.Row4;
            Result.Value = _matrix;
            
        }

        private SharpDX.Vector4[] _matrix = new SharpDX.Vector4[4];
        
        
        
        [Input(Guid = "F5B2A750-9C2E-488F-90CD-9905E75382A7")]
        public readonly InputSlot<float> Longitude = new();

        [Input(Guid = "5CC61C18-E4D2-4042-8A30-06DC107D864F")]
        public readonly InputSlot<float> Latitude = new();

        [Input(Guid = "E1BB90F8-B9F2-441E-ACCA-C2CF5CA56258")]
        public readonly InputSlot<float> HeightAboveSurface = new();
        
        [Input(Guid = "E8015F0A-DF3D-4ACA-A1DC-B3ABD2473F9F")]
        public readonly InputSlot<float> Orientation = new();
        
        [Input(Guid = "97903119-6773-4819-A373-D32C03A946BD")]
        public readonly InputSlot<float> ForwardBackwards = new();
        
        [Input(Guid = "76E8A7F1-F24C-451E-967E-01D6C40D19D7")]
        public readonly InputSlot<float> RightLeft = new();
        
        [Input(Guid = "CFB98977-1F6A-4002-8BEE-EBCF0F9D68B2")]
        public readonly InputSlot<float> UpDown = new();
        
        [Input(Guid = "D2356A3F-030B-42B1-B699-3E4FACAD5BB4")]
        public readonly InputSlot<float> Spin = new();
        
        [Input(Guid = "9305E2F7-B51C-4A99-A462-948CEC25C4C0")]
        public readonly InputSlot<float> EarthRadius = new();

        
        [Input(Guid = "c0dbc42d-bf79-417b-af75-840611eba4c5")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "e6c462ce-52d1-4680-867d-6b1e52bb52cf")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "97c5c461-c3ee-4385-8057-1f4ec575d52b")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "d33bcd6a-b25f-4381-b342-93eb6da6eb68")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();
        
        [Input(Guid = "5f6d286d-8583-48ba-a9d2-4cc3af79052d")]
        public readonly InputSlot<bool> ActivateControl = new InputSlot<bool>();        
    }
}
