using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Selection;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal static class Modifications
{
    /// <summary>
    /// Deletes the selected items and tries to collapse the gaps and patches the connection gaps is possible
    /// </summary>
    /// <param name="context"></param>
    internal static void DeleteSelectedOps(GraphUiContext context)
    {
        if (context.Selector.Selection.Count == 0)
            return;

        if(!SymbolUiRegistry.TryGetSymbolUi(context.CompositionOp.Symbol.Id, out var compositionUi))
        {
            Log.Warning("Can't find composition ui?");
            return;
        }

        var deletedItems = new List<MagGraphItem>();
        var deletedChildUis = new List<SymbolUi.Child>();
        foreach (var s in context.Selector.Selection)
        {
            if(!context.Layout.Items.TryGetValue(s.Id, out var item))
            {
                Log.Warning("Can't find selectable item " + s);
                continue;
            }

            if (item.Variant != MagGraphItem.Variants.Operator)
            {
                Log.Debug("Sorry, deleting outputs and inputs is not yet supported");
                continue;
            }

            if (!compositionUi.ChildUis.TryGetValue(item.Id, out var childUi))
            {
                Log.Warning("Can't find symbol child for " + item);
                continue;
            }

            //uiItems.Add(new ItemWithChildUi(item, childUi));
            deletedItems.Add(item);
            deletedChildUis.Add(childUi);

        }

        // find obsolete connections...
        var obsoleteConnections = new HashSet<MagGraphConnection>();
        foreach (var i in deletedItems)
        {
            foreach (var inLine in i.InputLines)
            {
                if (inLine.ConnectionIn != null)
                    obsoleteConnections.Add(inLine.ConnectionIn);
            }

            foreach (var outLine in i.OutputLines)
            {
                obsoleteConnections.UnionWith(outLine.ConnectionsOut);
            }
        }
        
        if (deletedChildUis.Count == 0)
            return;
        
        var collapsableConnectionPairs= MagItemMovement.FindLinkedVerticalCollapsableConnectionPairs(obsoleteConnections.ToList());
        var macroCommand = new MacroCommand("Delete items");

        // Delete items...
        macroCommand.AddAndExecCommand(new DeleteSymbolChildrenCommand(compositionUi, deletedChildUis));

        // Collapse vertical gaps
        var relevantItems = MagItemMovement.CollectSnappedItems(deletedItems);
        var movableItems = new HashSet<MagGraphItem>(relevantItems.Except(deletedItems));

        foreach (var pair in collapsableConnectionPairs)
        {
            var mci = pair.Ca;
            var mco = pair.Cb;
            var affectedItems = MagItemMovement.MoveToCollapseGaps(mci, mco, movableItems, true);
            if (affectedItems.Count == 0)
                continue;
            
            var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
            var newMoveCommand = new ModifyCanvasElementsCommand(context.CompositionOp.Symbol.Id, affectedItemsAsNodes, context.Selector);
            macroCommand.AddExecutedCommandForUndo(newMoveCommand);
        
            MagItemMovement.MoveToCollapseGaps(mci, mco, movableItems, dryRun:false);
            
            newMoveCommand.StoreCurrentValues();
        
            macroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                    new Symbol.Connection(mci.SourceItem.Id,
                                                                                              mci.SourceOutput.Id,
                                                                                              mco.TargetItem.Id,
                                                                                              mco.TargetInput.Id),
                                                                    0));                            
        }
        
        UndoRedoStack.Add(macroCommand);
        
        context.Layout.FlagAsChanged();
        context.Selector.Clear();
    }

    //private record ItemWithChildUi(MagGraphItem Item, SymbolUi.Child UiChild);
}