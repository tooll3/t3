using System;
using System.Linq;
using ImGuiNET;
using Lib.math.@float;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Types;
using Types.Values;
using Vector2 = System.Numerics.Vector2;

namespace libEditor.CustomUi;

public static class ValueUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 canvasScale)
    {
        if (!(instance is Value valueInstance))
            return SymbolUi.Child.CustomUiResult.None;

        var dragWidth = WidgetElements.DrawDragIndicator(area, drawList, canvasScale);
        var usableArea = area;
        area.Min.X += dragWidth;

        drawList.AddRectFilled(area.Min, area.Max, UiColors.BackgroundFull.Fade(0.1f));

        var symbolChild = instance.SymbolChild;
        drawList.PushClipRect(area.Min, area.Max, true);
            
        var isAnimated = instance.Parent?.Symbol.Animator.IsInputSlotAnimated(valueInstance.Float)??false;
            
        var value = (isAnimated || valueInstance.Float.HasInputConnections) 
                        ? (double)valueInstance.Float.Value 
                        :(double)valueInstance.Float.TypedInputValue.Value;
            
        // Draw slider
        const float rangeMin = 0f;
        const float rangeMax = 1f;
            
        if (MathF.Abs(rangeMax - rangeMin) > 0.0001f)
        {
            var f = MathUtils.NormalizeAndClamp((float)value, rangeMin, rangeMax);
            var w = (int)area.GetWidth() * f;
            drawList.AddRectFilled(area.Min, 
                                   new Vector2(area.Min.X + w, area.Max.Y),
                                   UiColors.WidgetSlider);
                
            drawList.AddRectFilled(new Vector2(area.Min.X + w, area.Min.Y), 
                                   new Vector2(area.Min.X + w + 1, area.Max.Y),
                                   UiColors.WidgetActiveLine);
        }
            
        // // Slider Range
        // if (rangeMin == 0 && rangeMax != 0)
        // {
        //     ValueLabel.Draw(drawList, area, new Vector2(1, 1), valueInstance.SliderMax);
        // }

        // Interaction
        {
            var editingUnlocked = ImGui.GetIO().KeyCtrl || _activeJogDialInputSlot != null;
            var inputSlot = valueInstance.Float;
            if (editingUnlocked)
            {
                ImGui.SetCursorScreenPos(area.Min);
                ImGui.InvisibleButton("button", area.GetSize());
                    
                if (ImGui.IsItemActivated() && ImGui.GetIO().KeyCtrl)
                {
                    _jogDialCenter = ImGui.GetIO().MousePos;
                    _activeJogDialInputSlot = inputSlot;
                    drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), UiColors.WidgetHighlight);
                }
                    
                if (_activeJogDialInputSlot == inputSlot)
                {
                    var restarted = ImGui.IsItemActivated();
                    if (ImGui.IsItemActive())
                    {
                        SingleValueEdit.DrawValueEditMethod(ref value,  restarted, _jogDialCenter,double.NegativeInfinity, double.PositiveInfinity, false, 0.025f);
                        inputSlot.SetTypedInputValue((float)value);
                    }
                    else
                    {
                        _activeJogDialInputSlot = null;
                    }
                }
            }
        }
            
        // Label if instance has title
        if (!string.IsNullOrEmpty(symbolChild.Name))
        {
            WidgetElements.DrawPrimaryTitle(drawList, area, symbolChild.Name, canvasScale);
            WidgetElements.DrawSmallValue(drawList, area, $"{value:0.000}", canvasScale);
        }
        else
        {
            WidgetElements.DrawPrimaryValue(drawList, area, $"{value:0.000}", canvasScale);
        }
            
        drawList.PopClipRect();
        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels
               | SymbolUi.Child.CustomUiResult.PreventTooltip;
    }

    private static Vector2 _jogDialCenter;
    private static InputSlot<float> _activeJogDialInputSlot;
}