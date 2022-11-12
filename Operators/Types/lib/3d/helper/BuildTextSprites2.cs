using System;
using System.Numerics;
using Operators.Utils;
using T3.Core;
using T3.Core.DataStructures;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1a6a58ea_c63a_4c99_aa9d_aeaeb01662f4
{
    public class BuildTextSprites2 : Instance<BuildTextSprites2>
    {
        [Output(Guid = "3D2F53A3-F1F0-489B-B20B-BADB09CDAEBE")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> SpriteBuffer = new();

        [Output(Guid = "A4295D83-282E-4B5D-8927-312DD26F07A6")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> PointBuffer = new();

        public BuildTextSprites2()
        {
            SpriteBuffer.UpdateAction = Update;
            PointBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (Filepath.DirtyFlag.IsDirty || _bmFont == null)
                _bmFont = BmFontDescription.InitializeFromFile(Filepath.GetValue(context));

            UpdateMesh(context);
        }

        private void UpdateMesh(EvaluationContext context)
        {
            var text = Text.GetValue(context);
            if (string.IsNullOrEmpty(text))
            {
                text = " ";
            }

            if (_bmFont == null)
                return;

            var lineNumber = 0;
            var horizontalAlign = (BmFontDescription.HorizontalAligns)HorizontalAlign.GetValue(context)
                                                                                     .Clamp(0,
                                                                                            Enum.GetValues(typeof(BmFontDescription.HorizontalAligns)).Length -
                                                                                            1);
            var verticalAlign =
                (BmFontDescription.VerticalAligns)VerticalAlign.GetValue(context).Clamp(0, Enum.GetValues(typeof(BmFontDescription.VerticalAligns)).Length - 1);

            //var verticalAlign = VerticalAlign.GetValue(context);
            var characterSpacing = Spacing.GetValue(context);
            var lineHeight = LineHeight.GetValue(context);
            //var commonScaleH = (512.0 / _bmFont.Font.Common.ScaleH);
            var scaleFactor = 1.0 / _bmFont.Font.Info.Size * 0.00185;
            var size = (float)(Size.GetValue(context) * scaleFactor); // Scaling to match 1080p 72DPI pt font sizes 
            var position = Position.GetValue(context);

            var numLinesInText = text.Split('\n').Length;

            var color = Color.GetValue(context);
            float textureWidth = _bmFont.Font.Common.ScaleW;
            float textureHeight = _bmFont.Font.Common.ScaleH;
            float cursorX = 0;
            float cursorY = 0;
            const float sdfWidth = 5f; // assumption after some experiments

            switch (verticalAlign)
            {
                case BmFontDescription.VerticalAligns.Top:
                    cursorY = _bmFont.Font.Common.Base * (1 + sdfWidth / _bmFont.Font.Info.Size);
                    break;
                case BmFontDescription.VerticalAligns.Middle:
                    cursorY = _bmFont.Font.Common.LineHeight * lineHeight * (numLinesInText - 1) / 2 + _bmFont.Font.Common.LineHeight / 2f +
                              _bmFont.Font.Common.Base * (sdfWidth / _bmFont.Font.Info.Size);
                    break;
                case BmFontDescription.VerticalAligns.Bottom:
                    cursorY = _bmFont.Font.Common.LineHeight * lineHeight * numLinesInText;
                    break;
            }

            if (_spriteBuffer == null || _spriteBuffer.Length != text.Length)
            {
                _spriteBuffer = new Sprite[text.Length];
            }

            if (_pointBuffer == null || _pointBuffer.Length != text.Length)
            {
                _pointBuffer = new Point[text.Length];
            }

            var outputIndex = 0;
            var currentLineCharacterCount = 0;
            var lastChar = 0;

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    AdjustLineAlignment();

                    cursorY -= _bmFont.Font.Common.LineHeight * lineHeight;
                    cursorX = 0;
                    currentLineCharacterCount = 0;
                    lastChar = 0;
                    lineNumber++;
                    continue;
                }

                if (!_bmFont.InfoForCharacter.TryGetValue(c, out var charInfo))
                {
                    lastChar = 0;
                    continue;
                }

                if (lastChar != 0)
                {
                    var key = lastChar | c;
                    if (_bmFont.KerningForPairs.TryGetValue(key, out var kerning2))
                    {
                        cursorX += kerning2;
                    }
                }

                var sizeWidth = charInfo.Width * size;
                var sizeHeight = charInfo.Height * size;
                var x = position.X + (cursorX + charInfo.XOffset) * size;
                var y = position.Y + ((cursorY - charInfo.YOffset)) * size;

                if (charInfo.Width != 1 || charInfo.Height != 1)
                {
                    _spriteBuffer[outputIndex]
                        = new Sprite
                              {
                                  Width = sizeWidth,
                                  Height = sizeHeight,
                                  Color = color,
                                  UvMin = new Vector2(charInfo.X / textureWidth, charInfo.Y / textureHeight),
                                  UvMax = new Vector2((charInfo.X + charInfo.Width) / textureWidth, (charInfo.Y + charInfo.Height) / textureHeight),
                                  Pivot = default,
                                  CharIndex = (uint)outputIndex,
                                  CharIndexInLine = (uint)currentLineCharacterCount,
                                  LineIndex = (uint)lineNumber,
                                  Extra = 0
                              };

                    _pointBuffer[outputIndex]
                        = new Point
                              {
                                  Position = new Vector3(x, y, 0),
                                  W = 1,
                                  Orientation = Quaternion.Identity
                              };
                    outputIndex++;
                }

                currentLineCharacterCount++;
                cursorX += charInfo.XAdvance;
                cursorX += characterSpacing;
                lastChar = c;
            }

            AdjustLineAlignment();

            ResourceManager.Instance().SetupStructuredBuffer(_spriteBuffer, ref SpriteBuffer.Value);
            SpriteBuffer.Value.DebugName = "SpriteBufferLayout";
            
            ResourceManager.Instance().SetupStructuredBuffer(_pointBuffer, ref PointBuffer.Value);
            PointBuffer.Value.DebugName = "PointBufferLayout";

            void AdjustLineAlignment()
            {
                switch (horizontalAlign)
                {
                    case BmFontDescription.HorizontalAligns.Center:
                        OffsetLineCharacters((cursorX / 2 - characterSpacing / 2) * size, currentLineCharacterCount, outputIndex);
                        break;
                    case BmFontDescription.HorizontalAligns.Right:
                        OffsetLineCharacters(cursorX * size, currentLineCharacterCount, outputIndex);
                        break;
                }
            }
        }

        private void OffsetLineCharacters(float offset, int currentLineCharacterCount, int outputIndex)
        {
            for (var backIndex = 0; backIndex <= currentLineCharacterCount; backIndex++)
            {
                var index0 = outputIndex - backIndex;
                if (index0 < 0 || index0 >= _pointBuffer.Length)
                    continue;

                _pointBuffer[index0].Position.X -= offset;
            }
        }

        private BmFontDescription _bmFont;

        private Sprite[] _spriteBuffer;
        private Point[] _pointBuffer;

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

        [Input(Guid = "C4F03392-FF7E-4B4A-8740-F93A581B2B6B")]
        public readonly InputSlot<Vector2> Position = new();

        [Input(Guid = "14829EAC-BA59-4D31-90DC-53C7FC56CC30", MappedType = typeof(BmFontDescription.VerticalAligns))]
        public readonly InputSlot<int> VerticalAlign = new();

        [Input(Guid = "E43BC887-D425-4F9C-8A86-A32C761DE0CC", MappedType = typeof(BmFontDescription.HorizontalAligns))]
        public readonly InputSlot<int> HorizontalAlign = new();
    }
}