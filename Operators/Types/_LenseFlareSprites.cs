using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_947ad81e_47da_46c3_9b1d_8e578174d876
{
    public class _LenseFlareSprites : Instance<_LenseFlareSprites>
    {
        [Output(Guid = "B26730FF-B1FF-40A7-91AF-B10026ED4C32")]
        public readonly Slot<StructuredList> OutBuffer = new Slot<StructuredList>();

        public _LenseFlareSprites()
        {
            OutBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var worldToClipSpace = Matrix.Multiply(context.WorldToCamera, context.CameraToClipSpace);
            //Matrix worldToView = context.WorldToCamera * context.CameraProjection;
            //var worldToClipSpace = context.WorldToCamera
            //var viewToWorld = Matrix.Invert(worldToClipSpace);

            
            
            var color = Color.GetValue(context);
            var randomizeColor = RandomizeColor.GetValue(context);
            var size = Size.GetValue(context);
            var randomizeSize = RandomizeSize.GetValue(context);
            var stretch = Stretch.GetValue(context);
            var distance = Distance.GetValue(context);
            var spread = Spread.GetValue(context);
            var randomizeSpread = RandomizeSpread.GetValue(context);
            var positionFactor = PositionFactor.GetValue(context);
            var randomizePosition = RandomizePosition.GetValue(context);
            
            var referencedLightIndex = LightIndex.GetValue(context);

            var innerFxZone = InnerFxZone.GetValue(context);
            var edgeFxZone = EdgeFxZone.GetValue(context);
            var zoneFxScale = FxZoneScale.GetValue(context);
            var zoneFxBrightness = FxZoneBrightness.GetValue(context);
            
            var rand = new Random(RandomSeed.GetValue(context));
            var fxZoneMode = (ZoneFxModes) FxZoneMode.GetValue(context);
            

            int startLightIndex = 0;
            int endLightIndex = context.PointLights.Count;

            if (referencedLightIndex >= 0)
            {
                startLightIndex = referencedLightIndex;
                endLightIndex = referencedLightIndex + 1;
            }

            _tempList.Clear();

            for (int lightIndex = startLightIndex; lightIndex < endLightIndex; lightIndex++)
            {
                var pointLight = context.PointLights.GetPointLight(lightIndex);
                var lightPosDx = pointLight.Position.ToSharpDxVector4(1);

                var posInViewDx = SharpDX.Vector4.Transform(lightPosDx, worldToClipSpace);
                posInViewDx /= posInViewDx.W;

                // Ignore light sources behind
                var hideFactor = posInViewDx.Z < 0 ? 0 : 1;
                
                posInViewDx /= posInViewDx.W;
                var lightPosInView2D = new Vector2(posInViewDx.X, posInViewDx.Y);

                var count = SpriteCount.GetValue(context).Clamp(0, 1000);
                if (count != _sprites.NumElements)
                {
                    _sprites = new StructuredList<Sprite>(count);
                }
                
                // Render Planes
                for (var i = 0; i < count; ++i)
                {
                    var f  =  (float)i / count - 0.5f;
                    var positionOnLine = (float)((-distance
                                                  + f * spread  + spread * randomizeSpread * (rand.NextDouble() - 0.5)
                                                  + randomizeSpread * (rand.NextDouble() - 0.5) + 1));

                    Vector2 objectScreenPos = lightPosInView2D * positionOnLine * positionFactor + (new Vector2(1, 1) - positionFactor) * lightPosInView2D;

                    objectScreenPos += new Vector2((float)(randomizePosition.X * (rand.NextDouble() - 0.5)), (float)(randomizePosition.Y * (rand.NextDouble() - 0.5)));
                    
                    var sizeWithRandom = size * (float)(1.0 + randomizeSize * (rand.NextDouble() - 0.5)) / 0.2f;

                    var spriteColor = Vector4.Clamp(new Vector4(
                                                  color.X + randomizeColor.X * (float)(rand.NextDouble() - 0.5) * 4,
                                                  color.Y + randomizeColor.Y * (float)(rand.NextDouble() - 0.5) * 4,
                                                  color.Z + randomizeColor.Z * (float)(rand.NextDouble() - 0.5) * 4,
                                                  color.W * (1 - randomizeColor.W * (float)(rand.NextDouble() * 2))
                                                 ) * pointLight.Color, Vector4.Zero, Vector4.One);

                    
                    // Center Trigger
                    if (fxZoneMode != ZoneFxModes.Off)
                    {
                        var triggerPosition = fxZoneMode == ZoneFxModes.Lights
                                                  ? lightPosInView2D
                                                  : objectScreenPos;
                    
                        var centerTriggerAmount = innerFxZone.X > 0.0001
                                                      ? Math.Max(0, innerFxZone.X - triggerPosition.Length()) / innerFxZone.X
                                                      : 0;
                        float smoothEdgeTriggerAmount = 0;
                        
                        if (edgeFxZone.X > 0 && edgeFxZone.Y > 0)
                        {
                            var insideEdgeTriggerAmountY = Math.Min(edgeFxZone.X, Math.Max(0, Math.Abs(triggerPosition.Y) - 1 + edgeFxZone.X)) /
                                                           edgeFxZone.X;
                            var outsideEdgeTriggerAmountY = Math.Min(edgeFxZone.Y, Math.Max(0, -Math.Abs(triggerPosition.Y) + 1 + edgeFxZone.Y)) /
                                                            edgeFxZone.Y;
                    
                            var insideEdgeTriggerAmountX = Math.Min(edgeFxZone.X, Math.Max(0, Math.Abs(triggerPosition.X) - 1 + edgeFxZone.X)) /
                                                           edgeFxZone.X;
                            var outsideEdgeTriggerAmountX = Math.Min(edgeFxZone.Y, Math.Max(0, -Math.Abs(triggerPosition.X) + 1 + edgeFxZone.Y)) /
                                                            edgeFxZone.Y;
                    
                            var edgeTriggerAmount = Math.Max(insideEdgeTriggerAmountY * outsideEdgeTriggerAmountY,
                                                             insideEdgeTriggerAmountX * outsideEdgeTriggerAmountX);
                            var t = edgeTriggerAmount;
                            smoothEdgeTriggerAmount = t * t * (3 - 2 * t);
                        }
                    
                        var totalTriggerAmount = centerTriggerAmount + smoothEdgeTriggerAmount;
                        sizeWithRandom *= (1 + zoneFxScale * totalTriggerAmount).Clamp(0,100);
                        spriteColor.W += (zoneFxBrightness * totalTriggerAmount).Clamp(0,1);
                    }

                    // Fade with incomming alpha from FlatShaders and Materials
                    //color.W *= materialAlpha;

                    //objectToWorld = Matrix.Scaling(scale, scale, 1) * objectToWorld;

                    // var autoRotation = 0f;
                    // if (AutoRotate == 1)
                    // {
                    //     //rotateToCenter = (float)(Math.Atan2( objectScreenPos.X- lightScreenPos.X, objectScreenPos.Y - lightScreenPos.Y) + 3.1415/2) * RotateToLight;
                    //     autoRotation = (float)(Math.Atan2(objectScreenPos.X - lightScreenPos.X, objectScreenPos.Y - lightScreenPos.Y) + 3.1415 / 2);
                    // }
                    // else if (AutoRotate == 2)
                    // {
                    //     autoRotation = (float)(Math.Atan2(objectScreenPos.X, objectScreenPos.Y) + 3.1415 / 2);
                    // }
                    //
                    // objectToWorld = Matrix.Translation(OffsetX, OffsetY, 0)
                    //                 * Matrix.RotationZ((float)(
                    //                                               (RotateValue) / 180 * Math.PI
                    //                                               - autoRotation
                    //                                               + (RotateRandom / 180 * Math.PI) * (rand.NextDouble() - 0.5)
                    //                                               + (RotateEntities / 180 * Math.PI) * i / Count
                    //                                           )) * objectToWorld;
                    //
                    // var oldObjectToWorld = context.ObjectTWorld;
                    // context.ObjectTWorld = objectToWorld * context.ObjectTWorld;
                    //
                    // // Transforom UV to pick correct texture cell
                    // if (TextureCellsRows == 0)
                    //     TextureCellsRows = 1;
                    //
                    // if (TextureCellsColumns == 0)
                    //     TextureCellsColumns = 1;

                    // int row = (int)(Math.Floor(i / TextureCellsColumns) % TextureCellsRows);
                    // int column = (int)(i % TextureCellsRows);
                    //
                    // var translationUV = new Vector3(1 / TextureCellsColumns * column, 1 / TextureCellsRows * row, 0);
                    // var rotationUV = new Quaternion();
                    // var scaleUV = new Vector3(1 / TextureCellsColumns, 1 / TextureCellsRows, 0);
                    // var pivotUV = new Vector3(0, 0, 0);
                    //
                    // var transformUV = Matrix.Transformation(pivotUV, new Quaternion(), scaleUV, pivotUV, rotationUV, translationUV);
                    // var prevTransformUV = context.TextureMatrix;
                    // context.TextureMatrix = transformUV * prevTransformUV;

                    
                    _tempList.Add(new Sprite
                                      {
                                          PosInClipSpace = objectScreenPos,
                                          Size = sizeWithRandom * stretch * hideFactor,
                                          Color = spriteColor,
                                          Rotation = 0,
                                          UvMin = Vector2.Zero,
                                          UvMax = Vector2.One,
                                      });                    
                    
                }
            }

            // Copy to structured array
            if (_tempList.Count != _sprites.NumElements)
            {
                _sprites = new StructuredList<Sprite>(_tempList.Count);
            }

            for (var spriteIndex = 0; spriteIndex < _tempList.Count; spriteIndex++)
            {
                _sprites.TypedElements[spriteIndex] = _tempList[spriteIndex];
            }

            OutBuffer.Value = _sprites;
        }

        private List<Sprite> _tempList = new List<Sprite>(100);

        [StructLayout(LayoutKind.Explicit, Size = SizeInBytes)]
        public struct Sprite
        {
            [FieldOffset(0 * 4)]
            public Vector2 PosInClipSpace;

            [FieldOffset(2 * 4)]
            public Vector2 Size;

            [FieldOffset(4 * 4)]
            public float Rotation;

            [FieldOffset(5 * 4)]
            public Vector4 Color;

            [FieldOffset(9 * 4)]
            public Vector2 UvMin;

            [FieldOffset(11 * 4)]
            public Vector2 UvMax;

            [FieldOffset(13 * 4)]
            public Vector3 __padding;

            private const int SizeInBytes = 16 * 4;
        }

        private StructuredList<Sprite> _sprites = new StructuredList<Sprite>(10);

        private enum ColorSources
        {
            Light,
            Global,
        }

        private enum ZoneFxModes
        {
            Off,
            Objects,
            Lights,
        }

        [Input(Guid = "50FDA27A-BD94-4438-9CEF-0AAA16FAE12B")]
        public readonly InputSlot<int> LightIndex = new();

        [Input(Guid = "59BA312F-C74F-4E76-8830-57C2EFFA0FF1")]
        public readonly InputSlot<int> SpriteCount = new();

        [Input(Guid = "F21DBEFD-EA54-4901-A806-B1A2C5EE140F")]
        public readonly InputSlot<float> Distance = new();

        [Input(Guid = "792E9B7E-9094-4BF9-8AED-D8B3FCDEE358")]
        public readonly InputSlot<float> Spread = new();

        [Input(Guid = "CCF77198-7682-4F6B-96F2-986E6827A4DF")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "C8347CA9-C700-4195-A23F-0F220F5823E2")]
        public readonly InputSlot<Vector2> Stretch = new();

        [Input(Guid = "2A1285B1-63EA-46A5-8D96-B7BA33EDD88B")]
        public readonly InputSlot<float> RandomizeSize = new();

        [Input(Guid = "BC1D9FDC-EA07-4C0D-BE2D-02FA955F9E5A")]
        public readonly InputSlot<float> RandomizeSpread = new();

        
        [Input(Guid = "7244CC40-8F0A-4381-80A3-EB818E262C88", MappedType = typeof(ColorSources))]
        public readonly InputSlot<int> ColorSource = new();

        [Input(Guid = "7D9DA46C-2D1F-48F8-BDC5-BB7E29C363C7")]
        public readonly InputSlot<Vector4> Color = new();

        [Input(Guid = "53351CF3-71A5-4CB3-AD81-60ABC7718D4B")]
        public readonly InputSlot<Vector4> RandomizeColor = new();

        [Input(Guid = "77EAC715-D2EE-4BD5-93F5-1E9E7119A0E6")]
        public readonly InputSlot<Vector2> TextureCells = new();

        [Input(Guid = "9CFFFB1A-675E-410C-96DA-C02BD6B3A81A")]
        public readonly InputSlot<int> RandomSeed = new();
        
        [Input(Guid = "1C250003-CF16-44DF-9A5E-F9FCF331617C")]
        public readonly InputSlot<Vector2> PositionFactor = new();

        [Input(Guid = "D314C572-71C7-4A67-921B-DF369817DD4A")]
        public readonly InputSlot<Vector2> RandomizePosition = new();

        [Input(Guid = "C1B5F49F-3538-48AA-8D16-92D48FCF08CB", MappedType = typeof(ZoneFxModes))]
        public readonly InputSlot<int> FxZoneMode = new();
        
        [Input(Guid = "1C11CB25-05F8-4422-AA16-DB57E8CD2E0B")]
        public readonly InputSlot<Vector2> EdgeFxZone = new();
        
        [Input(Guid = "00ED2D51-4CF0-43D6-ADAA-CE42E5EB8439")]
        public readonly InputSlot<Vector2> InnerFxZone = new();

        [Input(Guid = "0D98C14C-3F62-47E2-8FE8-A85208D9C02D")]
        public readonly InputSlot<float> FxZoneScale = new();

        [Input(Guid = "520EE127-F542-4AD8-A6EA-4A24A70ADE4D")]
        public readonly InputSlot<float> FxZoneBrightness = new();

        
    }
}