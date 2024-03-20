using System.Collections.Concurrent;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi
{
    public static class CustomChildUiRegistry
    {
        public static IReadOnlyDictionary<Type, DrawChildUiDelegate> Entries => EntriesRw;
        internal static readonly ConcurrentDictionary<Type, DrawChildUiDelegate> EntriesRw = new();
        
        public static void Register(Type type, DrawChildUiDelegate drawChildUiDelegate)
        {
            EntriesRw.TryAdd(type, drawChildUiDelegate);
        }
    }

    public delegate SymbolChildUi.CustomUiResult DrawChildUiDelegate(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 scale);
}