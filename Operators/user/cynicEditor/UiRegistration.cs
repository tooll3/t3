using Operators.user.cynic.research;
using Operators.user.cynic.research.data;
using T3.Core.Logging;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;

namespace Operator.User.cynic.Editor;

public class UiRegistration : IOperatorUIInitializer
{
    // ReSharper disable once EmptyConstructor
    public UiRegistration()
    {
    }

    public void Initialize()
    {
        CustomChildUiRegistry.Entries.Add(typeof(GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(DataList), DataListUi.DrawChildUi);
        Log.Debug("Registered UI entries. Total: {0}", CustomChildUiRegistry.Entries.Count);
    }
}