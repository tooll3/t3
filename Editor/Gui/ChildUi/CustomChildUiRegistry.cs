#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi;

public static class CustomChildUiRegistry
{
    private static readonly ConcurrentDictionary<string, DrawChildUiDelegate> EntriesRw = new();

    public static void Register(Type type, DrawChildUiDelegate drawChildUiDelegate, ICollection<Type> types)
    {
        var name = type.FullName;
        if(name == null)
            throw new ArgumentException("Type name cannot be null", nameof(type));
        
        if (EntriesRw.TryAdd(name, drawChildUiDelegate))
        {
            types.Add(type);
            Log.Debug("Registered custom child UI for type: " + type);
        }
    }

    internal static bool TryGetValue(Type type, [NotNullWhen(true)] out DrawChildUiDelegate? o)
    {
        var name = type.FullName;
        if (name == null)
            throw new ArgumentException("Type name cannot be null", nameof(type));
        return EntriesRw.TryGetValue(name, out o);
    }

    public static bool Remove(Type symbolInstanceType)
    {
        var name = symbolInstanceType.FullName;
        
        if (name == null)
            throw new ArgumentException("Type name cannot be null", nameof(symbolInstanceType));
        
        return EntriesRw.TryRemove(name, out var _);
    }
}

public delegate SymbolUi.Child.CustomUiResult DrawChildUiDelegate(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 scale);