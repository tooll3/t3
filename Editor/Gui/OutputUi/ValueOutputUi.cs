using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.OutputUi;

internal sealed class ValueOutputUi<T> : OutputUi<T>
{
    public override IOutputUi Clone()
    {
        return new ValueOutputUi<T>
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
    }

    protected override void DrawTypedValue(ISlot slot)
    {
        if (slot is Slot<T> typedSlot)
        {
            var value = typedSlot.Value;
            switch (value)
            {
                case float f:
                    ImGui.Value("", f);
                    break;
                case string s:
                    ImGui.BeginChild("scrollable");
                    ImGui.TextUnformatted(s);
                    ImGui.EndChild();
                    break;
                default:
                    var type = typeof(T);
                    ImGui.PushFont(Fonts.FontSmall);
                        
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Fade(0.6f).Rgba);
                    ImGui.TextUnformatted(type.Namespace);
                    ImGui.PopStyleColor();
                        
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    ImGui.TextUnformatted(type.Name);
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                        
                    ImGui.Dummy(new Vector2(0,2 * T3Ui.UiScaleFactor));

                    var valueAsString = value == null ? "undefined" : value.ToString();
                    ImGui.TextUnformatted(valueAsString);
                    break;
            }
        }
        else
        {
            Debug.Assert(false);
        }
    }
}