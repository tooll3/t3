using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi;
using T3.Editor.UiModel;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable SuggestBaseTypeForParameter

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

/// <summary>
/// Generate considerations:
/// - The layout model is a temporary data that is completely be generated from the t3ui data.
/// - Its computation is expensive and should only be done if required.
/// - This might be especially complicated if the state cannot be stored in the layout model, because it
///   relies on a temp. property like expanding a parameter type group.
/// </summary>
public class SnapGraphLayout
{
    public void ComputeLayout(Instance composition)
    {
        if (!SymbolUiRegistry.Entries.TryGetValue(composition.Symbol.Id, out var parentSymbolUi))
            return;

        var parentSymbol = composition.Symbol;

        if (HasCompositionDataChanged(parentSymbol, ref _compositionModelHash))
        {
             CollectItemReferences(composition, parentSymbolUi);
             CollectConnectionReferences(composition);
             UpdateVisibleItemLines();
        }

        UpdateConnectionLayout();
    }

    /// <remarks>
    /// This method is extremely slow for large compositions...
    /// </remarks>
    private void CollectItemReferences(Instance composition, SymbolUi parentSymbolUi)
    {
        Items.Clear();

        foreach (var childInstance in composition.Children)
        {
            var childId = childInstance.SymbolChildId;
            var symbolChildUi = parentSymbolUi.ChildUis.SingleOrDefault(cc => cc.Id == childId);
            if (symbolChildUi == null)
                continue;

            Items.Add(childId, new()
                                   {
                                       Id = childId,
                                       Instance = childInstance,
                                       SymbolUi = SymbolUiRegistry.Entries[childInstance.Symbol.Id],
                                       SymbolChild = composition.Symbol.Children.SingleOrDefault(cc => cc.Id == childId),
                                       SymbolChildUi = symbolChildUi,
                                       PosOnCanvas = symbolChildUi.PosOnCanvas,
                                       Size = SnapGraphItem.GridSize,
                                   });
        }
    }

    private void CollectConnectionReferences(Instance composition)
    {
        SnapConnections.Clear();
        foreach (var c in composition.Symbol.Connections)
        {
            // Skip connection to symbol inputs and outputs for now
            if (c.IsConnectedToSymbolInput
                || c.IsConnectedToSymbolOutput
                || !Items.TryGetValue(c.SourceParentOrChildId, out var sourceItem)
                || !Items.TryGetValue(c.TargetParentOrChildId, out var targetItem)
               )
                continue;

            var output = targetItem.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId);
            var input = targetItem.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId);

