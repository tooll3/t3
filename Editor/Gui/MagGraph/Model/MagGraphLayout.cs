#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.UiModel;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable SuggestBaseTypeForParameter

namespace T3.Editor.Gui.MagGraph.Model;

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
            return; // this would be a real issue - we should log its failure here
        
        if (forceUpdate || FrameStats.Last.UndoRedoTriggered || _structureFlaggedAsChanged || HasCompositionDataChanged(compositionOp.Symbol, ref _compositionModelHash))
            RefreshDataStructure(compositionOp, parentSymbolUi);

        UpdateConnectionLayout();
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

        var addedItemCount =0;
        var updatedItemCount = 0;
        
        foreach (var (childId, childInstance) in compositionOp.Children)
        {
            if (!compositionSymbolUi.ChildUis.TryGetValue(childId, out var childUi))
            {
                // this would also be a real issue - we should log its failure here
                // one simplification could be iterating over this collection instead,
                // and doing 'var child = childUi.SymbolChild'
                // could be potentially worth creating the paradigm of "the UI primarily works with the UI model", and only touches
                // the data model "e.g. symbol.Children" when necessary?
                continue;
            }
            
            if(!SymbolUiRegistry.TryGetSymbolUi(childInstance.Symbol.Id, out var symbolUi))
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
                                         Size = MagGraphItem.GridSize,
                                         LastUpdateCycle = _structureUpdateCycle,
                                     };
                addedItemCount++;
            }
        }

        foreach (var input in compositionOp.Inputs)
        {
            // input / output ids are abstract properties - release builds may optimize out any 
            // performance penalties of this (especially in the case of sealed implementation classes)
            // but storing the id locally and reusing that might be a micro-optimization that can be worth it
            // e.g. var id = input.Id
            if (Items.TryGetValue(input.Id, out var inputOp))
            {
                inputOp.ResetConnections(_structureUpdateCycle);
                updatedItemCount++;
            }
            else
            {
                var inputUi = compositionSymbolUi.InputUis[input.Id];
                Items[input.Id]= new MagGraphItem
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
        
        // iterating over IReadOnlyLists *might* be faster in a for loop than a foreach
        // i know that .NET 7 removed this advantage for List<T>, but idk if they did for IReadOnlyList<T> and similar interfaces
        // if readability is important to you and you want to (micro?) optimize, could be worth A-B testing in a release build
        foreach (var output in compositionOp.Outputs)
        {
                var outputUi = compositionSymbolUi.OutputUis[output.Id];
                if(Items.TryGetValue( output.Id, out var outputOp))
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
        
        if(Items.TryGetValue( PlaceholderCreation.PlaceHolderId, out var placeholderOp))
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
                Log.Debug("Remove obsolete item item " + item);
            }
        }
    }

    private readonly HashSet<long> _connectedOutputs = new(100);

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
        _connectedOutputs.Clear();

        foreach (var c in composition.Symbol.Connections)
        {
            if (c.IsConnectedToSymbolInput)
                continue;

            // i would make the GetConnectionSourceHash a local function - inlining is free, and it seems like this is the only place it is useful atm
            _connectedOutputs.Add(GetConnectionSourceHash(c));
            //_connectedOutputs.Add(new GuidPair(c.SourceParentOrChildId, c.SourceSlotId).Hash);
        }
    }

    // 99.9% unnecessary, but in the event that hashing functions are a concerning bottleneck, something unholy like below might perform better.
    // I understand hashing here is already thoroughly optimized by virtue of the use of longs as pre-computed hashes
    // however, afaik, hashsets/dictionaries use ints pretty exclusively (if im not mistaken), so it may be able to go further
    // i have not profiled it, just guessing
    // it also likely is not as thorough of a hash as one might need in more demanding cases,
    // but i'd give it a spin if hashing is any sort of a bottleneck here
    private readonly struct GuidPair
    {
        public readonly int Hash;
        public GuidPair(in Guid a, in Guid b)
        {
            var aInts = new GuidAsInts(a);
            var bInts = new GuidAsInts(b);
            
            var aIntHash = HashInts(aInts.a, bInts.a);
            var bIntHash = HashInts(aInts.b, bInts.b);
            var cIntHash = HashInts(aInts.c, bInts.c);
            var dIntHash = HashInts(aInts.d, bInts.d);
            
            Hash = HashInts(HashInts(aIntHash, bIntHash), HashInts(cIntHash, dIntHash));
            return;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // idk if this is technically necessary
            static int HashInts(int a, int b) => a ^ b; // you can replace this with something better lol
        }
        
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct GuidAsInts
        {
            [FieldOffset(0)]
            public readonly int a;
            [FieldOffset(4)]
            public readonly int b;
            [FieldOffset(8)]
            public readonly int c;
            [FieldOffset(12)]
            public readonly int d;
            
            
            [FieldOffset(0)]
            public readonly Guid guid;

            public GuidAsInts(in Guid g)
            {
                a = b = c = d = default;
                guid = g; // overwrites previous values due to the field layout
            }
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
                                            Id= parentInput!.Id,
                                            OutputUi = null,
                                            // IsPrimary =true,
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
    /// This is accessible because for some use-cases we need to compute the height of inserted items
    /// </summary>
    internal static int CollectVisibleLines(MagGraphItem item, List<MagGraphItem.InputLine> inputLines, List<MagGraphItem.OutputLine> outputLines,
                                           HashSet<long>? connectedOutputs =null)
    {
        Debug.Assert(item.Instance != null && item.SymbolUi != null);
        int visibleIndex = 0;
        
        for (var inputLineIndex = 0; inputLineIndex < item.Instance.Inputs.Count; inputLineIndex++)
        {
            var input = item.Instance.Inputs[inputLineIndex];
            if (!item.SymbolUi.InputUis.TryGetValue(input.Id, out var inputUi)) //TODO: Log error?
                continue;

            var isRelevant = inputUi.Relevancy is (Relevancy.Relevant or Relevancy.Required);
            var isMatchingType = false; //input.ValueType == typeof(float);//ConnectionTargetType;

            var shouldBeVisible = isRelevant || isMatchingType || inputLineIndex == 0 || input.HasInputConnections;
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
            var isConnected = connectedOutputs != null && connectedOutputs.Contains(outputHash);
            if (outputIndex > 0 && !isConnected)
                continue;

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

        Symbol.Connection c; // Avoid closure capture // this is still captured according to my rider extension
        for (var cIndex = 0; cIndex < composition.Symbol.Connections.Count; cIndex++)
        {
            c = composition.Symbol.Connections[cIndex];
            
            if (c.IsConnectedToSymbolInput)
            {
                if (!Items.TryGetValue(c.TargetParentOrChildId, out var targetItem2)
                    || !Items.TryGetValue(c.SourceSlotId, out var symbolInputItem)) // wouldnt this be an error we should know about?
                    continue;

                var symbolInput = composition.Inputs.FirstOrDefault(i => i.Id == c.SourceSlotId);
                
                // should Instance really be nullable?
                var targetInput = targetItem2.Instance.Inputs.FirstOrDefault(i => i.Input.InputDefinition.Id == c.TargetSlotId); 
                Debug.Assert(targetInput != null); // if this is in a release build, we will end up having a null reference exception anyway. should probably just use Inputs.First
                
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
                var sourceOutput = sourceItem2.Instance.Outputs.FirstOrDefault(o => o.Id == c.SourceSlotId); // same as above
                
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
    
    private static bool HasCompositionDataChanged(Symbol composition, ref int originalHash)
    {
        var newHash = 0; // this should be a long or be wrapped in an "unchecked" statement
        foreach (var i in composition.Children.Keys)
        {
            newHash += i.GetHashCode();
        }

        newHash += composition.Connections.Count.GetHashCode(); // a hashcode of an int is that int's value - you can skip this method call

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