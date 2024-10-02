using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

public abstract class NodeNavigation
{
    public static void SelectAbove()
    {
        TryMoveSelectionTowards(Directions.Up);
    }
    
    public static void SelectBelow()
    {
        TryMoveSelectionTowards(Directions.Down);
    }
    
    public static void SelectLeft()
    {
        TryMoveSelectionTowards(Directions.Left);
    }

    public static void SelectRight()
    {
        TryMoveSelectionTowards(Directions.Right);
    }
    
    
    private static void TryMoveSelectionTowards(Directions direction)
    {
        var currentInstance = NavigationHistory.GetLastSelectedInstance();

        var composition = currentInstance?.Parent;
        if (composition == null)
            return;

        var symbolUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
        var currentSymbolChildUi = symbolUi.ChildUis.Single(c => c.Id == currentInstance.SymbolChildId);
        
        // Search all children
        SymbolChildUi bestMatch = null;
        var bestRelevancy = float.PositiveInfinity;
        foreach (var otherChildUi in symbolUi.ChildUis)
        {
            var alignedDelta = GetAlignedDelta(direction, otherChildUi.PosOnCanvas - currentSymbolChildUi.PosOnCanvas);

            if (otherChildUi == currentSymbolChildUi)
            {
                continue;
            }
            
            if (alignedDelta.X <= 0)
                continue;

            var r = alignedDelta.X + alignedDelta.Y * 5;
            if (r > bestRelevancy)
                continue;
            
            bestMatch = otherChildUi;
            bestRelevancy = r;
        }

        if (bestMatch == null)
        {
            return;
        }

        var bestInstance = currentInstance.Parent.Children.Single(c => c.SymbolChildId == bestMatch.Id);
        if (bestInstance == null)
        {
            Debug.Assert(false);
        }

        //Log.Debug($"Found with relevancy {bestRelevancy}: " + Structure.GetReadableInstancePath( OperatorUtils.BuildIdPathForInstance( bestInstance)), bestInstance);
        
        var path = OperatorUtils.BuildIdPathForInstance(bestInstance);
        if (Structure.GetInstanceFromIdPath(path) == null)
            return;
            
        GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(path);

        if (!ParameterWindow.IsAnyInstanceVisible())
        {
            ParameterPopUp.Open(bestInstance);
        }
    }

    private static Vector2 GetAlignedDelta(Directions direction, Vector2 deltaOnCanvas)
    {
        return direction switch
                   {
                       Directions.Up    => new Vector2(-deltaOnCanvas.Y, MathF.Abs(deltaOnCanvas.X)),
                       Directions.Right => new Vector2(deltaOnCanvas.X, MathF.Abs(deltaOnCanvas.Y)),
                       Directions.Down  => new Vector2(deltaOnCanvas.Y, MathF.Abs(deltaOnCanvas.X)),
                       Directions.Left  => new Vector2(-deltaOnCanvas.X, MathF.Abs(deltaOnCanvas.Y)),
                       _                => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                   };
    }
    
    
    private enum Directions
    {
        Up,
        Right,
        Down,
        Left,
    }
    
    
    
}