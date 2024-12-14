#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable SuggestBaseTypeForParameter

namespace T3.Editor.Gui.MagGraph.Model;

/// <summary>
/// Holds an intermediate view model that is updated if required. This view model
/// builds referenceable items, view elements, and connections. This makes it much easier to traverse the
/// graph without dictionary lookups. The layout also precomputes the visibility of input links, which simplifies
/// the layout and rendering of connection lines (one of the most complicated parts of the legacy layout).
/// Generate considerations:
/// - The layout model is a temporary data that is completely generated from the t3ui data.
/// - Its computation is expensive and should only be done if required.
/// - This might be especially complicated if the state cannot be stored in the layout model, because it
///   relies on a temp. property like expanding a parameter type group.
///
/// It basically separates the following steps:
/// - generating the ui-model data (esp. elements, their inputs and connection slot indices)
/// - Updating the layouts
/// 
/// </summary>
internal sealed class MagGraphLayout
{
    public void ComputeLayout(Instance compositionOp, bool forceUpdate = false)
    {
        if (!SymbolUiRegistry.TryGetSymbolUi(compositionOp.Symbol.Id, out var parentSymbolUi))
            return;

        if (forceUpdate || FrameStats.Last.UndoRedoTriggered || _structureFlaggedAsChanged ||
            HasCompositionDataChanged(compositionOp.Symbol, ref _compositionModelHash))
            RefreshDataStructure(compositionOp, parentSymbolUi);

        UpdateConnectionLayout();
        ComputeVerticalStackBoundaries();
    }

    public void FlagAsChanged()
    {
        _structureFlaggedAsChanged = true;
    }

    private int _structureUpdateCycle;

    private void RefreshDataStructure(Instance composition, SymbolUi parentSymbolUi)
    {
        _structureUpdateCycle++;
        CollectItemReferences(composition, parentSymbolUi);
        UpdateConnectionSources(composition);
        UpdateVisibleItemLines();
        CollectConnectionReferences(composition);
        _structureFlaggedAsChanged = false;
    }

    /// <remarks>
    /// This method is extremely slow for large compositions...
    /// </remarks>
    private void CollectItemReferences(Instance compositionOp, SymbolUi compositionSymbolUi)
    {
        //Items.Clear();

        var addedItemCount = 0;
        var updatedItemCount = 0;

        foreach (var (childId, childInstance) in compositionOp.Children)
        {
            if (!compositionSymbolUi.ChildUis.TryGetValue(childId, out var childUi))
                continue;

            if (!SymbolUiRegistry.TryGetSymbolUi(childInstance.Symbol.Id, out var symbolUi))
                continue;

            if (Items.TryGetValue(childId, out var opItem))
            {
                opItem.ResetConnections(_structureUpdateCycle);
                updatedItemCount++;
            }
            else
            {
                Items[childId] = new MagGraphItem
                                     {
                                         Variant = MagGraphItem.Variants.Operator,
                                         Id = childId,
                                         Instance = childInstance,
                                         Selectable = childUi,
                                         SymbolUi = symbolUi,
                                         SymbolChild = childUi.SymbolChild,
                                         ChildUi = childUi,
                                         Size = MagGraphItem.GridSize,
                                         LastUpdateCycle = _structureUpdateCycle,
                                         DampedPosOnCanvas = childUi.PosOnCanvas,
                                     };
                addedItemCount++;
            }
        }

        foreach (var input in compositionOp.Inputs)
        {
            if (Items.TryGetValue(input.Id, out var inputOp))
            {
                inputOp.ResetConnections(_structureUpdateCycle);
                updatedItemCount++;
            }
            else
            {
                var inputUi = compositionSymbolUi.InputUis[input.Id];
                Items[input.Id] = new MagGraphItem
                                      {
                                          Variant = MagGraphItem.Variants.Input,
                                          Id = input.Id,
                                          Instance = compositionOp,
                                          Selectable = inputUi,
                                          Size = MagGraphItem.GridSize,
                                      };
                addedItemCount++;
            }
        }

        foreach (var output in compositionOp.Outputs)
        {
            var outputUi = compositionSymbolUi.OutputUis[output.Id];
            if (Items.TryGetValue(output.Id, out var outputOp))
            {
                outputOp.ResetConnections(_structureUpdateCycle);
                updatedItemCount++;
            }
            else
            {
                Items[output.Id] = new MagGraphItem
                                       {
                                           Variant = MagGraphItem.Variants.Output,
                                           Id = output.Id,
                                           Instance = compositionOp,
                                           Selectable = outputUi,
                                           Size = MagGraphItem.GridSize,
                                       };
                addedItemCount++;
            }
        }

        if (Items.TryGetValue(PlaceholderCreation.PlaceHolderId, out var placeholderOp))
        {
            placeholderOp.ResetConnections(_structureUpdateCycle);
            updatedItemCount++;
        }

        var hasObsoleteItems = Items.Count > updatedItemCount + addedItemCount;
        if (hasObsoleteItems)
        {
            foreach (var item in Items.Values)
            {
                if (item.LastUpdateCycle >= _structureUpdateCycle)
                    continue;

                Items.Remove(item.Id);
                item.Variant = MagGraphItem.Variants.Obsolete;
            }
        }
    }

