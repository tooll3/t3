#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi;

public static class CustomChildUiRegistry
{
    private static readonly ConcurrentDictionary<Type, DrawChildUiDelegate> EntriesRw = new();

    public static void Register(Type type, DrawChildUiDelegate drawChildUiDelegate, ICollection<Type> types)
    {
        if(EntriesRw.TryAdd(type, drawChildUiDelegate))
            types.Add(type);
    }

    internal static bool TryGetValue(Type type, [NotNullWhen(true)] out DrawChildUiDelegate? o) => EntriesRw.TryGetValue(type, out o);

    public static bool Remove(Type symbolInstanceType) => EntriesRw.TryRemove(symbolInstanceType, out var _);
}

public delegate SymbolUi.Child.CustomUiResult DrawChildUiDelegate(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 scale);