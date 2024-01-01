using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;

namespace Operator.User.cynic.Editor;

public class UiRegistration : IOperatorUIInitializer
{
    public void Initialize()
    {
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05.GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_bfe540ef_f8ad_45a2_b557_cd419d9c8e44.DataList), DataListUi.DrawChildUi);
    }
}