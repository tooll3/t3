// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;

namespace T3.Core.Animation.Curve
{
    public class VDefinition
    {
        public enum Interpolation
        {
            Constant = 0,
            Linear,
            Spline,
        };

        public enum EditMode
        {
            Linear = 0,
            Smooth,
            Horizontal,
            Tangent,
            Constant,
            Cubic,
        }

        public double U { get; set; }
        public double Value { get; set; }
        public double Tension { get; set; }
        public double Bias { get; set; }
        public double Continuity { get; set; }
        public Interpolation InType { get; set; }
        public Interpolation OutType { get; set; }

        public EditMode InEditMode { get; set; }
        public EditMode OutEditMode { get; set; }

        public double InTangentAngle { get; set; }
        public double OutTangentAngle { get; set; }
        public bool Weighted { get; set; }
        public bool BrokenTangents { get; set; }

        public VDefinition()
        {
            Value = 0.0;
            Tension = 0.0;
            Bias = 0.0;
            Continuity = 0.0;
            InType = Interpolation.Linear;
            OutType = Interpolation.Linear;
            InTangentAngle = 0.0;
            OutTangentAngle = 0.0;
        }

        public VDefinition Clone()
        {
            return new VDefinition()
            {
                Value = Value,
                Tension = Tension,
                Bias = Bias,
                Continuity = Continuity,
                InType = InType,
                OutType = OutType,
                InEditMode = InEditMode,
                OutEditMode = OutEditMode,
                InTangentAngle = InTangentAngle,
                OutTangentAngle = OutTangentAngle
            };
        }

        //public void Read(JToken jsonV)
        //{
        //    Value = jsonV["Value"].Value<double>();
        //    Tension = jsonV["Tension"].Value<double>();
        //    Bias = jsonV["Bias"].Value<double>();
        //    Continuity = jsonV["Continuity"].Value<double>();
        //    InType = (Interpolation)Enum.Parse(typeof(Interpolation), jsonV["InType"].Value<string>());
        //    OutType = (Interpolation)Enum.Parse(typeof(Interpolation), jsonV["OutType"].Value<string>());

        //    InTangentAngle = jsonV.Value<double>("InTangentAngle");
        //    OutTangentAngle = jsonV.Value<double>("OutTangentAngle");

        //    InEditMode = (EditMode)Enum.Parse(typeof(EditMode), jsonV["InEditMode"].Value<string>());
        //    OutEditMode = (EditMode)Enum.Parse(typeof(EditMode), jsonV["OutEditMode"].Value<string>());
        //}

        //public void Write(JsonTextWriter writer)
        //{
        //    writer.WriteValue("Value", Value);
        //    writer.WriteValue("Tension", Tension);
        //    writer.WriteValue("Bias", Bias);
        //    writer.WriteValue("Continuity", Continuity);
        //    writer.WriteValue("InType", InType);
        //    writer.WriteValue("OutType", OutType);
        //    writer.WriteValue("InEditMode", InEditMode);
        //    writer.WriteValue("OutEditMode", OutEditMode);
        //    writer.WriteValue("InTangentAngle", InTangentAngle);
        //    writer.WriteValue("OutTangentAngle", OutTangentAngle);
        //}

        public static void AngleLengthToTCB(double angle, double length,
                                            out double tension, out double continuity, out double bias)
        {
            tension = 0;
            continuity = 0;
            bias = 0;
        }

        public static void TCBToAngleLength(double tension, double continuity, double bias,
                                            out double angle, out double length)
        {
            angle = 0;
            length = 0;
        }
    };

}