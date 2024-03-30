using System.Numerics;
using ImGuiNET;
using lib.data;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class DataListUi
    {
        public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect, Vector2 canvasScale)
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
        
        private const SymbolUi.Child.CustomUiResult  DefaultResult =             SymbolUi.Child.CustomUiResult.Rendered
                                                                        | SymbolUi.Child.CustomUiResult.PreventTooltip
                                                                        | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph
                                                                        | SymbolUi.Child.CustomUiResult.PreventInputLabels
                                                                        | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp;
    }
}