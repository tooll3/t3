using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Serialization;

namespace T3.Core.Animation;

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

    /** Proposed modes for improved keyframe handling
     * This would require converting the original animations on import and calculating
     * default tangent weights.
     */
    public enum Mode
    {
        Linear,
        Constant,
        Horizontal,
        AutoClamped,
        Auto,
        FreeAligned,
        Free,
    }
    
    private double _u;
    public double U
    {
        get => _u; 
        set => _u = Math.Round(value, Curve.TIME_PRECISION);
    }
            
    public double Value { get; set; }
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
        InType = Interpolation.Linear;
        OutType = Interpolation.Linear;
        InEditMode = EditMode.Linear;
        OutEditMode = EditMode.Linear;
        InTangentAngle = 0.0;
        OutTangentAngle = 0.0;
    }

    public VDefinition Clone()
    {
        return new VDefinition()
                   {
                       Value = Value,
                       U = U,
                       InType = InType,
                       OutType = OutType,
                       InEditMode = InEditMode,
                       OutEditMode = OutEditMode,
                       InTangentAngle = InTangentAngle,
                       OutTangentAngle = OutTangentAngle
                   };
    }

    public void CopyValuesFrom(VDefinition def)
    {
        Value = def.Value;
        U = def.U;
        InType = def.InType;
        OutType = def.OutType;
        InEditMode = def.InEditMode;
        OutEditMode = def.OutEditMode;
        InTangentAngle = def.InTangentAngle;
        OutTangentAngle = def.OutTangentAngle;            
    }

    public void Read(JToken jsonV)
    {
        Value = jsonV["Value"].Value<double>();
        InType = (Interpolation)Enum.Parse(typeof(Interpolation), jsonV["InType"].Value<string>());
        OutType = (Interpolation)Enum.Parse(typeof(Interpolation), jsonV["OutType"].Value<string>());

        InTangentAngle = jsonV.Value<double>("InTangentAngle");
        OutTangentAngle = jsonV.Value<double>("OutTangentAngle");

        InEditMode = (EditMode)Enum.Parse(typeof(EditMode), jsonV["InEditMode"].Value<string>());
        OutEditMode = (EditMode)Enum.Parse(typeof(EditMode), jsonV["OutEditMode"].Value<string>());
    }

    public void Write(JsonTextWriter writer)
    {
        writer.WriteValue("Value", Value);
        writer.WriteObject("InType", InType);
        writer.WriteObject("OutType", OutType);
        writer.WriteObject("InEditMode", InEditMode);
        writer.WriteObject("OutEditMode", OutEditMode);
        writer.WriteValue("InTangentAngle", InTangentAngle);
        writer.WriteValue("OutTangentAngle", OutTangentAngle);
    }
};