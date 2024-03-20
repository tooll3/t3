using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.OutputUi
{
    public class FloatDictOutputUi : OutputUi<Dict<float>>
    {
        public override IOutputUi Clone()
        {
            return new FloatDictOutputUi()
            {
                OutputDefinition = OutputDefinition,
                PosOnCanvas = PosOnCanvas,
                Size = Size
            };
        }

        private static bool _autoFit = true;
        private static bool _clear = false;
        private static float _minFit = -1;
        private static float _maxFit = 1;

        private static List<List<float>> _previousChannelValues;
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Dict<float>> typedSlot)
            {
                var floatDict = typedSlot.Value;
                if (floatDict == null)
                    return;
                
                var outputString = string.Join(", ", $"{floatDict:0.000}");
                ImGui.BeginChild("##scrolling");
                ImGui.TextUnformatted($"{outputString}");


                var count = Math.Min(1024, floatDict.Count);
                const int horizontalLength = 512;

                if (_clear)
                    _previousChannelValues = null;

                
                if(_clear || _previousChannelValues == null)
                    _previousChannelValues = new List<List<float>>();
                
                // Expand list of previous values if necessary
                if (count > _previousChannelValues.Count)
                {
                    while (_previousChannelValues.Count < count)
                    {
                        // And new list and initialize to zeroes
                        var newList = Enumerable.Repeat(0f, horizontalLength).ToList();
                        _previousChannelValues.Add(newList);
                    }
                }

                var valueEnum = floatDict.Values.GetEnumerator();
                for (var i = 0; i < count; i++)
                {
                    valueEnum.MoveNext();
                    _previousChannelValues[i].Add(valueEnum.Current);

                    // Keep only a certain number of samples
                    while (_previousChannelValues[i].Count > horizontalLength)
                        _previousChannelValues[i].RemoveAt(0);
                }


                var keyEnum = floatDict.Keys.GetEnumerator();
                valueEnum = floatDict.Values.GetEnumerator();
                
                // Print general user interface
                if (_previousChannelValues.Count > 0)
                {
                    ImGui.Checkbox("Auto Fit", ref _autoFit);
                    ImGui.SameLine();
                    _clear  = ImGui.Button("Clear");
                    if (!_autoFit)
                    {
                        ImGui.DragFloat("Max", ref _maxFit);
                        ImGui.DragFloat("Min", ref _minFit);
                    }
                }
                
                for (var channelIndex = 0; channelIndex < _previousChannelValues.Count; channelIndex++)
                {
                    keyEnum.MoveNext();
                    valueEnum.MoveNext();

                    // Set plot color, repeating every 10 colors
                    var hue = ((float)channelIndex * 360.0f/10.0f) / 360.0f;
                    hue -= (float) Math.Floor(hue);
                    const float saturation = 0.5f;
                    const float value = 0.75f;
                    var plotColor = new Vector4(1f);
                    
                    ImGui.ColorConvertHSVtoRGB(hue, saturation, value,
                                               out plotColor.X, out plotColor.Y, out plotColor.Z);
                    ImGui.PushStyleColor(ImGuiCol.PlotLines, plotColor);

                    var floatArray = _previousChannelValues[channelIndex].ToArray();
                    if (_autoFit)
                    {
                        ImGui.PlotLines("##values", ref floatArray[0], _previousChannelValues[channelIndex].Count,
                                        0,
                                        keyEnum.Current);
                    }
                    else
                    {
                        ImGui.PlotLines("##values", ref floatArray[0],
                                        _previousChannelValues[channelIndex].Count,
                                        0,
                                        keyEnum.Current,
                                        _minFit,
                                        _maxFit);
                    }

                    ImGui.SameLine();
                    ImGui.TextUnformatted($"{valueEnum.Current:0.000}");

                    var textColor = new Vector4(UiColors.TextMuted.R,
                                                UiColors.TextMuted.G,
                                                UiColors.TextMuted.B,
                                                UiColors.TextMuted.A);
                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);

                    if (_autoFit && _previousChannelValues[channelIndex].Count > 0)
                    {
                        var min = float.PositiveInfinity;
                        var max = float.NegativeInfinity;
                        var sum = 0f;
                        foreach (var number in floatArray)
                        {
                            sum += number;
                            min = Math.Min(min, number);
                            max = Math.Max(max, number);
                        }

                        ImGui.SameLine();
                        ImGui.TextUnformatted($"  [{min:G3} .. {max:G3}]" +
                                              $"  avg {(sum / _previousChannelValues[channelIndex].Count):0.000}");
                    }

                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                }

                ImGui.EndChild();
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}