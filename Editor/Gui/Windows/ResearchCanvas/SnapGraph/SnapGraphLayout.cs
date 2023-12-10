using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
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
             UpdateConnectionSources(composition);
             UpdateVisibleItemLines(composition);
             CollectConnectionReferences(composition);
        }

        UpdateLayout();
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
                                       Category = SnapGraphItem.Categories.Operator,
                                       Id = childId,
                                       Instance = childInstance,
                                       Selectable = symbolChildUi,
                                       SymbolUi = SymbolUiRegistry.Entries[childInstance.Symbol.Id],
                                       SymbolChild = composition.Symbol.Children.SingleOrDefault(cc => cc.Id == childId),
                                       SymbolChildUi = symbolChildUi,
                                       PosOnCanvas = symbolChildUi.PosOnCanvas,
                                       Size = SnapGraphItem.GridSize,
                                   });
        }

        foreach (var input in composition.Inputs)
        {
            var inputUi = parentSymbolUi.InputUis[input.Id];
            Items.Add(input.Id, new()
                                    {
                                        Category = SnapGraphItem.Categories.Input,
                                        Id = input.Id,
                                        Instance = composition,
                                        Selectable = inputUi,
                                        SymbolUi = null,
                                        SymbolChild = null,
                                        SymbolChildUi = null,
                                        PosOnCanvas = inputUi.PosOnCanvas,
                                        Size = SnapGraphItem.GridSize,
                                    });
        }
        
        foreach (var output in composition.Outputs)
        {
            var outputUi = parentSymbolUi.OutputUis[output.Id];
            Items.Add(output.Id, new()
                                    {
                                        Category = SnapGraphItem.Categories.Output,
                                        Id = output.Id,
                                        Instance = composition,
                                        Selectable = outputUi,
                                        SymbolUi = null,
                                        SymbolChild = null,
                                        SymbolChildUi = null,
                                        PosOnCanvas = outputUi.PosOnCanvas,
                                        Size = SnapGraphItem.GridSize,
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
        var inputLines = new List<SnapGraphItem.InputLine>(8); 
        var outputLines = new List<SnapGraphItem.OutputLine>(4); 
        
        // Todo: Implement connected multi-inputs
        foreach (var item in Items.Values)
        {
            inputLines.Clear();
            outputLines.Clear();
            
            var visibleIndex = 0;
            
            // Collect inputs
            if (item.Category == SnapGraphItem.Categories.Operator)
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
                            inputLines.Add(new SnapGraphItem.InputLine
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
                        inputLines.Add(new SnapGraphItem.InputLine
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

                    //return  c.SourceParentOrChildId.GetHashCode() << 32 + c.SourceSlotId.GetHashCode();
                    long outputHash = item.Id.GetHashCode() << 32 + output.Id.GetHashCode();
                    var isConnected = ConnectedOuputs.Contains(outputHash);
                    //Log.Debug($"is connected  {isConnected}: {outputHash}");
                    if (outputIndex > 0 && !isConnected)
                        continue;

                    outputLines.Add(new SnapGraphItem.OutputLine
                                        {
                                            Output = output,
                                            OutputUi = outputUi,
                                            // IsPrimary = outputIndex == 0,
                                            OutputIndex = outputIndex,
                                            VisibleIndex = outputIndex == 0 ? 0 : visibleIndex,
                                            ConnectionsOut = new List<SnapGraphConnection>(),
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
            else if (item.Category == SnapGraphItem.Categories.Input)
            {
                if (item.Selectable is IInputUi inputUi)
                {
                    var input = item.Instance.Inputs.FirstOrDefault(i => i.Id == item.Id);
                    outputLines.Add(new SnapGraphItem.OutputLine
                                        {
                                            Output = input, // This looks confusing but is correct
                                            OutputUi = null,
                                            // IsPrimary =true,
                                            OutputIndex = 0,
                                            VisibleIndex = 0,
                                            ConnectionsOut = new List<SnapGraphConnection>(),
                                        });
                    
                }
            }
            else if (item.Category == SnapGraphItem.Categories.Output)
            {
                var output = item.Instance.Outputs.FirstOrDefault(o => o.Id == item.Id);
                if (item.Selectable is IOutputUi outputUi)
                {
                    inputLines.Add(new SnapGraphItem.InputLine()
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
            item.Size = new Vector2(SnapGraphItem.Width, SnapGraphItem.LineHeight * (Math.Max(1, visibleIndex)));
        }
    }
    
    private void CollectConnectionReferences(Instance composition)
    {
        SnapConnections.Clear();
        SnapConnections.Capacity = composition.Symbol.Connections.Count;
        
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
                
                var connectionFromSymbolInput = new SnapGraphConnection
                                              {
                                                  Style = SnapGraphConnection.ConnectionStyles.Unknown,
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
                SnapConnections.Add(connectionFromSymbolInput);
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
                
                var connectionFromSymbolInput = new SnapGraphConnection
                                                    {
                                                        Style = SnapGraphConnection.ConnectionStyles.Unknown,
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
                SnapConnections.Add(connectionFromSymbolInput);
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

            var snapGraphConnection = new SnapGraphConnection
                                          {
                                              Style = SnapGraphConnection.ConnectionStyles.Unknown,
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
            SnapConnections.Add(snapGraphConnection);
        }
    }

    private static void GetVisibleInputIndex(SnapGraphItem targetItem, IInputSlot input, out int inputIndex, out int multiInputIndex)
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

    private void UpdateLayout()
    {
        foreach (var sc in SnapConnections)
        {
            var sourceMin = sc.SourceItem.PosOnCanvas;
            var sourceMax = sourceMin + sc.SourceItem.Size;

            var targetMin = sc.TargetItem.PosOnCanvas;
            //var targetMax = targetMin + sc.TargetItem.Size;

            
            // Snapped horizontally
            if (
                //sc.InputLineIndex == 0
                //&& sc.OutputLineIndex == 0
                MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs((sourceMin.Y + sc.VisibleOutputIndex* SnapGraphItem.GridSize.Y) 
                             - (targetMin.Y + sc.InputLineIndex* SnapGraphItem.GridSize.Y)) < 1)
            {
                sc.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal;
                var p = new Vector2(sourceMax.X, sourceMin.Y + ( + sc.VisibleOutputIndex + 0.5f) * SnapGraphItem.GridSize.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
                // Log.Debug($"Snap horizontally {r.SourceItem} -> {r.TargetItem}");
            }

            if (sc.InputLineIndex == 0
                && sc.OutputLineIndex == 0
                && MathF.Abs(sourceMin.X - targetMin.X) < 1
                && MathF.Abs(sourceMax.Y - targetMin.Y) < 1)
            {
                sc.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical;
                var p = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X / 2, targetMin.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
                // Log.Debug($"Snap vertically {r.SourceItem} -> {r.TargetItem}");
            }

            if (sc.OutputLineIndex == 0
                && sc.InputLineIndex > 0
                && MathF.Abs(sourceMax.X - targetMin.X) < 1
                && MathF.Abs(sourceMin.Y - (targetMin.Y + sc.VisibleOutputIndex * SnapGraphItem.GridSize.Y)) < 1
                )
            {
                sc.Style = SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal;
                var p = new Vector2(sourceMax.X, targetMin.Y + (0.5f + sc.InputLineIndex) * SnapGraphItem.GridSize.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
            }

            if (sc.OutputLineIndex > 0
                && sc.InputLineIndex == 0
                && MathF.Abs(sourceMax.X - targetMin.Y) < 1
                && MathF.Abs(sourceMax.Y + (1 + sc.SourceItem.OutputLines.Length + sc.VisibleOutputIndex) * SnapGraphItem.GridSize.Y) < 1)
            {
                sc.Style = SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical;
                var p = new Vector2(sourceMax.X, targetMin.Y + 0.5f * SnapGraphItem.GridSize.Y);
                sc.SourcePos = p;
                sc.TargetPos = p;
                continue;
            }

            if (sc.OutputLineIndex == 0
                && sc.InputLineIndex == 0
                && sourceMax.Y < targetMin.Y
                && MathF.Abs(sourceMin.X - targetMin.X) < SnapGraphItem.GridSize.X / 2)
            {
                sc.SourcePos = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X / 2, sourceMax.Y);
                sc.TargetPos = new Vector2(targetMin.X + SnapGraphItem.GridSize.X / 2, targetMin.Y);
                sc.Style = SnapGraphConnection.ConnectionStyles.BottomToTop;
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


            sc.SourcePos = new Vector2(sourceMax.X, sourceMin.Y + (sc.VisibleOutputIndex + 0.5f) * SnapGraphItem.GridSize.Y);
            sc.TargetPos = new Vector2(targetMin.X, targetMin.Y + (sc.InputLineIndex + 0.5f) * SnapGraphItem.GridSize.Y);

            sc.Style = SnapGraphConnection.ConnectionStyles.RightToLeft;

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