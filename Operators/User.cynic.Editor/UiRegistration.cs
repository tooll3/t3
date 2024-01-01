using Operators.User.cynic.research;
using Operators.User.cynic.research.data;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;

namespace Operator.User.cynic.Editor;

public class UiRegistration : IOperatorUIInitializer
{
    public void Initialize()
    {
        CustomChildUiRegistry.Entries.Add(typeof(GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(DataList), DataListUi.DrawChildUi);
    }
}