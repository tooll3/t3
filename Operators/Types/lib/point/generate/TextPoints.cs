using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.DataTypes;
using T3.Core.Logging;
using PointT3 = T3.Core.DataTypes.Point;

using System.IO;
using System.Collections.Generic;
using System.Numerics;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace T3.Operators.Types.Id_bdb41a6d_e225_4a8a_8348_820d45153e3f
{
    public class TextPoints : Instance<TextPoints>
    {

        [Input(Guid = "7432f063-9957-47f7-8250-c3e6456ec7c6")]
        public readonly InputSlot<string> InputText = new InputSlot<string>();

        [Input(Guid = "abe3f777-a33b-4e39-9eee-07cc729acf32")]
        public readonly InputSlot<string> InputFont = new InputSlot<string>();

        [Input(Guid = "12467649-9036-4b47-9868-2b4436580227")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "711edaf8-3fd1-4453-966d-78ed777df934")]
        public readonly InputSlot<float> Resolution = new InputSlot<float>();

        [Input(Guid = "6b378d1a-afc1-4006-ac50-b5b80dc55535")]
        public readonly InputSlot<float> LineHeight = new InputSlot<float>();

        [Input(Guid = "e2cff4bc-6c40-40f1-95a1-9e48d0c8624f", MappedType = typeof(TextAligns))]
        public readonly InputSlot<int> TextAlign = new InputSlot<int>();

        [Input(Guid = "02163610-108e-4154-a3b2-84b65df852fa", MappedType =  typeof(VerticalAlignment))]
        public readonly InputSlot<int> VerticalAlign = new InputSlot<int>();

        [Input(Guid = "128bd413-10ab-4c2c-9c49-3de925c26f02", MappedType =  typeof(HorizontalAlignment))]
        public readonly InputSlot<int> HorizontalAlign = new InputSlot<int>();

        public enum TextAligns
        {
            Left,
            Right,
            Center,
        }

        public TextPoints()
        {
            OutputList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            string inputText = InputText.GetValue(context);
            string inputFont = InputFont.GetValue(context);

            // check if text and font are not empty
            if (string.IsNullOrEmpty(inputText) || string.IsNullOrEmpty(inputFont))
            {
                return;
            }

            // check if font file exist
            if (!File.Exists(inputFont))
            {
                Log.Debug($"File {inputFont} doesn't exist", this);
                return;
            }

            // fetch parameters
            float size = Size.GetValue(context);
            float resolution = Resolution.GetValue(context);
            float lineSpace = LineHeight.GetValue(context);
            TextAlignment textAlign = (TextAlignment)TextAlign.GetValue(context);
            VerticalAlignment vertical = (VerticalAlignment)VerticalAlign.GetValue(context);
            HorizontalAlignment horizontal = (HorizontalAlignment)HorizontalAlign.GetValue(context);

            // create font
            FontCollection collection = new();
            FontFamily family = collection.Add(inputFont);
            Font font = family.CreateFont(resolution);
            TextOptions textOptions = new(font)
            {
                TextAlignment = textAlign,
                VerticalAlignment = vertical,
                HorizontalAlignment = horizontal,
                LineSpacing = lineSpace,
            };

            // generate points wiht text from font
            IPathCollection paths = TextBuilder.GenerateGlyphs(inputText, textOptions);
            List<PointT3> points = new List<PointT3>();
            foreach (var path in paths)
            {
                var p = path.Flatten();
                foreach (var q in p)
                {
                    // fill list with points from glyph (and close the path)
                    int count = q.Points.Length;
                    for (int i = 0; i < count+1; ++i)
                    {
                        points.Add(GetPoint(q.Points.Span[i%count], size/resolution));
                    }

                    // add line separator
                    points.Add(PointT3.Separator());
                }
            }

            // no points
            if (points.Count == 0) return;

            // output list
            StructuredList<PointT3> list = new StructuredList<PointT3>(points.Count);
            for (int index = 0; index < points.Count; index++) list.TypedElements[index] = points[index];
            OutputList.Value = list;
        }

        PointT3 GetPoint (PointF p, float size)
        {
            return new PointT3
            {
                Position = new Vector3(p.X * size, -p.Y * size, 0f),
                W = 1f,
                Orientation = new Quaternion(0f, 0f, 0f, 1f),
                Color = new Vector4(1f, 1f, 1f, 1f)
            };
        }

        [Output(Guid = "c65da6e8-3eb7-4152-9b79-34fcaaa31807")]
        public readonly Slot<StructuredList> OutputList = new Slot<StructuredList>();
    }
}

