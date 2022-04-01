using System;
using System.Collections.Generic;
using System.Resources;
using Microsoft.Win32;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Vector4 = SharpDX.Vector4;

namespace T3.Operators.Types.Id_73d99108_f49a_48fb_aa5d_707c00abb1c2
{
    public class RepetitionPoints : Instance<RepetitionPoints>
    {
        [Output(Guid = "46c3b7f4-3590-46d7-871f-b98685f62c07")]
        public readonly Slot<StructuredList> ResultList = new();

        public RepetitionPoints()
        {
            ResultList.UpdateAction = Update;
            //_pointListWithSeparator.TypedElements[2] = Point.Separator();
        }
        
        private void Update(EvaluationContext context)
        {
            var startPosition = StartPosition.GetValue(context);
            var addSeparator = AddSeparator.GetValue(context);
            var startW = StartW.GetValue(context);
            
            var offset = Offset.GetValue(context);
            var translateStep = Translate.GetValue(context);
            var rotateStep = Rotate.GetValue(context);
            var scaleStep = Scale.GetValue(context);
            var pivotTemp = Pivot.GetValue(context);
            var pivot = new SharpDX.Vector3(pivotTemp.X, pivotTemp.Y, pivotTemp.Z);
            
            var count = Count.GetValue(context).Clamp(1,10000);
            var listCount = count + (addSeparator ? 1:0); 
            
            if (_pointList.NumElements != listCount)
            {
                //_points = new T3.Core.DataTypes.Point[count];
                _pointList.SetLength(listCount);
            }

            SharpDX.Vector3 startTranslation = new SharpDX.Vector3(startPosition.X, startPosition.Y, startPosition.Z);

            for (var i = 0; i < count; ++i) 
            {
                float u = (i + 1+ offset);
                        
                var translation = new SharpDX.Vector3(translateStep.X, translateStep.Y, translateStep.Z) * u + startTranslation;
                var rotation = SharpDX.Quaternion.RotationYawPitchRoll(    rotateStep.X / 360.0f * (float)(2.0 * Math.PI) * u,
                                                                           rotateStep.Y / 360.0f * (float)(2.0 * Math.PI) * u,
                                                                           rotateStep.Z / 360.0f * (float)(2.0 * Math.PI) * u);
                var scale = (SharpDX.Vector3.One - scaleStep) * u + SharpDX.Vector3.One;
                //var scale = Vector3.One;
                
                var transform = Matrix.Transformation(scalingCenter: SharpDX.Vector3.Zero, 
                                                      scalingRotation: SharpDX.Quaternion.Identity, 
                                                      scaling: scale, 
                                                      rotationCenter: pivot, 
                                                      rotation: rotation, 
                                                      translation: translation);
            
                //context.ObjectToWorld = transform * prevTransform;
                
                //var rot = Quaternion.CreateFromAxisAngle(new Vector3(0,1,0), (float)Math.Atan2(startPosition.X - to.X, startPosition.Y - to.Y) );
                SharpDX.Vector4 v = new SharpDX.Vector4(0, 0, 0, 1);
                SharpDX.Vector4.Transform(ref v, ref transform, out SharpDX.Vector4 pos);
            
                _pointList.TypedElements[i].Position = new Vector3(pos.X, pos.Y, pos.Z);
                _pointList.TypedElements[i].W = scale.Length() + startW;
                _pointList.TypedElements[i].Orientation =  new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            }
            
            if (addSeparator)
            {
                _pointList[listCount - 1] = Point.Separator();
            }

            ResultList.Value = _pointList;            
        }

        private readonly StructuredList<Point> _pointList = new(2);

        [Input(Guid = "81EA19E7-97DC-461D-8EB0-7E9091CC1FC3")]
        public readonly InputSlot<int> Count = new();

        
        [Input(Guid = "4213911b-4103-481f-85c6-9bccac116264")]
        public readonly InputSlot<Vector3> StartPosition = new();

        [Input(Guid = "3A1A829D-D273-4EC0-B327-30BE0E0463C4")]
        public readonly InputSlot<Vector3> Translate = new();

        // [Input(Guid = "F5B3C1BC-3A79-40F4-8007-6AC98C16A9AA")]
        // public readonly InputSlot<Vector3> Scale = new();

        [Input(Guid = "5956A2C7-ADDC-409A-87D8-62E67DA212F4")]
        public readonly InputSlot<float> Scale = new();

        
        [Input(Guid = "AF208E67-43BB-4C09-8076-859E7FEFA93F")]
        public readonly InputSlot<Vector3> Rotate = new();

        [Input(Guid = "AF3C87E3-889D-410F-B4BC-62BD29BEE8FA")]
        public readonly InputSlot<float> Offset = new();

        
        [Input(Guid = "283f9939-7b04-4733-8130-edcf34305fe5")]
        public readonly InputSlot<Vector3> Pivot = new();
        
        [Input(Guid = "2f1383f3-f5ae-4f2d-bb8e-0ad8e35dd621")]
        public readonly InputSlot<float> StartW = new();

        [Input(Guid = "ff0c0b01-6272-4580-8ce0-8629c7807d68")]
        public readonly InputSlot<bool> AddSeparator = new();
    }
}