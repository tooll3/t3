using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Windows.ResearchCanvas.Model;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

/// <summary>
/// Generate considerations:
/// - The layout model is a temporary data that is completely be generated from the t3ui data.
/// - Its computation is expensive and should only be done if required.
/// - This might be especially complicated if the state cannot be stored in the layout model, because it
///   relies on a temp. property like expanding a parameter type group.
/// </summary>
public class SnapGraphLayout
{
    private Instance _lastInstance;
    private int _hash;
    
    public void CollectSnappingGroupsFromSymbolUi(Instance instance)
    {
        if (!SymbolUiRegistry.Entries.TryGetValue(instance.Symbol.Id, out var parentSymbolUi))
            return;

        var parentSymbol = instance.Symbol;
        
        var checkSum = 0;
        foreach (var i in instance.Symbol.Children)
        {
            checkSum += i.GetHashCode();
        }

        if (true || checkSum != _hash)
        {
            _hash = checkSum;

            Items = new Dictionary<Guid, SnapGraphItem>(parentSymbolUi.ChildUis.Count);
            foreach (var childInstance in instance.Children)
            {
                var childId = childInstance.SymbolChildId;
                var symbolChildUi = parentSymbolUi.ChildUis.SingleOrDefault(cc => cc.Id == childId);
                Items[childId] = new()
                                     {
                                         Id = childId,
                                         Instance = childInstance,
                                         SymbolUi = SymbolUiRegistry.Entries[childInstance.Symbol.Id],
                                         SymbolChild = parentSymbol.Children.SingleOrDefault(cc => cc.Id == childId),
                                         SymbolChildUi = symbolChildUi,
                                         PosOnCanvas = symbolChildUi.PosOnCanvas,
                                         Size = SnapGraphItem.GridSize,
                                     };
            }
        }
        
        SnapGroups.Clear();
        
        // Collect visible inputs to compute height for operator sockets
        // Todo: Implement connected multi-inputs
        foreach (var (childId, item) in Items)
        {
            item.VisibleInputSockets.Clear();

            for (var inputIndex = 0; inputIndex < item.Instance.Inputs.Count; inputIndex++)
            {
                var input = item.Instance.Inputs[inputIndex];
                if (!item.SymbolUi.InputUis.TryGetValue(input.Id, out var inputUi)) //TODO: Log error?
                    continue;

                // Todo: Add temp expanded inputs (e.g. while dragging + hovered)
                
                if ( inputIndex > 0 &&
                           !input.IsConnected
                           && inputUi.Relevancy is not (Relevancy.Relevant or Relevancy.Required)
                           )
                    continue;

                item.VisibleInputSockets.Add(new SnapGraphItem.InSocket
                                                 {
                                                     Input = input,
                                                     InputUi = inputUi,
                                                 });
            }

            item.VisibleOutputSockets.Clear();

            for (var outputIndex = 0; outputIndex < item.Instance.Outputs.Count; outputIndex++)
            {
                var output = item.Instance.Outputs[outputIndex];
                if (!item.SymbolUi.OutputUis.TryGetValue(output.Id, out var outputUi)) //TODO: Log error?
                    continue;

                if (outputIndex > 0)
                {
                    item.VisibleOutputSockets.Add(new SnapGraphItem.OutSocket
                                                      {
                                                          Output = output,
                                                          OutputUi = outputUi,
                                                      });
                }
            }

            var count = Math.Max(1, item.VisibleInputSockets.Count + item.VisibleOutputSockets.Count);
            item.Size = new Vector2(SnapGraphItem.GridSize.X, SnapGraphItem.GridSize.Y * count);
        }
        
        
        
        // Resolve connection styles and snap groups
        SnapConnections = new SnapGraphConnection[parentSymbol.Connections.Count];
        
        for (var index = 0; index < parentSymbol.Connections.Count; index++)
        {
            var connection = parentSymbol.Connections[index];
            var snapConnection = ComputeConnectionProperties(connection);
            SnapConnections[index] = snapConnection;

            if (snapConnection.Style == SnapGraphConnection.ConnectionStyles.Unknown)
                continue;
            
            var sourceGroup = snapConnection.SourceItem.SnapGroup;
            var targetGroup = snapConnection.TargetItem.SnapGroup;
            if (!snapConnection.IsSnapped)
                continue;
            
            if (sourceGroup == null && targetGroup == null )
            {
                var newGroup = new SnapGroup();
                snapConnection.TargetItem.SnapGroup = newGroup;
                snapConnection.SourceItem.SnapGroup = newGroup;
                newGroup.Items.Add(snapConnection.TargetItem);
                newGroup.Items.Add(snapConnection.SourceItem);
                newGroup.ConnectionUiIndices.Add(index);
                SnapGroups.Add(newGroup);
            }
            else if(sourceGroup == targetGroup)
            {
                // already merged
            }
            else if (sourceGroup != null && targetGroup != null)
            {
                // merge sourceGroup -> targetGroup
                foreach (var i in sourceGroup.Items)
                {
                    i.SnapGroup = targetGroup;
                    targetGroup.Items.Add(i);
                }

                SnapGroups.Remove(sourceGroup);
            }
            else if (sourceGroup != null)
            {
                snapConnection.TargetItem.SnapGroup = sourceGroup;
            }
            else
            {
                snapConnection.SourceItem.SnapGroup = targetGroup;
            }
        }
    }

