using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator.Slots;
using T3.Core.DataTypes;
using T3.Gui;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.OutputUi
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

        private static List<List<float>> _previousValues;
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Dict<float>> typedSlot)
            {
                ImGui.BeginChild("##scrolling");

                var v = typedSlot.Value;
                var outputString = v == null ? "" : string.Join(", ", $"{v:0.000}");
                ImGui.TextUnformatted($"{outputString}");

                if (v == null)
                    return;

                var count = Math.Min(1024, v.Count);
                var horizontalLength = 512;

                if (_clear)
                    _previousValues = null;

                // expand list of previous values if necessary
                if (_previousValues == null)
                    _previousValues = new List<List<float>>();
                if (count > _previousValues.Count)
                {
                    while (_previousValues.Count < count)
                    {
                        // and new list and initialize to zeroes
                        var newList = Enumerable.Repeat(0f, horizontalLength).ToList();
                        _previousValues.Add(newList);
                    }
                }

                var valueEnum = v.Values.GetEnumerator();
                for (var i = 0; i < count; i++)
                {
                    valueEnum.MoveNext();
                    _previousValues[i].Add(valueEnum.Current);

                    // keep only a certain number of samples
                    while (_previousValues[i].Count > horizontalLength)
                        _previousValues[i].RemoveAt(0);
                }


                var keyEnum = v.Keys.GetEnumerator();
                valueEnum = v.Values.GetEnumerator();
                for (var i = 0; i < _previousValues.Count; i++)
                {
                    keyEnum.MoveNext();
                    valueEnum.MoveNext();

                    // print general user interface?
                    if (i == 0)
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

                    // set plot color, repeating every 10 colors
                    var hue = ((float)i * 360.0f/10.0f) / 360.0f;
                    hue -= (float) Math.Floor(hue);
                    var saturation = 0.5f;
                    var value = 0.75f;
                    Vector4 plotColor = new Vector4(1f);
                    ImGui.ColorConvertHSVtoRGB(hue, saturation, value,
                                               out plotColor.X, out plotColor.Y, out plotColor.Z);
                    ImGui.PushStyleColor(ImGuiCol.PlotLines, plotColor);

                    var floatArray = _previousValues[i].ToArray();
                    if (_autoFit)
                    {
                        ImGui.PlotLines("##values", ref floatArray[0], _previousValues[i].Count,
                                        0,
                                        keyEnum.Current);
                    }
                    else
                    {
                        ImGui.PlotLines("##values", ref floatArray[0],
                                        _previousValues[i].Count,
                                        0,
                                        keyEnum.Current,
                                        _minFit,
                                        _maxFit);
                    }

                    ImGui.SameLine();
                    ImGui.TextUnformatted($"{valueEnum.Current:0.000}");

                    var textColor = new Vector4(T3Style.Colors.TextMuted.R,
                                                T3Style.Colors.TextMuted.G,
                                                T3Style.Colors.TextMuted.B,
                                                T3Style.Colors.TextMuted.A);
                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);

                    if (_autoFit && _previousValues[i].Count > 0)
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
                                              $"  avg {(sum / _previousValues[i].Count):0.000}");
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