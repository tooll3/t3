using ImGuiNET;
using lib.io.data;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
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

            drawList.AddRectFilled(innerRect.Min, innerRect.Max, UiColors.Gray);
            
            var list = dataList.InputList.Value;
            if (list == null)
            {
                return DefaultResult;
            }
            
            ImGui.SetCursorScreenPos(innerRect.Min);
            
            var modified =global::T3.Editor.Gui.TableView.TableList.Draw(list, innerRect.GetSize());
            if (modified)
            {
                dataList.
                    InputList.DirtyFlag.Invalidate();
                dataList.Result.DirtyFlag.Invalidate();
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