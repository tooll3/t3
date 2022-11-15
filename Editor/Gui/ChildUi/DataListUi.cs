using ImGuiNET;
using T3.Core.Operator;
using T3.Operators.Types.Id_bfe540ef_f8ad_45a2_b557_cd419d9c8e44;
using UiHelpers;

namespace Editor.Gui.ChildUi
{
    public static class DataListUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is DataList dataList)
                || !ImGui.IsRectVisible(selectableScreenRect.Min, selectableScreenRect.Max))
                return DefaultResult;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-7);
            if (innerRect.GetHeight() < 1)
                return DefaultResult;

            drawList.AddRectFilled(innerRect.Min, innerRect.Max, Color.DarkGray);
            
            var list = dataList.InputList.Value;
            if (list == null)
            {
                return DefaultResult;
            }
            
            ImGui.SetCursorScreenPos(innerRect.Min);
            
            var modified =TableView.TableList.Draw(list, innerRect.GetSize());
            if (modified)
            {
                dataList.InputList.DirtyFlag.Invalidate();
            }

            return DefaultResult;
        }
        
        private const SymbolChildUi.CustomUiResult  DefaultResult =             SymbolChildUi.CustomUiResult.Rendered
                                                                        | SymbolChildUi.CustomUiResult.PreventTooltip
                                                                        | SymbolChildUi.CustomUiResult.PreventOpenSubGraph
                                                                        | SymbolChildUi.CustomUiResult.PreventInputLabels
                                                                        | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp;
    }
}