using System;
using System.Collections.Generic;
using System.Numerics;
using Operators.Utils;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_1a6a58ea_c63a_4c99_aa9d_aeaeb01662f4
{
    public class TextSprites : Instance<TextSprites>
    {
        [Output(Guid = "89685AC6-6A97-403C-8334-E685A4CCCDA0")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> PointBuffer = new();

        [Output(Guid = "0BEA15C8-329A-4705-BC11-12EEF4F1A70A")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> SpriteBuffer = new();

        [Output(Guid = "5BB66419-9FA8-4BD0-8476-D389E9EC78D5")]
        public readonly Slot<Texture2D> Texture = new();

        public TextSprites()
        {
            SpriteBuffer.UpdateAction = Update;
            PointBuffer.UpdateAction = Update;
            Texture.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (Filepath.DirtyFlag.IsDirty || _bmFont == null)
            {
                var filepath = Filepath.GetValue(context);
                _bmFont = BmFontDescription.InitializeFromFile(filepath);
                if (_bmFont != null)
                {
                    var imageFilePath = filepath.Replace(".fnt", ".png");
                    UpdateTexture(imageFilePath);
                }
            }

            UpdateMesh(context);
            
            Texture.DirtyFlag.Clear();
            SpriteBuffer.DirtyFlag.Clear();
            PointBuffer.DirtyFlag.Clear();
        }

        private void UpdateTexture(string imageFilePath)
        {
            ShaderResourceView srv = null;

            try
            {
                (_textureResId, _srvResId) = ResourceManager.Instance().CreateTextureFromFile(imageFilePath, () => { Texture.DirtyFlag.Invalidate(); });

                if (ResourceManager.ResourcesById.TryGetValue(_textureResId, out var resource1) && resource1 is Texture2dResource textureResource)
                    Texture.Value = textureResource.Texture;

                if (ResourceManager.ResourcesById.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
                    srv = srvResource.ShaderResourceView;
            }
            catch (Exception e)
            {
                Log.Error($"Filed to create texture from file '{imageFilePath}':" + e.Message);
            }

            try
            {
                if (srv != null)
                    ResourceManager.Device.ImmediateContext.GenerateMips(srv);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to generate mipmaps for texture {imageFilePath}:" + e);
            }

            Texture.DirtyFlag.Clear();
        }

        private void UpdateMesh(EvaluationContext context)
        {
            var text = Text.GetValue(context);

            if (_bmFont == null)
            {
                Log.Warning("Can't generate text sprites without valid BMFont definition", this);
                return;
            }

            var lineNumber = 0;

            var offsetBaseLine = OffsetBaseLine.GetValue(context);
            var horizontalAlign = (BmFontDescription.HorizontalAligns)HorizontalAlign.GetValue(context)
                                                                                     .Clamp(0,
                                                                                            Enum.GetValues(typeof(BmFontDescription.HorizontalAligns)).Length -
                                                                                            1);
            var verticalAlign =
                (BmFontDescription.VerticalAligns)VerticalAlign.GetValue(context).Clamp(0, Enum.GetValues(typeof(BmFontDescription.VerticalAligns)).Length - 1);

            var characterSpacing = Spacing.GetValue(context);
            var lineHeight = LineHeight.GetValue(context);
            const float viewHeightInT3Units = 2;
            var textSize = Size.GetValue(context);

            var scaleFactor =  (textSize / _bmFont.BmFont.Info.Size) * (viewHeightInT3Units / 1080f); // from cursor space to t3 units  
            var textPosition = Position.GetValue(context);

            var numLinesInText = text.Split('\n').Length;

            var color = Color.GetValue(context);
            float textureWidth = _bmFont.BmFont.Common.ScaleW;
            float textureHeight = _bmFont.BmFont.Common.ScaleH;
            float cursorX = 0;
            float cursorY = 0;
            var verticalCenterOffset = _bmFont.Padding.Up + _bmFont.BmFont.Common.Base + _bmFont.Padding.Down - _bmFont.BmFont.Info.Size / 2f;

            switch (verticalAlign)
            {
                case BmFontDescription.VerticalAligns.Top:
                    // an ugly approximation to the original text implementation
                    cursorY = _bmFont.BmFont.Common.Base * (1 + _bmFont.Padding.Up / _bmFont.BmFont.Info.Size) - verticalCenterOffset;
                    break;
                
                case BmFontDescription.VerticalAligns.Middle:
                    cursorY = _bmFont.BmFont.Common.LineHeight * lineHeight * (numLinesInText - 1) / 2;
                    break;
                
                case BmFontDescription.VerticalAligns.Bottom:
                    cursorY = _bmFont.BmFont.Common.LineHeight * lineHeight * (numLinesInText) - (verticalCenterOffset - offsetBaseLine);
                    break;
            }
            
            _sprites.Clear();
            _points.Clear();

            var outputIndex = 0;
            var currentLineCharacterCount = 0;
            var lastCharId = 0;

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    AdjustLineAlignment();

                    cursorY -= _bmFont.BmFont.Common.LineHeight * lineHeight;
                    cursorX = 0;
                    currentLineCharacterCount = 0;
                    lastCharId = 0;
                    lineNumber++;
                    continue;
                }

                if (!_bmFont.InfoForCharacter.TryGetValue(c, out var charInfo))
                {
                    lastCharId = 0;
                    continue;
                }

                cursorX += _bmFont.GetKerning(lastCharId, charInfo.Id) *0;



                var charTopLeft = new Vector2(cursorX + charInfo.XOffset,
                                              cursorY + charInfo.YOffset);

                var center = textPosition + new Vector3(
                                                        charTopLeft.X + charInfo.Width/2f,
                                                        cursorY,
                                                        0
                                                       ) * scaleFactor;

                var pivot = new Vector2(0,
                                        charInfo.YOffset + charInfo.Height / 2f - verticalCenterOffset - offsetBaseLine
                                       );
                pivot *= scaleFactor;

                var isVisible = charInfo.Width != 0 || charInfo.Height != 0;
                //if (isVisible)
                {
                    _sprites.Add(
                                 new Sprite
                                     {
                                         Width = charInfo.Width * scaleFactor,
                                         Height = charInfo.Height * scaleFactor,
                                         Color = color,
                                         UvMin = new Vector2(charInfo.X / textureWidth, charInfo.Y / textureHeight),
                                         UvMax = new Vector2((charInfo.X + charInfo.Width) / textureWidth, (charInfo.Y + charInfo.Height) / textureHeight),
                                         Pivot = pivot,
                                         CharIndex = (uint)outputIndex,
                                         CharIndexInLine = (uint)currentLineCharacterCount,
                                         LineIndex = (uint)lineNumber,
                                         Extra = 0
                                     });

                    _points.Add(new Point
                                    {
                                        Position = center,
                                        W = 1,
                                        Orientation = Quaternion.Identity,
                                        Selected = 1,
                                        Velocity = Vector3.One,
                                        Color = Vector4.One,
                                    });
                    outputIndex++;
                    currentLineCharacterCount++;
                }

                cursorX += charInfo.XAdvance;
                cursorX += characterSpacing;
                lastCharId = charInfo.Id;
            }

            AdjustLineAlignment();

            SpriteBuffer.Value ??= new BufferWithViews();
            ResourceManager.SetupStructuredBuffer( _sprites.Count > 0 ? _sprites.ToArray() : _nonSprite, ref SpriteBuffer.Value.Buffer);
            ResourceManager.CreateStructuredBufferSrv(SpriteBuffer.Value.Buffer, ref SpriteBuffer.Value.Srv);
            ResourceManager.CreateStructuredBufferUav(SpriteBuffer.Value.Buffer, UnorderedAccessViewBufferFlags.None,ref SpriteBuffer.Value.Uav);

            PointBuffer.Value ??= new BufferWithViews();
            ResourceManager.SetupStructuredBuffer( _points.Count > 0 ? _points.ToArray() : _nonPoints, ref PointBuffer.Value.Buffer);
            ResourceManager.CreateStructuredBufferSrv(PointBuffer.Value.Buffer, ref PointBuffer.Value.Srv);
            ResourceManager.CreateStructuredBufferUav(PointBuffer.Value.Buffer, UnorderedAccessViewBufferFlags.None,ref PointBuffer.Value.Uav);

            void AdjustLineAlignment()
            {
                switch (horizontalAlign)
                {
                    case BmFontDescription.HorizontalAligns.Center:
                        OffsetLineCharacters((cursorX / 2 - characterSpacing / 2) * scaleFactor, currentLineCharacterCount, outputIndex);
                        break;
                    case BmFontDescription.HorizontalAligns.Right:
                        OffsetLineCharacters(cursorX * scaleFactor, currentLineCharacterCount, outputIndex);
                        break;
                }
            }
        }

        private readonly Sprite[] _nonSprite = { new()
                                                            {
                                                                Height = 0,
                                                                Width =  0,
                                                             
                                                            } };
        
        private readonly Point[] _nonPoints = { new()
                                                           {
                                                               Position = Vector3.Zero,
                                                               W= float.NaN,
                                                               Orientation =  Quaternion.Identity,
                                                               Color = Vector4.One,
                                                               Selected = 1,
                                                               Velocity = Vector3.One,
                                                           } };

        private void OffsetLineCharacters(float offset, int currentLineCharacterCount, int outputIndex)
        {
            for (var backIndex = 0; backIndex <= currentLineCharacterCount; backIndex++)
            {
                var index0 = outputIndex - backIndex;
                if (index0 < 0 || index0 >= _points.Count)
                    continue;

                var p = _points[index0];
                p.Position.X -= offset;
                _points[index0] = p;
            }
        }

        private BmFontDescription _bmFont;

        private readonly List<Sprite> _sprites = new(100);
        private readonly List<Point> _points = new(100);

        // Inputs ----------------------------------------------------
        [Input(Guid = "F2DD87B1-7F37-4B02-871B-B2E35972F246")]
        public readonly InputSlot<string> Text = new();

        [Input(Guid = "E827FDD1-20CA-473C-99EE-B839563690E9")]
        public readonly InputSlot<string> Filepath = new();

        [Input(Guid = "1CDE902D-5EAA-4144-B579-85F54717356B")]
        public readonly InputSlot<Vector4> Color = new();

        [Input(Guid = "5008E9B4-083A-4494-8F7C-50FE5D80FC35")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "E05E143E-8D4C-4DE7-8C9C-7FA7755009D3")]
        public readonly InputSlot<float> Spacing = new();

        [Input(Guid = "9EB4E13F-0FE3-4ED9-9DF1-814F075A05DA")]
        public readonly InputSlot<float> LineHeight = new();

        [Input(Guid = "77A79A37-6151-4A35-BE4D-415A91B0E651")]
        public readonly InputSlot<Vector3> Position = new();

        [Input(Guid = "14829EAC-BA59-4D31-90DC-53C7FC56CC30", MappedType = typeof(BmFontDescription.VerticalAligns))]
        public readonly InputSlot<int> VerticalAlign = new();

        [Input(Guid = "E43BC887-D425-4F9C-8A86-A32C761DE0CC", MappedType = typeof(BmFontDescription.HorizontalAligns))]
        public readonly InputSlot<int> HorizontalAlign = new();

        [Input(Guid = "7094D22F-DCF9-4FD0-9570-7243E3284CF4")]
        public readonly InputSlot<float> OffsetBaseLine = new();

        private uint _textureResId;
        private uint _srvResId;
    }
}