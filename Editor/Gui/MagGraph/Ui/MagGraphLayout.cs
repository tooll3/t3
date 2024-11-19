using System.Runtime.CompilerServices;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.UiModel;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable SuggestBaseTypeForParameter

namespace T3.Editor.Gui.MagGraph.Ui;

/// <summary>
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

        if (forceUpdate || _structureFlaggedAsChanged || HasCompositionDataChanged(compositionOp.Symbol, ref _compositionModelHash))
            RefreshDataStructure(compositionOp, parentSymbolUi);

        UpdateLayout();
    }
    
    public void FlagAsChanged()
    {
        _structureFlaggedAsChanged = true;
    }
    
    private void RefreshDataStructure(Instance composition, SymbolUi parentSymbolUi)
    {
        CollectItemReferences(composition, parentSymbolUi);
        UpdateConnectionSources(composition);
        UpdateVisibleItemLines(composition);
        CollectConnectionReferences(composition);
        _structureFlaggedAsChanged = false;
    }

    /// <remarks>
    /// This method is extremely slow for large compositions...
    /// </remarks>
    private void CollectItemReferences(Instance compositionOp, SymbolUi compositionSymbolUi)
    {
        Items.Clear();

        foreach (var (childId, childInstance) in compositionOp.Children)
        {
            if(!compositionSymbolUi.ChildUis.TryGetValue(childId, out var childUi))
                continue;
            
            if(!SymbolUiRegistry.TryGetSymbolUi(childInstance.Symbol.Id, out var symbolUi))
                continue;
            
            Items.Add(childId, new MagGraphItem
                                   {
                                       Variant = MagGraphItem.Variants.Operator,
                                       Id = childId,
                                       Instance = childInstance,
                                       Selectable = childUi,
                                       SymbolUi = symbolUi,
                                       SymbolChild =  childUi.SymbolChild,
                                       PosOnCanvas = childUi.PosOnCanvas,
                                       Size = MagGraphItem.GridSize,
                                   });
        }

        foreach (var input in compositionOp.Inputs)
        {
            var inputUi = compositionSymbolUi.InputUis[input.Id];
            Items.Add(input.Id, new MagGraphItem
                                    {
                                        Variant = MagGraphItem.Variants.Input,
                                        Id = input.Id,
                                        Instance = compositionOp,
                                        Selectable = inputUi,
                                        SymbolUi = null,
                                        SymbolChild = null,
                                        PosOnCanvas = inputUi.PosOnCanvas,
                                        Size = MagGraphItem.GridSize,
                                    });
        }
        
        foreach (var output in compositionOp.Outputs)
        {
            var outputUi = compositionSymbolUi.OutputUis[output.Id];
            Items.Add(output.Id, new MagGraphItem
                                     {
                                        Variant = MagGraphItem.Variants.Output,
                                        Id = output.Id,
                                        Instance = compositionOp,
                                        Selectable = outputUi,
                                        SymbolUi = null,
                                        SymbolChild = null,
                                        PosOnCanvas = outputUi.PosOnCanvas,
                                        Size = MagGraphItem.GridSize,
                                    });
        }
    }

    private HashSet<long> ConnectedOuputs = new(100);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetConnectionSourceHash(Symbol.Connection c)
    {
        return  c.SourceParentOrChildId.GetHashCode() << 32 + c.SourceSlotId.GetHashCode();
    }

    /// <summary>
    /// Sadly there is no easy method to store if an output has a connection 
    /// </summary>
    private void UpdateConnectionSources(Instance composition)
    {
        ConnectedOuputs.Clear();

        foreach (var c in composition.Symbol.Connections)
        {
            if (c.IsConnectedToSymbolInput)
                continue;

            ConnectedOuputs.Add(GetConnectionSourceHash(c));
        }
    }
    
    private void UpdateVisibleItemLines(Instance composition)
    {
        var inputLines = new List<MagGraphItem.InputLine>(8); 
        var outputLines = new List<MagGraphItem.OutputLine>(4); 
        
        // Todo: Implement connected multi-inputs
        foreach (var item in Items.Values)
        {
            inputLines.Clear();
            outputLines.Clear();
            
            var visibleIndex = 0;
            
            // Collect inputs
            if (item.Variant == MagGraphItem.Variants.Operator)
            {
                for (var inputLineIndex = 0; inputLineIndex < item.Instance.Inputs.Count; inputLineIndex++)
                {
                    var input = item.Instance.Inputs[inputLineIndex];
                    if (!item.SymbolUi.InputUis.TryGetValue(input.Id, out var inputUi)) //TODO: Log error?
                        continue;

                    if (inputLineIndex > 0
                        && (!input.HasInputConnections
                            && inputUi.Relevancy is not (Relevancy.Relevant or Relevancy.Required))
                       )
                        continue;

                    if (input.IsMultiInput && input is IMultiInputSlot multiInputSlot)
                    {
                        var multiInputIndex = 0;
                        foreach (var i in multiInputSlot.GetCollectedInputs())
                        {
                            inputLines.Add(new MagGraphItem.InputLine
                                               {
                                                   Id = input.Id,
                                                   Type = input.ValueType,
                                                   Input = input,
                                                   InputUi = inputUi,
                                                   // IsPrimary = inputLineIndex == 0,
                                                   VisibleIndex = visibleIndex,
                                                   MultiInputIndex = multiInputIndex++,
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
                                               // IsPrimary = inputLineIndex == 0,
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

                    long outputHash = item.Id.GetHashCode() << 32 + output.Id.GetHashCode();
                    var isConnected = ConnectedOuputs.Contains(outputHash);
                    if (outputIndex > 0 && !isConnected)
                        continue;

                    outputLines.Add(new MagGraphItem.OutputLine
                                        {
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
            }
            else if (item.Variant == MagGraphItem.Variants.Input)
            {
                if (item.Selectable is IInputUi inputUi)
                {
                    var input = item.Instance.Inputs.FirstOrDefault(i => i.Id == item.Id);
                    outputLines.Add(new MagGraphItem.OutputLine
                                        {
                                            Output = input, // This looks confusing but is correct
                                            OutputUi = null,
                                            // IsPrimary =true,
                                            OutputIndex = 0,
                                            VisibleIndex = 0,
                                            ConnectionsOut = new List<MagGraphConnection>(),
                                        });
                    
                }
            }
            else if (item.Variant == MagGraphItem.Variants.Output)
            {
                var output = item.Instance.Outputs.FirstOrDefault(o => o.Id == item.Id);
                if (item.Selectable is IOutputUi outputUi)
                {
                    inputLines.Add(new MagGraphItem.InputLine()
                                        {
                                            Type = output.ValueType,
                                            Id = output.Id,
                                            Input = output,
                                            // InputUi = null,
                                            // IsPrimary =true,
                                            MultiInputIndex = 0,
                                            VisibleIndex = 0,
                                        });
                }
            }

            item.InputLines = inputLines.ToArray();
            item.OutputLines = outputLines.ToArray();

            //var count = Math.Max(1, item.InputLines.Count + item.OutputLines.Count -2);
            item.Size = new Vector2(MagGraphItem.Width, MagGraphItem.LineHeight * (Math.Max(1, visibleIndex)));
        }
    }
    
    private void CollectConnectionReferences(Instance composition)
    {
        MagConnections.Clear();
        MagConnections.Capacity = composition.Symbol.Connections.Count;
        
        foreach (var c in composition.Symbol.Connections)
        {
            if (c.IsConnectedToSymbolInput)
            {
                if (!Items.TryGetValue(c.TargetParentOrChildId, out var targetItem2)
                    ||!Items.TryGetValue(c.SourceSlotId, out var symbolInputItem))
                    continue;
                
                var symbolInput = composition.Inputs.FirstOrDefault((i => i.Id == c.SourceSlotId));
                var targetInput = targetItem2.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId);
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
                    ||!Items.TryGetValue(c.TargetSlotId, out var symbolOutputItem))
                    continue;
                
                var symbolOutput = composition.Outputs.FirstOrDefault((o => o.Id == c.TargetSlotId));
                var sourceOutput = sourceItem2.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId);
                //GetVisibleInputIndex(targetItem2, targetInput, out var targetInputIndex, out var targetMultiInputIndex);
                
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
                    //Log.Warning($"OutputIndex {outputIndex} exceeds number of outputlines {sourceItem.OutputLines.Length} in {sourceItem}");
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
            var output = sourceItem.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId);
            var input = targetItem.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId);

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
                Log.Warning($"OutputIndex {outputIndex} exceeds number of outputlines {sourceItem.OutputLines.Length} in {sourceItem}");
                outputIndex = sourceItem.OutputLines.Length - 1;
            }

            var snapGraphConnection = new MagGraphConnection
                                          {
                                              Style = MagGraphConnection.ConnectionStyles.Unknown,
                                              SourceItem = sourceItem,
                                              SourceOutput = output,
                                              TargetItem = targetItem,
                                              //TargetInput = input,
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
    /// Update the layout of the connections (e.g. if they are snapped, of flowing vertically or horizontally)
    /// </summary>
    private void UpdateLayout()
    {
        foreach (var sc in MagConnections)
        {
            var sourceMin = sc.SourceItem.PosOnCanvas;
            var sourceMax = sourceMin + sc.SourceItem.Size;

            var targetMin = sc.TargetItem.PosOnCanvas;
            
            // Snapped horizontally
            if (
                MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs((sourceMin.Y + sc.VisibleOutputIndex* MagGraphItem.GridSize.Y) 
                             - (targetMin.Y + sc.InputLineIndex* MagGraphItem.GridSize.Y)) < 1)
            {
                sc.Style = MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal;
                var p = new Vector2(sourceMax.X, sourceMin.Y + ( + sc.VisibleOutputIndex + 0.5f) * MagGraphItem.GridSize.Y);
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
                sc.Style = MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal;
                var p = new Vector2(sourceMax.X, targetMin.Y + (0.5f + sc.InputLineIndex) * MagGraphItem.GridSize.Y);
                sc.SourcePos = p;
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
                && MathF.Abs(sourceMin.X - targetMin.X) < MagGraphItem.GridSize.X / 2)
            {
                sc.SourcePos = new Vector2(sourceMin.X + MagGraphItem.GridSize.X / 2, sourceMax.Y);
                sc.TargetPos = new Vector2(targetMin.X + MagGraphItem.GridSize.X / 2, targetMin.Y);
                sc.Style = MagGraphConnection.ConnectionStyles.BottomToTop;
                continue;
            }
            
            // Snapped horizontally 2
            // if (sc.OutputLineIndex > 0
            //     && sc.InputLineIndex > 0
            //     && MathF.Abs(sourceMax.X - targetMin.X) < 1
            //     // && MathF.Abs((sourceMin.Y + sc.VisibleOutputIndex * SnapGraphItem.GridSize.Y) 
            //     //              - (targetMin.Y + sc.InputLineIndex * SnapGraphItem.GridSize.Y)) < 1
            //     )
            // {
            //     sc.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal;
            //     var p = new Vector2(sourceMax.X, sourceMin.Y + 0.5f * SnapGraphItem.GridSize.Y);
            //     sc.SourcePos = p;
            //     sc.TargetPos = p;
            //     continue;
            // }


            sc.SourcePos = new Vector2(sourceMax.X, sourceMin.Y + (sc.VisibleOutputIndex + 0.5f) * MagGraphItem.GridSize.Y);
            sc.TargetPos = new Vector2(targetMin.X, targetMin.Y + (sc.InputLineIndex + 0.5f) * MagGraphItem.GridSize.Y);

            sc.Style = MagGraphConnection.ConnectionStyles.RightToLeft;

            //TODO: Snapped from output
            //TODO: Snapped to input
            //TODO: Snapped vertically
        }
    }
    
    private static bool HasCompositionDataChanged(Symbol composition, ref int originalHash)
    {
        var newHash = 0;
        foreach (var i in composition.Children.Keys)
        {
            newHash += i.GetHashCode();
        }

        if (newHash == originalHash)
            return false;

        originalHash = newHash;
        return true;
    }

    
    //public readonly List<SnapGroup> SnapGroups = new();
    public readonly Dictionary<Guid, MagGraphItem> Items = new(127);
    public readonly List<MagGraphConnection> MagConnections = new(127);
    private int _compositionModelHash;
    private bool _structureFlaggedAsChanged;

}