    private SnapGraphConnection ComputeConnectionProperties(Symbol.Connection c)
    {
        var r = new SnapGraphConnection
                    {
                        Style = SnapGraphConnection.ConnectionStyles.Unknown,
                    };

        // Skip connection to symbol inputs and outputs for now
        if (c.IsConnectedToSymbolInput || c.IsConnectedToSymbolOutput)
            return r;

        if (!Items.TryGetValue(c.SourceParentOrChildId, out var sourceItem))
            return r;

        if (!Items.TryGetValue(c.TargetParentOrChildId, out var targetItem))
            return r;

        r.SourceItem = sourceItem;
        r.TargetItem = targetItem;
        

        // Find connected index
        var inputIndex = 0;
        foreach (var input in targetItem.VisibleInputSockets)
        {
            if (input.Input.Id == c.TargetSlotId)
                break;

            inputIndex++;
        }

        var outputIndex = 0;
        foreach (var output in sourceItem.VisibleOutputSockets)
        {
            if (output.Output.Id == c.SourceSlotId)
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
            Log.Debug($"Snap horizontally {r.SourceItem} -> {r.TargetItem}");
            return r;
        }

        // Snapped vertically
        if (inputIndex == 0
            && outputIndex == 0
            && MathF.Abs(sourceMin.X - targetMin.X) < 1
            && MathF.Abs(sourceMax.Y - targetMin.Y) < 1)
        {
            r.Style = SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical;
            var p = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X /2, targetMin.Y);
            r.SourcePos = p;
            r.TargetPos = p;
            Log.Debug($"Snap vertically {r.SourceItem} -> {r.TargetItem}");
            return r;
        }

        // Snapped to input
        if (outputIndex == 0
            && inputIndex > 0
            && MathF.Abs(sourceMax.X - targetMin.Y) < 1
            && MathF.Abs(sourceMin.Y - targetMin.Y + (inputIndex + 1) * SnapGraphItem.GridSize.Y) < 1)
        {
            r.Style = SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal;
            var p = new Vector2(sourceMax.X, targetMin.Y + (1.5f + inputIndex) * SnapGraphItem.GridSize.Y);
            r.SourcePos = p;
            r.TargetPos = p;
            return r;
        }

        // Snapped from output
        if (outputIndex > 0
            && inputIndex == 0
            && MathF.Abs(sourceMax.X - targetMin.Y) < 1
            && MathF.Abs(sourceMax.Y + (1 + sourceItem.VisibleOutputSockets.Count + outputIndex) * SnapGraphItem.GridSize.Y) < 1)
        {
            r.Style = SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical;
            var p = new Vector2(sourceMax.X, targetMin.Y + 0.5f * SnapGraphItem.GridSize.Y);
            r.SourcePos = p;
            r.TargetPos = p;
            return r;
        }

        if (outputIndex == 0
            && inputIndex == 0
            && sourceMax.Y < targetMin.Y
            && MathF.Abs(sourceMin.X - targetMin.X) < SnapGraphItem.GridSize.X / 2)
        {
            r.SourcePos = new Vector2(sourceMin.X + SnapGraphItem.GridSize.X / 2, sourceMax.Y );
            r.TargetPos = new Vector2(targetMin.X + SnapGraphItem.GridSize.X / 2, targetMin.Y );
            r.Style = SnapGraphConnection.ConnectionStyles.BottomToTop;
        }
        else 
        {
            var usedOutputUnit = outputIndex == 0 ? 0 : (1 + outputIndex+ sourceItem.VisibleOutputSockets.Count);
            
            r.SourcePos = new Vector2(sourceMax.X, sourceMin.Y + (usedOutputUnit + 0.5f) * SnapGraphItem.GridSize.Y );
            r.TargetPos = new Vector2(targetMin.X, targetMin.Y + (inputIndex + 0.5f) * SnapGraphItem.GridSize.Y );

            r.Style = SnapGraphConnection.ConnectionStyles.RightToLeft;
        }

        return r;
    }
    
    public readonly List<SnapGroup> SnapGroups = new();
    public Dictionary<Guid, SnapGraphItem> Items = new();
    public SnapGraphConnection[] SnapConnections = {};
}