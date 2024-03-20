using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.OutputUi
{
    public class FloatListOutputUi : OutputUi<List<float>>
    {
        public override IOutputUi Clone()
        {
            return new FloatListOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }

        private static bool _autoFit;
        private static float _minFit = -1;
        private static float _maxFit = 1;
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                var v = typedSlot.Value;

                if (v == null)
                    return;
                
                if (v.Count > 1)
                {
                    var length = Math.Min(1024, v.Count);
                    var floatList = v.GetRange(0, length).ToArray();
                    
                    ImGui.Checkbox("Auto Fit", ref _autoFit);
                    if (_autoFit)
                    {
                        ImGui.PlotLines("##values", ref floatList[0], length);
                    }
                    else
                    {
                        ImGui.PlotLines("##values", ref floatList[0], 
                                        length, 
                                        0, 
                                        "", 
                                        _minFit, 
                                        _maxFit, 
                                        new Vector2( ImGui.GetContentRegionAvail().X, 200));


                        FormInputs.AddFloat("Max", ref _maxFit);
                        FormInputs.AddFloat("Min", ref _minFit);
                    }
                }

                if (v.Count > 0)
                {
                    var min = float.PositiveInfinity;
                    var max = float.NegativeInfinity;
                    var sum = 0f;
                    foreach (var number in v)
                    {
                        sum += number;
                        min = Math.Min(min, number);
                        max = Math.Max(max, number);
                    }
                
                    ImGui.TextUnformatted($"{v.Count}  between {min:G5} .. {max:G5}  avg {sum/v.Count:G5}");
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}