            SnapConnections.Add(new SnapGraphConnection
                                    {
                                        Style = SnapGraphConnection.ConnectionStyles.Unknown,
                                        SourceItem = sourceItem,
                                        SourceOutput = output,
                                        TargetItem = targetItem,
                                        TargetInput = input,
                                    });
        }
    }

    private void UpdateVisibleItemLines()
    {
        // Todo: Implement connected multi-inputs
        foreach (var item in Items.Values)
        {
            item.InputLines.Clear();

            for (var inputIndex = 0; inputIndex < item.Instance.Inputs.Count; inputIndex++)
            {
                var input = item.Instance.Inputs[inputIndex];
                if (!item.SymbolUi.InputUis.TryGetValue(input.Id, out var inputUi)) //TODO: Log error?
                    continue;

                // Todo: Add temp expanded inputs (e.g. while dragging + hovered)

                if (!input.IsConnected
                    && inputUi.Relevancy is not (Relevancy.Relevant or Relevancy.Required)
                   )
                    continue;

                item.InputLines.Add(new SnapGraphItem.InputLine
                                        {
                                            Input = input,
                                            InputUi = inputUi,
                                            IsPrimary = inputIndex == 0,
                                        });
            }

            item.OutputLines.Clear();

            for (var outputIndex = 0; outputIndex < item.Instance.Outputs.Count; outputIndex++)
            {
                var output = item.Instance.Outputs[outputIndex];
                if (!item.SymbolUi.OutputUis.TryGetValue(output.Id, out var outputUi)) //TODO: Log error?
                    continue;

                item.OutputLines.Add(new SnapGraphItem.OutputLine
                                         {
                                             Output = output,
                                             OutputUi = outputUi,
                                             IsPrimary = outputIndex == 0,
                                         });
            }

            var count = Math.Max(1, item.InputLines.Count + item.OutputLines.Count);
            item.Size = new Vector2(SnapGraphItem.GridSize.X, SnapGraphItem.GridSize.Y * count);
        }
    }

    private void UpdateConnectionLayout()
    {
        foreach (var r in SnapConnections)
        {
            var targetItem = r.TargetItem;
            var sourceItem = r.SourceItem;

            // Find connected index
            var inputIndex = 0;
            foreach (var inputLine in targetItem.InputLines)
            {
                if (inputLine.Input == r.TargetInput)
                    break;

                inputIndex++;
            }

            var outputIndex = 0;
            foreach (var output in sourceItem.OutputLines)
            {
                if (output.Output == r.SourceOutput)
                    break;

                outputIndex++;
            }

            var sourceMin = sourceItem.PosOnCanvas;
            var sourceMax = sourceMin + sourceItem.Size;

            var targetMin = targetItem.PosOnCanvas;
            var targetMax = targetMin + targetItem.Size;

            // Snapped horizontally
            if (inputIndex == 0
                && outputIndex == 0
                && MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs(sourceMin.Y - targetMin.Y) < 1)
            {
                r.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal;
                var p = new Vector2(sourceMax.X, sourceMin.Y + 0.5f * SnapGraphItem.GridSize.Y);
                r.SourcePos = p;
                r.TargetPos = p;
                continue;
                // Log.Debug($"Snap horizontally {r.SourceItem} -> {r.TargetItem}");
            }

            if (inputIndex == 0
                && outputIndex == 0
                && MathF.Abs(sourceMin.X - targetMin.X) < 1
                && MathF.Abs(sourceMax.Y - targetMin.Y) < 1)
            {
                r.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical;
                var p = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X / 2, targetMin.Y);
                r.SourcePos = p;
                r.TargetPos = p;
                continue;
                // Log.Debug($"Snap vertically {r.SourceItem} -> {r.TargetItem}");
            }

            if (outputIndex == 0
                && inputIndex > 0
                && MathF.Abs(sourceMax.X - targetMin.Y) < 1
                && MathF.Abs(sourceMin.Y - targetMin.Y + (inputIndex + 1) * SnapGraphItem.GridSize.Y) < 1)
            {
                r.Style = SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal;
                var p = new Vector2(sourceMax.X, targetMin.Y + (1.5f + inputIndex) * SnapGraphItem.GridSize.Y);
                r.SourcePos = p;
                r.TargetPos = p;
                continue;
            }

            if (outputIndex > 0
                && inputIndex == 0
                && MathF.Abs(sourceMax.X - targetMin.Y) < 1
                && MathF.Abs(sourceMax.Y + (1 + sourceItem.OutputLines.Count + outputIndex) * SnapGraphItem.GridSize.Y) < 1)
            {
                r.Style = SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical;
                var p = new Vector2(sourceMax.X, targetMin.Y + 0.5f * SnapGraphItem.GridSize.Y);
                r.SourcePos = p;
                r.TargetPos = p;
                continue;
            }

            if (outputIndex == 0
                && inputIndex == 0
                && sourceMax.Y < targetMin.Y
                && MathF.Abs(sourceMin.X - targetMin.X) < SnapGraphItem.GridSize.X / 2)
            {
                r.SourcePos = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X / 2, sourceMax.Y);
                r.TargetPos = new Vector2(targetMin.X + SnapGraphItem.GridSize.X / 2, targetMin.Y);
                r.Style = SnapGraphConnection.ConnectionStyles.BottomToTop;
                continue;
            }

            var usedOutputUnit = outputIndex == 0 ? 0 : (1 + outputIndex + sourceItem.OutputLines.Count);

            r.SourcePos = new Vector2(sourceMax.X, sourceMin.Y + (usedOutputUnit + 0.5f) * SnapGraphItem.GridSize.Y);
            r.TargetPos = new Vector2(targetMin.X, targetMin.Y + (inputIndex + 0.5f) * SnapGraphItem.GridSize.Y);

            r.Style = SnapGraphConnection.ConnectionStyles.RightToLeft;

            //TODO: Snapped from output
            //TODO: Snapped to input
            //TODO: Snapped vertically
        }
    }



    private static bool HasCompositionDataChanged(Symbol composition, ref int hash)
    {
        var newHash = 0;
        foreach (var i in composition.Children)
        {
            newHash += i.GetHashCode();
        }

        if (newHash == hash)
            return false;

        hash = newHash;
        return true;
    }

    
    //public readonly List<SnapGroup> SnapGroups = new();
    public readonly Dictionary<Guid, SnapGraphItem> Items = new(127);
    public readonly List<SnapGraphConnection> SnapConnections = new(127);
    private int _compositionModelHash;
}