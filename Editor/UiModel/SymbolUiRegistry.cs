#nullable enable
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Windows;

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

    public static SymbolChildUi? GetSymbolChildUi(this SymbolChild symbolChild)
    {
        var parent = symbolChild.Parent;
        if (parent == null)
            return null;
        
        return GetSymbolChildUiWithId(parent.GetSymbolUi(), symbolChild.Id);
    }
    
    public static SymbolChildUi? GetSymbolChildUi(this Instance instance)
    {
        var parent = instance.Parent;
        if (parent == null)
            return null;
        
        return GetSymbolChildUiWithId(parent.GetSymbolUi(), instance.SymbolChildId); 
    }

    public static SymbolChildUi? GetSymbolChildUiWithId(this Instance instance, Guid id, bool allowNull = false)
    {
        var symbolUi = instance.GetSymbolUi();
        return symbolUi.GetSymbolChildUiWithId(id);
    }
    
    // todo - simplify lookups with a dictionary or something
    public static SymbolChildUi? GetSymbolChildUiWithId(this SymbolUi symbolUi, Guid id, bool allowNull = true)
    {
        return allowNull ? symbolUi.ChildUis.SingleOrDefault(child => child.Id == id) 
                   : symbolUi.ChildUis.Single(child => child.Id == id);
    }
    
    public static Instance GetChildInstanceWith(this Instance instance, SymbolChild child) => GetChildInstanceWithId(instance, child.Id);

    public static Instance? GetChildInstanceWithId(this Instance instance, Guid id, bool allowNull = false)
    {
        return allowNull ? instance.Children.SingleOrDefault(child => child.SymbolChildId == id) 
                   : instance.Children.Single(child => child.SymbolChildId == id);
    }

    public static bool TryGetChildInstance(this Instance instance, Guid id, bool recursive, out Instance? child, out List<Guid>? pathFromRoot)
    {
        child = instance.Children.SingleOrDefault(child => child.SymbolChildId == id);
        var success = child != null;

        if (success)
        {
            pathFromRoot = OperatorUtils.BuildIdPathForInstance(child);
            return true;
        }

        if (recursive)
        {
            foreach (var childInstance in instance.Children)
            {
                if (TryGetChildInstance(childInstance, id, true, out child, out pathFromRoot))
                    return true;
            }
        }

        pathFromRoot = null;
        return false;
    }

    public static bool TryGetValue(Guid rSymbolId, out SymbolUi? symbolUi)
    {
        foreach(var package in EditorSymbolPackage.AllPackages)
        {
            if (package.TryGetSymbolUi(rSymbolId, out symbolUi))
                return true;
        }
        symbolUi = null;
        return false;
    }
}