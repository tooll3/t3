using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Utils.BmFont;

namespace T3.Operators.Types.Id_c5707b79_859b_4d53_92e0_cbed53aae648
{
    public class _RenderFontBuffer : Instance<_RenderFontBuffer>
    {
        // Inputs ----------------------------------------------------
        [Input(Guid = "F2DD87B1-7F37-4B02-871B-B2E35972F246")]
        public readonly InputSlot<string> Text = new InputSlot<string>();

        [Input(Guid = "E827FDD1-20CA-473C-99EE-B839563690E9")]
        public readonly InputSlot<string> Filepath = new InputSlot<string>();

        [Input(Guid = "1CDE902D-5EAA-4144-B579-85F54717356B")]
        public readonly InputSlot<Vector4> Color = new InputSlot<Vector4>();

        [Input(Guid = "5008E9B4-083A-4494-8F7C-50FE5D80FC35")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "E05E143E-8D4C-4DE7-8C9C-7FA7755009D3")]
        public readonly InputSlot<float> Spacing = new InputSlot<float>();

        [Input(Guid = "9EB4E13F-0FE3-4ED9-9DF1-814F075A05DA")]
        public readonly InputSlot<float> LineHeight = new InputSlot<float>();

        [Input(Guid = "C4F03392-FF7E-4B4A-8740-F93A581B2B6B")]
        public readonly InputSlot<Vector2> Position = new InputSlot<Vector2>();

        [Input(Guid = "FFD2233A-8F3E-426B-815B-8071E4C779AB")]
        public readonly InputSlot<float> Slant = new InputSlot<float>();

        [Input(Guid = "14829EAC-BA59-4D31-90DC-53C7FC56CC30")]
        public readonly InputSlot<int> VerticalAlign = new InputSlot<int>();

        [Input(Guid = "E43BC887-D425-4F9C-8A86-A32C761DE0CC")]
        public readonly InputSlot<int> HorizontalAlign = new InputSlot<int>();

        // Outputs ---------------------------------------------------------

        [Output(Guid = "3D2F53A3-F1F0-489B-B20B-BADB09CDAEBE")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new Slot<SharpDX.Direct3D11.Buffer>();

        [Output(Guid = "A0ECA9CE-35AA-497D-B5C9-CDE52A7C8D58")]
        public readonly Slot<int> VertexCount = new Slot<int>();

        public _RenderFontBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            InitializeFontDescription(context);
            UpdateMesh(context);
        }