    private readonly HashSet<int> _connectedOutputs = new(100);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetConnectionSourceHash(Symbol.Connection c)
    {
        var hash = c.SourceSlotId.GetHashCode();
        hash = hash * 31 + c.SourceParentOrChildId.GetHashCode();
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCodeForSlot(ISlot output)
    {
        var hash = output.Id.GetHashCode();
        hash = hash * 31 + output.Parent.SymbolChildId.GetHashCode();
        return hash;
    }

    /// <summary>
    /// Sadly there is no easy method to store if an output has a connection 
    /// </summary>
    private void UpdateConnectionSources(Instance composition)
    {
        _connectedOutputs.Clear();

        foreach (var c in composition.Symbol.Connections)
        {
            if (c.IsConnectedToSymbolInput)
                continue;

            _connectedOutputs.Add(GetConnectionSourceHash(c));
        }
    }

    private void UpdateVisibleItemLines()
    {
        var inputLines = new List<MagGraphItem.InputLine>(8);
        var outputLines = new List<MagGraphItem.OutputLine>(4);

        // Todo: Implement connected multi-inputs
        foreach (var item in Items.Values)
        {
            inputLines.Clear();
            outputLines.Clear();

            var visibleIndex = 0;

            
            switch (item.Variant)
            {
                // Collect inputs
                case MagGraphItem.Variants.Operator:
                {
                    visibleIndex = CollectVisibleLines(item, inputLines, outputLines, _connectedOutputs);
                    break;
                }
                case MagGraphItem.Variants.Input:
                {
                    Debug.Assert(item.Selectable is IInputUi);
                    var parentInstance = item.Instance;

                    Debug.Assert(parentInstance != null);
                    var parentInput = parentInstance.Inputs.FirstOrDefault(i => i.Id == item.Id);

                    outputLines.Add(new MagGraphItem.OutputLine
                                        {
                                            Output = parentInput!, // This looks confusing but is correct
                                            Id = parentInput!.Id,
                                            OutputUi = null,
                                            OutputIndex = 0,
                                            VisibleIndex = 0,
                                            ConnectionsOut = [],
                                        });

                    break;
                }
                case MagGraphItem.Variants.Output:
                {
                    Debug.Assert(item.Selectable is IOutputUi);

                    var parentInstance = item.Instance;
                    Debug.Assert(parentInstance != null);

                    var parentOutput = parentInstance.Outputs.FirstOrDefault(o => o.Id == item.Id);
                    Debug.Assert(parentOutput != null);

                    inputLines.Add(new MagGraphItem.InputLine
                                       {
                                           Type = parentOutput.ValueType,
                                           Id = parentOutput.Id,
                                           Input = parentOutput,
                                           MultiInputIndex = 0,
                                           VisibleIndex = 0,
                                       });
                    break;
                }
                case MagGraphItem.Variants.Placeholder:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            


            item.InputLines = inputLines.ToArray();
            item.OutputLines = outputLines.ToArray();

            //var count = Math.Max(1, item.InputLines.Count + item.OutputLines.Count -2);
            item.Size = new Vector2(MagGraphItem.Width, MagGraphItem.LineHeight * (Math.Max(1, visibleIndex)));
        }
    }

    /// <summary>
    /// This is accessible because for some use-cases we need to compute the height of inserted items.
    /// </summary>
    internal static int CollectVisibleLines(MagGraphItem item, List<MagGraphItem.InputLine> inputLines, List<MagGraphItem.OutputLine> outputLines,
                                            HashSet<int>? connectedOutputs = null)
    {
        Debug.Assert(item.Instance != null && item.SymbolUi != null);
        int visibleIndex = 0;

        for (var inputLineIndex = 0; inputLineIndex < item.Instance.Inputs.Count; inputLineIndex++)
        {
            var input = item.Instance.Inputs[inputLineIndex];
            if (!item.SymbolUi.InputUis.TryGetValue(input.Id, out var inputUi))
            {
                Log.Warning("Can't find input? " + input.Id);
                continue;
            }

            var isRelevant = inputUi.Relevancy is Relevancy.Relevant or Relevancy.Required;
            //var isMatchingType = false; //input.ValueType == typeof(float);//ConnectionTargetType;

            var isPrimaryInput = inputLineIndex == 0;
            var shouldBeVisible = isRelevant || isPrimaryInput || input.HasInputConnections;
            if (!shouldBeVisible)
                continue;

            if (input.IsMultiInput && input is IMultiInputSlot multiInputSlot)
            {
                var multiInputIndex = 0;
                foreach (var _ in multiInputSlot.GetCollectedInputs())
                {
                    inputLines.Add(new MagGraphItem.InputLine
                                       {
                                           Id = input.Id,
                                           Type = input.ValueType,
                                           Input = input,
                                           InputUi = inputUi,
                                           VisibleIndex = visibleIndex,
                                           MultiInputIndex = multiInputIndex++,
                                       });
                    visibleIndex++;
                }

                if (isRelevant && multiInputIndex == 0)
                {
                    inputLines.Add(new MagGraphItem.InputLine
                                       {
                                           Id = input.Id,
                                           Type = input.ValueType,
                                           Input = input,
                                           InputUi = inputUi,
                                           VisibleIndex = visibleIndex,
                                           MultiInputIndex = multiInputIndex,
                                       });
                    visibleIndex++;
                }
            }
            else
            {
                inputLines.Add(new MagGraphItem.InputLine
                                   {
                                       Id = input.Id,
                                       Type = input.ValueType,
                                       Input = input,
                                       InputUi = inputUi,
                                       VisibleIndex = visibleIndex,
                                   });
                visibleIndex++;
            }
        }

        // Collect outputs
        for (var outputIndex = 0; outputIndex < item.Instance.Outputs.Count; outputIndex++)
        {
            var output = item.Instance.Outputs[outputIndex];
            if (!item.SymbolUi.OutputUis.TryGetValue(output.Id, out var outputUi))
            {
                Log.Warning("Can't find outputUi:" + output.Id);
                continue;
            }

            // Should non connected secondary outputs be visible?
            // int outputHash2 = GetHashCodeForSlot(output);
            // var isConnected = connectedOutputs != null && connectedOutputs.Contains(outputHash2);
            // if (outputIndex > 0 && !isConnected)
            //     continue;

            outputLines.Add(new MagGraphItem.OutputLine
                                {
                                    Id = outputUi.Id,
                                    Output = output,
                                    OutputUi = outputUi,
                                    OutputIndex = outputIndex,
                                    VisibleIndex = outputIndex == 0 ? 0 : visibleIndex,
                                    ConnectionsOut = [],
                                });
            if (outputIndex == 0)
            {
                item.PrimaryType = output.ValueType;
            }
            else
            {
                visibleIndex++;
            }
        }

        return visibleIndex;
    }

    private void CollectConnectionReferences(Instance composition)
    {
        MagConnections.Clear();
        MagConnections.Capacity = composition.Symbol.Connections.Count;

        Symbol.Connection c; // Avoid closure capture
        for (var cIndex = 0; cIndex < composition.Symbol.Connections.Count; cIndex++)
        {
            c = composition.Symbol.Connections[cIndex];

            if (c.IsConnectedToSymbolInput)
            {
                if (!Items.TryGetValue(c.TargetParentOrChildId, out var targetItem2)
                    || !Items.TryGetValue(c.SourceSlotId, out var symbolInputItem))
                    continue;

                var symbolInput = composition.Inputs.FirstOrDefault(i => i.Id == c.SourceSlotId);
                var targetInput = targetItem2.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId);
                Debug.Assert(targetInput != null);

                GetVisibleInputIndex(targetItem2, targetInput, out var targetInputIndex, out var targetMultiInputIndex);

                var connectionFromSymbolInput = new MagGraphConnection
                                                    {
                                                        Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                        SourceItem = symbolInputItem,
                                                        SourceOutput = symbolInput,
                                                        TargetItem = targetItem2,
                                                        //TargetInput = targetInput,
                                                        InputLineIndex = targetInputIndex,
                                                        OutputLineIndex = 0,
                                                        ConnectionHash = c.GetHashCode(),
                                                        MultiInputIndex = targetMultiInputIndex,
                                                        VisibleOutputIndex = 0,
                                                    };

                symbolInputItem.OutputLines[0].ConnectionsOut.Add(connectionFromSymbolInput);
                targetItem2.InputLines[targetInputIndex].ConnectionIn = connectionFromSymbolInput;
                MagConnections.Add(connectionFromSymbolInput);
                continue;
            }

            if (c.IsConnectedToSymbolOutput)
            {
                if (!Items.TryGetValue(c.SourceParentOrChildId, out var sourceItem2)
                    || !Items.TryGetValue(c.TargetSlotId, out var symbolOutputItem))
                    continue;

                var symbolOutput = composition.Outputs.FirstOrDefault((o => o.Id == c.TargetSlotId));
                var sourceOutput = sourceItem2.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId);

                Debug.Assert(sourceOutput != null);

                var outputIndex2 = 0;
                foreach (var outLine in sourceItem2.OutputLines)
                {
                    if (outLine.Output != sourceOutput)
                        continue;

                    outputIndex2 = outLine.OutputIndex;
                    break;
                }

                if (outputIndex2 >= sourceItem2.OutputLines.Length)
                {
                    //Log.Warning($"OutputIndex {outputIndex} exceeds number of output lines {sourceItem.OutputLines.Length} in {sourceItem}");
                    outputIndex2 = sourceItem2.OutputLines.Length - 1;
                }

                var connectionFromSymbolInput = new MagGraphConnection
                                                    {
                                                        Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                        SourceItem = sourceItem2,
                                                        SourceOutput = symbolOutput,
                                                        TargetItem = symbolOutputItem,
                                                        //TargetInput = targetInput,
                                                        InputLineIndex = 0,
                                                        OutputLineIndex = 0,
                                                        ConnectionHash = c.GetHashCode(),
                                                        MultiInputIndex = 0,
                                                        VisibleOutputIndex = 0,
                                                    };

                sourceItem2.OutputLines[outputIndex2].ConnectionsOut.Add(connectionFromSymbolInput);
                symbolOutputItem.InputLines[0].ConnectionIn = connectionFromSymbolInput;
                MagConnections.Add(connectionFromSymbolInput);
                continue;
            }

            // Skip connection to symbol inputs and outputs for now
            if (c.IsConnectedToSymbolOutput
                || !Items.TryGetValue(c.SourceParentOrChildId, out var sourceItem)
                || !Items.TryGetValue(c.TargetParentOrChildId, out var targetItem)
               )
            {
                Log.Warning("Can't find items for connection line");
                continue;
            }

            // Connections between nodes
            var output = sourceItem.Variant == MagGraphItem.Variants.Input
                             ? sourceItem.Instance.Inputs.FirstOrDefault(o => o.Id == c.SourceSlotId)
                             : sourceItem.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId);

            var input = targetItem.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId);

