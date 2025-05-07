#nullable enable
using System.Diagnostics.CodeAnalysis;
using T3.Core.Model;
using T3.Core.Operator;

namespace T3.Editor.UiModel;

internal static class SymbolUiRegistry
{
    public static SymbolUi GetSymbolUi(this Instance instance) => instance.Symbol.GetSymbolUi();

    public static SymbolUi GetSymbolUi(this Symbol symbol)
    {
        var id = symbol.Id;
        var package = (EditorSymbolPackage)symbol.SymbolPackage;
        if (package.TryGetSymbolUi(id, out var symbolUi))
            return symbolUi!;

        throw new Exception($"Can't find symbol ui for symbol {id}");
    }

    public static bool TryGetChildInstance(this Instance instance, Guid id, bool recursive, [NotNullWhen(true)] out Instance? child, [NotNullWhen(true)] out IReadOnlyList<Guid>? pathFromRoot)
    {
        if (instance.Children.TryGetValue(id, out child))
        {
            pathFromRoot = child.InstancePath;
            return true;
        }

        if (recursive)
        {
            foreach (var childInstance in instance.Children.Values)
            {
                if (TryGetChildInstance(childInstance, id, true, out child, out pathFromRoot))
                    return true;
            }
        }

        pathFromRoot = null;
        return false;
    }

    public static bool TryGetSymbolUi(Guid symbolId, [NotNullWhen(true)] out SymbolUi? symbolUi)
    {
        foreach(var package in SymbolPackage.AllPackages)
        {
            if (package is not EditorSymbolPackage symbolPackage)
                continue;
            
            if (symbolPackage.TryGetSymbolUi(symbolId, out symbolUi))
                return true;
        }
        symbolUi = null;
        return false;
    }
    
    public static SymbolUi.Child? GetChildUi(this Instance instance) => instance.SymbolChild?.GetChildUi();
    public static SymbolUi.Child GetChildUi(this Symbol.Child symbolChild) => symbolChild.Parent!.GetSymbolUi().ChildUis[symbolChild.Id];
}