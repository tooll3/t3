using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.InputUi.SingleControlInputs
{
    public class Vector3InputUi : SingleControlInputUi<Vector3>
    {
        public override bool IsAnimatable => true;
        
        protected override bool DrawSingleEditControl(string name, ref Vector3 value)
        {
            return ImGui.DragFloat3("##Vector3Edit", ref value, Scale, Min, Max);
        }

        protected override void DrawValueDisplay(string name, ref Vector3 value)
        {
            DrawEditControl(name, ref value);
        }
        
        protected override void DrawAnimatedValue(string name, InputSlot<Vector3> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < 3)
            {
                DrawValueDisplay(name, ref inputSlot.Value);
                return;
            }

            SharpDX.Vector3 value = new SharpDX.Vector3((float)curves[0].GetSampledValue(time),
                                                        (float)curves[1].GetSampledValue(time),
                                                        (float)curves[2].GetSampledValue(time));
            Vector3 editValue = new Vector3(value.X, value.Y, value.Z);
            var edited = DrawSingleEditControl(name, ref editValue);
            if (!edited)
                return; // nothing changed

            SharpDX.Vector3 newValue = new SharpDX.Vector3(editValue.X, editValue.Y, editValue.Z);
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(newValue[i] - value[i]) > Single.Epsilon)
                {
                    var key = curves[i].GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };
                    key.Value = newValue[i];
                    curves[i].AddOrUpdateV(time, key);
                }
            }
        }
        
        public override void DrawSettings()
        {
            base.DrawSettings();

            ImGui.DragFloat("Min", ref Min);
            ImGui.DragFloat("Max", ref Max);
            ImGui.DragFloat("Scale", ref Scale);
        }
        
        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            if (Min != DefaultMin)
                writer.WriteValue("Min", Min);
            
            if (Max != DefaultMax) 
                writer.WriteValue("Max", Max);
            
            if (Scale != DefaultScale) 
                writer.WriteValue("Scale",  Scale);
        }
        
        private float Min = DefaultMin;
        private float Max = DefaultMax;
        private float Scale = DefaultScale;
        
        private const float DefaultScale = 0.01f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;
    }
}