            if (output == null || input == null)
            {
                Log.Warning("Unable to find connected items?");
                continue;
            }

            GetVisibleInputIndex(targetItem, input, out var inputIndex, out var multiInputIndex2);

            var outputIndex = 0;
            foreach (var outLine in sourceItem.OutputLines)
            {
                if (outLine.Output != output)
                    continue;

                outputIndex = outLine.OutputIndex;
                break;
            }

            if (outputIndex >= sourceItem.OutputLines.Length)
            {
                Log.Warning($"OutputIndex {outputIndex} exceeds number of output lines {sourceItem.OutputLines.Length} in {sourceItem}");
                outputIndex = sourceItem.OutputLines.Length - 1;
            }

            var snapGraphConnection = new MagGraphConnection
                                          {
                                              Style = MagGraphConnection.ConnectionStyles.Unknown,
                                              SourceItem = sourceItem,
                                              SourceOutput = output,
                                              TargetItem = targetItem,
                                              InputLineIndex = inputIndex,
                                              OutputLineIndex = outputIndex,
                                              ConnectionHash = c.GetHashCode(),
                                              MultiInputIndex = multiInputIndex2,
                                              VisibleOutputIndex = sourceItem.OutputLines[outputIndex].VisibleIndex,
                                          };

            targetItem.InputLines[inputIndex].ConnectionIn = snapGraphConnection;
            sourceItem.OutputLines[outputIndex].ConnectionsOut.Add(snapGraphConnection);
            MagConnections.Add(snapGraphConnection);
        }
    }

    private static void GetVisibleInputIndex(MagGraphItem targetItem, IInputSlot input, out int inputIndex, out int multiInputIndex)
    {
        // Find connected index
        inputIndex = 0;
        multiInputIndex = 0;
        for (var index = 0; index < targetItem.InputLines.Length; index++)
        {
            if (targetItem.InputLines[index].Id == input.Id)
            {
                while (targetItem.InputLines[index].ConnectionIn != null && index < targetItem.InputLines.Length)
                {
                    index++;
                    inputIndex++;
                    multiInputIndex++;
                }

                break;
            }

            inputIndex++;
        }
    }

    /// <summary>
    /// This improves the layout of arc connections inputs into multiple stacked ops so they
    /// avoid overlap.
    /// </summary>
    private void ComputeVerticalStackBoundaries()
    {
        // Reuse list to avoid allocations
        var listStackedItems = new List<MagGraphItem>();
        MagGraphItem? previousItem = null;

        listStackedItems.Clear();
        foreach (var item in Items.Values.OrderBy(i => MathF.Round(i.PosOnCanvas.X * 1f)).ThenBy(i => i.PosOnCanvas.Y))
        {
            item.VerticalStackArea = item.Area;

            if (previousItem == null)
            {
                listStackedItems.Clear();
                listStackedItems.Add(item);
                previousItem = item;
                continue;
            }

            // is stacked?
            if (Math.Abs(item.PosOnCanvas.X - previousItem.PosOnCanvas.X) < 20f
                && Math.Abs(item.PosOnCanvas.Y - previousItem.Area.Max.Y) < 20f)
            {
                listStackedItems.Add(item);
                previousItem = item;
            }
            else
            {
                if (listStackedItems.Count > 1)
                {
                    var stackArea = new ImRect(listStackedItems[0].PosOnCanvas,
                                               listStackedItems[^1].Area.Max);
                    foreach (var x in listStackedItems)
                    {
                        x.VerticalStackArea = stackArea;
                    }
                }

                listStackedItems.Clear();
                listStackedItems.Add(item);
                previousItem = item;
            }
        }
    }

    /// <summary>
    /// Update the layout of the connections (e.g. if they are snapped, of flowing vertically or horizontally)
    /// </summary>
    private void UpdateConnectionLayout()
    {
        foreach (var sc in MagConnections)
        {
            var sourceMin = sc.SourceItem.PosOnCanvas;
            var sourceMax = sourceMin + sc.SourceItem.Size;

            var targetMin = sc.TargetItem.PosOnCanvas;

            // Snapped horizontally
            if (
                MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs((sourceMin.Y + sc.VisibleOutputIndex * MagGraphItem.GridSize.Y)
                             - (targetMin.Y + sc.InputLineIndex * MagGraphItem.GridSize.Y)) < 1)
            {
                sc.Style = MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal;

                var p = new Vector2(sourceMax.X, sourceMin.Y + (+sc.VisibleOutputIndex + 0.5f) * MagGraphItem.GridSize.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
            }

            if (sc.InputLineIndex == 0
                && sc.OutputLineIndex == 0
                && MathF.Abs(sourceMin.X - targetMin.X) < 1
                && MathF.Abs(sourceMax.Y - targetMin.Y) < 1)
            {
                sc.Style = MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical;
                var p = new Vector2(sourceMin.X + MagGraphItem.GridSize.X / 2, targetMin.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
            }

            if (sc.OutputLineIndex == 0
                && sc.InputLineIndex > 0
                && MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs(sourceMin.Y - (targetMin.Y + sc.VisibleOutputIndex * MagGraphItem.GridSize.Y)) < 1
               )
            {
                //sc.Style = MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal;
                sc.Style = MagGraphConnection.ConnectionStyles.BottomToLeft;
                var p = new Vector2(sourceMax.X, targetMin.Y + (0.5f + sc.InputLineIndex) * MagGraphItem.GridSize.Y);
                sc.SourcePos = new Vector2((sourceMin.X + sourceMax.X) / 2, sourceMax.Y);
                sc.TargetPos = p;
                continue;
            }

            if (sc.OutputLineIndex > 0
                && sc.InputLineIndex == 0
                && MathF.Abs(sourceMax.X - targetMin.Y) < 1
                && MathF.Abs(sourceMax.Y + (1 + sc.SourceItem.OutputLines.Length + sc.VisibleOutputIndex) * MagGraphItem.GridSize.Y) < 1)
            {
                sc.Style = MagGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical;
                var p = new Vector2(sourceMax.X, targetMin.Y + 0.5f * MagGraphItem.GridSize.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
            }

            if (sc.OutputLineIndex == 0
                && sc.InputLineIndex == 0
                && sourceMax.Y < targetMin.Y
                //&& MathF.Abs(sourceMin.X - targetMin.X) < MagGraphItem.GridSize.X / 2
                && sourceMin.X > targetMin.X - MagGraphItem.GridSize.X / 2
               )
            {
                sc.SourcePos = new Vector2(sourceMin.X + MagGraphItem.GridSize.X / 2, sourceMax.Y);
                sc.TargetPos = new Vector2(targetMin.X + MagGraphItem.GridSize.X / 2, targetMin.Y);
                sc.Style = MagGraphConnection.ConnectionStyles.BottomToTop;
                continue;
            }

            sc.SourcePos = new Vector2(sourceMax.X, sourceMin.Y + (sc.VisibleOutputIndex + 0.5f) * MagGraphItem.GridSize.Y);
            sc.TargetPos = new Vector2(targetMin.X, targetMin.Y + (sc.InputLineIndex + 0.5f) * MagGraphItem.GridSize.Y);

            sc.Style = MagGraphConnection.ConnectionStyles.RightToLeft;

            //TODO: Snapped from output
            //TODO: Snapped to input
            //TODO: Snapped vertically
        }
    }

    /// <summary>
    /// We rely on manually flagging structure changes, because 
    /// computing a hash of a composition is not easy because the item order of children can change...
    /// </summary>
    private static bool HasCompositionDataChanged(Symbol composition, ref int originalHash)
    {
        var newHash = composition.Id.GetHashCode();

        if (newHash == originalHash)
            return false;

        originalHash = newHash;
        return true;
    }

    public readonly Dictionary<Guid, MagGraphItem> Items = new(127);
    public readonly List<MagGraphConnection> MagConnections = new(127);
    private int _compositionModelHash;
    private bool _structureFlaggedAsChanged;
}