        private void InitializeFontDescription(EvaluationContext context)
        {
            if (!Filepath.DirtyFlag.IsDirty && _font != null)
                return;

            var filepath = Filepath.GetValue(context);
            if (FontDescriptions.ContainsKey(filepath))
            {
                _font = FontDescriptions[filepath];
                return;
            }

            Font bmFont;

            var serializer = new XmlSerializer(typeof(Font));
            try
            {
                var stream = new FileStream(filepath, FileMode.Open);
                bmFont = (Font)serializer.Deserialize(stream);
                Log.Debug("loaded font with character count:" + bmFont.Chars.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load font {filepath} " + e + "\n" + e.Message);
                return;
            }

            _font = new FontDescription(bmFont);

            FontDescriptions[filepath] = _font;
        }

        private void UpdateMesh(EvaluationContext context)
        {
            var text = Text.GetValue(context);
            if (string.IsNullOrEmpty(text))
                return;

            if (_font == null)
                return;

            var horizontalAlign = HorizontalAlign.GetValue(context);
            var verticalAlign = VerticalAlign.GetValue(context);
            var characterSpacing = Spacing.GetValue(context);
            var lineHeight = LineHeight.GetValue(context);
            var commonScaleH = (512.0 / _font.Font.Common.ScaleH);
            var scaleFactor = 1.0 / _font.Font.Info.Size * 0.00185; 
            var size = (float)(Size.GetValue(context)  * scaleFactor); // Scaling to match 1080p 72DPI pt font sizes 
            var position = Position.GetValue(context);

            var numLinesInText = text.Split('\n').Length;

            var color = Color.GetValue(context);
            float textureWidth = _font.Font.Common.ScaleW;
            float textureHeight = _font.Font.Common.ScaleH;
            float cursorX = 0;
            float cursorY = 0;
            const float SdfWidth = 5f; // assumption after some experiments

            switch (verticalAlign)
            {
                // Top
                case 0:
                    //cursorY = _font.Font.Common.LineHeight * lineHeight * 1;
                    //cursorY = 40;// _font.Font.Common.LineHeight - _font.Font.Common.Base;// -_font.Font.Common.LineHeight;
                    cursorY = _font.Font.Common.Base * (1+ SdfWidth / _font.Font.Info.Size);
                    break;
                // Middle
                case 1:
                    //var evenLineNumber = numLinesInText + numLinesInText % 2;  
                    cursorY = _font.Font.Common.LineHeight * lineHeight * (numLinesInText - 1) / 2
                              + _font.Font.Common.LineHeight / 2f
                              + _font.Font.Common.Base * ( SdfWidth / _font.Font.Info.Size);
                    break;
                // Bottom
                case 2:
                    cursorY = _font.Font.Common.LineHeight * lineHeight * numLinesInText;
                    break;
            }


            

            //cursorY += (_font.Font.Common.LineHeight - _font.Font.Common.Base) * lineHeight;
            //cursorY += ( _font.Font.Common.Base) * lineHeight / 6;
            //cursorY += _font.Font.Common.LineHeight * lineHeight/6;
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

                    cursorY -= _font.Font.Common.LineHeight * lineHeight;
                    cursorX = 0;
                    currentLineCharacterCount = 0;
                    lastChar = 0;
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
                                  Size = sizeHeight,
                                  Orientation = new Vector3(0, 1, 0),
                                  AspectRatio = sizeWidth / sizeHeight,
                                  Color = color,
                                  UvMinMax = new Vector4(
                                                         charInfo.X / textureWidth, // uLeft 
                                                         charInfo.Y / textureHeight, // vTop 
                                                         (charInfo.X + charInfo.Width) / textureWidth, // uRight 
                                                         (charInfo.Y + charInfo.Height) / textureHeight // vBottom                              
                                                        ),
                                  BirthTime = (float)context.TimeForKeyframes,
                                  Speed = 0,
                                  Id = (uint)outputIndex,
                              };

                    outputIndex++;
                }

                currentLineCharacterCount++;
                cursorX += charInfo.XAdvance;
                cursorX += characterSpacing;
                lastChar = c;
            }
            AdjustLineAlignment();

            ResourceManager.Instance().SetupStructuredBuffer(_bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(_RenderFontBuffer);
            VertexCount.Value = outputIndex * 6;

            void AdjustLineAlignment()
            {
                switch (horizontalAlign)
                {
                    case 1:
                        OffsetLineCharacters((cursorX / 2 - characterSpacing / 2) * size, currentLineCharacterCount, outputIndex);
                        break;
                    case 2:
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

        private FontDescription _font;
        private static readonly Dictionary<string, FontDescription> FontDescriptions = new Dictionary<string, FontDescription>();

        private class FontDescription
        {
            public FontDescription(Font bmFont)
            {
                Font = bmFont;

                foreach (var c in bmFont.Chars)
                {
                    InfoForCharacter[c.Id] = c;
                }

                foreach (var kerning in bmFont.Kernings)
                {
                    var key = kerning.First << 16 | kerning.Second;
                    var value = kerning.Amount;
                    KerningForPairs[key] = value;
                }
            }

            public readonly Font Font;
            public readonly Dictionary<int, float> KerningForPairs = new Dictionary<int, float>();
            public readonly Dictionary<int, FontChar> InfoForCharacter = new Dictionary<int, FontChar>();
        }

        private BufferLayout[] _bufferContent;

        // Must be multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct BufferLayout
        {
            [FieldOffset(0)]
            public Vector3 Position;

            [FieldOffset(3 * 4)]
            public float Size;

            [FieldOffset(4 * 4)]
            public Vector3 Orientation;

            [FieldOffset(7 * 4)]
            public float AspectRatio;

            [FieldOffset(8 * 4)]
            public Vector4 Color;

            [FieldOffset(12 * 4)]
            public Vector4 UvMinMax;

            [FieldOffset(16 * 4)]
            public float BirthTime;

            [FieldOffset(17 * 4)]
            public float Speed;

            [FieldOffset(18 * 4)]
            public uint Id;
        }
    }
}