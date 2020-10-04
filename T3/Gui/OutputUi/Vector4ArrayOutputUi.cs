using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Gui.OutputUi
{
    public class Vector4ArrayOutputUi : OutputUi<SharpDX.Vector4[]>
    {
        public override IOutputUi Clone()
        {
            return new Vector4ArrayOutputUi()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
        }
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<SharpDX.Vector4[]> typedSlot)
            {
                var v = typedSlot.Value;
                //var outputString =  v == null? "NULL" : string.Join(", ", v);
                //ImGui.Text($"{outputString}");

                if (v == null)
                    return;

                for (var index = 0; index < v.Length && index < 50; index++)
                {
                    var row = v[index];
                    ImGui.Text("" + row);
                }

                // if (v.Count > 3)
                // {
                //     var length = Math.Min(256, v.Count);
                //     var floatList = v.GetRange(0, length).ToArray();
                //     ImGui.PlotLines("##values", ref floatList[0], length);
                // }

                // if (v.Count > 0)
                // {
                //     var min = float.PositiveInfinity;
                //     var max = float.NegativeInfinity;
                //     var sum = 0f;
                //     foreach (var number in v)
                //     {
                //         sum += number;
                //         min = Math.Min(min, number);
                //         max = Math.Max(max, number);
                //     }
                //
                //     ImGui.Text($"{v.Count} [{min:G5} .. {max:G5}] ∅{sum/v.Count:G5}");
                // }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}