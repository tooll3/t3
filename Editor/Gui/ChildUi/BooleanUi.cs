using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using T3.Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4;
using Icon = T3.Editor.Gui.Styling.Icon;
using String = T3.Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4.Boolean;


namespace T3.Editor.Gui.ChildUi
{


    public static class BooleanUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {

            //we get the parameters from the operator
            if (!(instance is String stringInstance))
                return SymbolChildUi.CustomUiResult.None;

            var v = stringInstance.True.TypedInputValue.Value;
            var w = stringInstance.False.TypedInputValue.Value;
            var color = stringInstance.RGBA.TypedInputValue.Value;
           
            /*if (string.IsNullOrEmpty(v))
            {
                //v = "on";

                v = "on";
              
            }

            if (string.IsNullOrEmpty(w))
            {
                w = "off";
            }*/
            // end of getting the strings

            if (!(instance is Boolean boolean)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            screenRect.Expand(-4);
            ImGui.SetCursorScreenPos(screenRect.Min);
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            var refValue = boolean.BoolValue.Value;
            var label = string.IsNullOrEmpty(symbolChild.Name)
                            ? refValue ? "" : ""
                            : symbolChild.ReadableName;// we reference here to show correct state when connected
            
            //we use the 
            if (CustomComponents.ToggleButtonB($"{label}{(refValue ? $" {v}" : $" {w}")}", ref refValue, new Vector2((screenRect.Max.X - screenRect.Min.X) + 20, screenRect.Max.Y - screenRect.Min.Y), color))
            {
               OnClickBehavior(ref refValue);
            }
            

           // ImGui.SameLine();
           

            // Calculate checkbox size and position
            var checkboxSize = new Vector2(20, 20);
            var checkboxPos = screenRect.Min  + new Vector2(4, ((screenRect.GetHeight() - checkboxSize.Y) / 2)-2);
            //var checkboxPos =  new Vector2(0,0);

            // Draw the checkbox
            ImGui.SetCursorScreenPos(checkboxPos);

            if (ImGui.Checkbox("", ref refValue))
            {
                OnClickBehavior(ref refValue);
            }

            void OnClickBehavior(ref bool refValue)
            {
                if (!boolean.BoolValue.IsConnected)
                {
                    boolean.BoolValue.TypedInputValue.Value = !boolean.BoolValue.TypedInputValue.Value;
                }

                boolean.BoolValue.Input.IsDefault = false;
                boolean.BoolValue.DirtyFlag.Invalidate();
            }

            
            //ImGui.TextUnformatted(label);
            ImGui.PopClipRect();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}