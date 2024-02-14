using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.DataTypes;
using T3.Core.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Numerics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace T3.Operators.Types.Id_bdb41a6d_e225_4a8a_8348_820d45153e3f
{
    public class TextPoints : Instance<TextPoints>
    {

        [Input(Guid = "7432f063-9957-47f7-8250-c3e6456ec7c6")]
        public readonly InputSlot<string> InputText = new InputSlot<string>();

        [Input(Guid = "abe3f777-a33b-4e39-9eee-07cc729acf32")]
        public readonly InputSlot<string> InputFont = new InputSlot<string>();

        public TextPoints()
        {
            OutputList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            string inputText = InputText.GetValue(context);
            string inputFont = InputFont.GetValue(context);

            if (string.IsNullOrEmpty(inputText) || string.IsNullOrEmpty(inputFont))
            {
                return;
            }

            if (!File.Exists(inputFont))
            {
                Log.Debug($"File {inputFont} doesn't exist", this);
                return;
            }

            GraphicsPath gp = new GraphicsPath();
            Graphics gfx = Graphics.FromImage(new Bitmap(1, 1));
            SizeF stringSize = new SizeF();
            T3.Core.DataTypes.Point point;
            float cursor = 0f;

            PrivateFontCollection collection = new PrivateFontCollection();
            collection.AddFontFile(inputFont);


            // FontFamily fontFamily = new FontFamily(@"D:\Projects\T3\t3\Resources\fonts\TeknoTwo.ttf");
            FontFamily fontFamily = collection.Families[0];
            // return;
            Log.Debug($"'{fontFamily.Name}");

            using (Font f = new Font(fontFamily.Name, 1f))
            {
                gp.AddString(inputText, fontFamily, 0, 1f, new System.Drawing.Point(0, 0), StringFormat.GenericDefault);
                int count = gp.PathPoints.Length + inputText.Length * 2; // segments + close loop + separator
                int index = 0;
                StructuredList<T3.Core.DataTypes.Point> list = new StructuredList<T3.Core.DataTypes.Point>(count);
                gp.Reset();
                for (int c = 0; c < inputText.Length; c++)
                {
                    string character = inputText[c].ToString();
                    gp.AddString(character, fontFamily, 0, 1f, new System.Drawing.Point(0, 0), StringFormat.GenericDefault);
                    gp.Flatten(new Matrix(), 0.01f);  // <<== *
                    for (int i = 0; i < gp.PathPoints.Length + 1; i++)
                    {
                        PointF p = gp.PathPoints[i % gp.PathPoints.Length];
                        point = new T3.Core.DataTypes.Point();
                        Vector3 pos = new Vector3(p.X + cursor, 1 - p.Y, 0f);
                        point.Position = pos;
                        point.W = 1f;
                        point.Orientation = new Quaternion(0f, 0f, 0f, 1f);
                        point.Color = new Vector4(1f, 1f, 1f, 1f);
                        list.TypedElements[index++] = point;
                    }
                    stringSize = gfx.MeasureString(character, f);
                    cursor += stringSize.Width;
                    list.TypedElements[index++] = T3.Core.DataTypes.Point.Separator();
                    gp.Reset();
                }
                OutputList.Value = list;
            }
        }

        [Output(Guid = "c65da6e8-3eb7-4152-9b79-34fcaaa31807")]
        public readonly Slot<T3.Core.DataTypes.StructuredList> OutputList = new Slot<T3.Core.DataTypes.StructuredList>();
    }
}

