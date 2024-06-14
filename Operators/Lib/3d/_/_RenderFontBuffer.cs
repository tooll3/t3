using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Operators.Utils;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Utils;

namespace lib._3d._
{
	[Guid("c5707b79-859b-4d53-92e0-cbed53aae648")]
    public class _RenderFontBuffer : Instance<_RenderFontBuffer>
    {
        [Output(Guid = "3D2F53A3-F1F0-489B-B20B-BADB09CDAEBE")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new();

        [Output(Guid = "A0ECA9CE-35AA-497D-B5C9-CDE52A7C8D58")]
        public readonly Slot<int> VertexCount = new();

        
        public _RenderFontBuffer()
        {
            Buffer.UpdateAction = UpdateMesh;
            _fontResource = new Resource<BmFontDescription>(Filepath, OnFileChanged);
        }
        
        private bool OnFileChanged(FileResource file, BmFontDescription currentValue, out BmFontDescription newValue, out string failureReason)
        {
            if (BmFontDescription.TryInitializeFromFile(file.AbsolutePath, out newValue))
            {
                failureReason = null;
                return true;
            }
            
            failureReason = "Failed to load font from file";
            return false;
        }
        
        private void UpdateMesh(EvaluationContext context)
        {
            var text = Text.GetValue(context);
            if (string.IsNullOrEmpty(text) )
            {
                text = " ";
            }

            if (_font == null)
                return;

            var lineNumber = 0;
            var horizontalAlign = (BmFontDescription.HorizontalAligns)HorizontalAlign.GetValue(context).Clamp(0, Enum.GetValues(typeof(BmFontDescription.HorizontalAligns)).Length -1);
            var verticalAlign = (BmFontDescription.VerticalAligns)VerticalAlign.GetValue(context).Clamp(0, Enum.GetValues(typeof(BmFontDescription.VerticalAligns)).Length -1);
            
            var characterSpacing = Spacing.GetValue(context);
            var lineHeight = LineHeight.GetValue(context);
            var scaleFactor = 1.0 / _font.BmFont.Info.Size * 0.00185; 
            var size = (float)(Size.GetValue(context)  * scaleFactor); // Scaling to match 1080p 72DPI pt font sizes 
            var position = Position.GetValue(context);

            var numLinesInText = text.Split('\n').Length;

            var color = Color.GetValue(context);
            float textureWidth = _font.BmFont.Common.ScaleW;
            float textureHeight = _font.BmFont.Common.ScaleH;
            float cursorX = 0;
            float cursorY = 0;
            const float sdfWidth = 5f; // assumption after some experiments

            switch (verticalAlign)
            {
                case BmFontDescription.VerticalAligns.Top:
                    cursorY = _font.BmFont.Common.Base * (1 + sdfWidth / _font.BmFont.Info.Size);
                    break;
                case BmFontDescription.VerticalAligns.Middle:
                    cursorY = _font.BmFont.Common.LineHeight * lineHeight * (numLinesInText - 1) / 2 + _font.BmFont.Common.LineHeight / 2f +
                              _font.BmFont.Common.Base * (sdfWidth / _font.BmFont.Info.Size);
                    break;
                case BmFontDescription.VerticalAligns.Bottom:
                    cursorY = _font.BmFont.Common.LineHeight * lineHeight * numLinesInText;
                    break;
            }

            if (_bufferContent == null || _bufferContent.Length != text.Length)
            {
                _bufferContent = new BufferLayout[text.Length];
            }

            var outputIndex = 0;
            var currentLineCharacterCount = 0;
            var lastChar = 0;

            for (var index = 0; index < text.Length; index++)
            {
                var c = text[index];
                
                if (c == '\n')
                {
                    AdjustLineAlignment();

                    cursorY -= _font.BmFont.Common.LineHeight * lineHeight;
                    cursorX = 0;
                    currentLineCharacterCount = 0;
                    lastChar = 0;
                    lineNumber++;
                    continue;
                }

                if (!_font.InfoForCharacter.TryGetValue(c, out var charInfo))
                {
                    lastChar = 0;
                    continue;
                }



                if (lastChar != 0)
                {
                    int key = lastChar | c;
                    if (_font.KerningForPairs.TryGetValue(key, out var kerning2))
                    {
                        cursorX += kerning2;
                    }
                }

                var sizeWidth = charInfo.Width * size;
                var sizeHeight = charInfo.Height * size;
                var x = position.X + (cursorX + charInfo.XOffset)  * size;
                var y = position.Y + ((cursorY - charInfo.YOffset)) * size ;

                if (charInfo.Width != 1 || charInfo.Height != 1)
                {
                    _bufferContent[outputIndex]
                        = new BufferLayout
                              {
                                  Position = new Vector3(x, y, 0),
                                  CharHeight = sizeHeight,
                                  Orientation = Quaternion.Identity,
                                  AspectRatio = sizeWidth / sizeHeight,
                                  Color = color,
                                  UvMinMax = new Vector4(
                                                         charInfo.X / textureWidth, // uLeft 
                                                         charInfo.Y / textureHeight, // vTop 
                                                         (charInfo.X + charInfo.Width) / textureWidth, // uRight 
                                                         (charInfo.Y + charInfo.Height) / textureHeight // vBottom                              
                                                        ),
                                  Id = (uint)outputIndex,
                                  LineNumber = (uint)lineNumber,
                                  Offset = new Vector2(charInfo.XOffset, charInfo.YOffset),
                              };

                    outputIndex++;
                }

                currentLineCharacterCount++;
                cursorX += charInfo.XAdvance;
                cursorX += characterSpacing;
                lastChar = c;
            }
            AdjustLineAlignment();

            ResourceManager.SetupStructuredBuffer(_bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(_RenderFontBuffer);
            VertexCount.Value = outputIndex * 6;

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
                if (index0 < 0 || index0 >= _bufferContent.Length)
                    continue;
                
                _bufferContent[index0].Position.X -= offset;
            }
        }

        private BmFontDescription _font;
        private BufferLayout[] _bufferContent;
        private Resource<BmFontDescription> _fontResource;

        [StructLayout(LayoutKind.Explicit, Size = StructSize)]
        public struct BufferLayout
        {
            [FieldOffset(0)]
            public Vector3 Position;

            [FieldOffset(3 * 4)]
            public float CharHeight;

            [FieldOffset(4 * 4)]
            public float AspectRatio;
            
            [FieldOffset(5 * 4)]
            public Quaternion Orientation;
            
            [FieldOffset(9 * 4)]
            public Vector4 Color;

            [FieldOffset(13 * 4)]
            public Vector4 UvMinMax;
            
            [FieldOffset(17 * 4)]
            public uint Id;
            
            [FieldOffset(18 * 4)]
            public uint LineNumber;
            
            [FieldOffset(19 * 4)]
            public Vector2 Offset;

            private const int StructSize = 21 * 4;
        }
        
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

        [Input(Guid = "FFD2233A-8F3E-426B-815B-8071E4C779AB")]
        public readonly InputSlot<float> Slant = new();

        [Input(Guid = "14829EAC-BA59-4D31-90DC-53C7FC56CC30")]
        public readonly InputSlot<int> VerticalAlign = new();

        [Input(Guid = "E43BC887-D425-4F9C-8A86-A32C761DE0CC")]
        public readonly InputSlot<int> HorizontalAlign = new();        
    }
}