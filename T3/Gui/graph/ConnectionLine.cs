using imHelpers;
using System;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Converts the elements of an Symbol into Connections, 
    /// </summary>
    public static class ConnectionNetwork
    {

    }

    /// <summary>
    /// Graphical representation between  between <see cref="IStackable"/> like <see cref="SymbolChildUi"/>.
    /// </summary>
    public class ConnectionLine
    {
        public IConnectionTarget TargetItem;
        public IConnectable SourceItem;
        public Symbol.Connection Connection;

        public bool IsSnapped()
        {
            //var children = SymbolChildUiRegistry.Entries[symbol.Id];

            if (TargetItem == null || SourceItem == null)
                return false;

            float targetY = TargetItem.Position.Y + GraphCanvas.GridSize + 1.0f;
            float sourceY = SourceItem.Position.Y + 1.0f;

            float targetX = TargetItem.Position.X;
            float sourceX = SourceItem.Position.X;

            int index = GetMultiInputIndex();
            //var range = Target.GetRangeForInputConnectionLine(Input, index, false);
            var inputSlot = FindConnectionInputSlot();
            float targetXmin = targetX + inputSlot.XInItem;
            float targetXmax = targetX + inputSlot.XInItem + inputSlot.Width;
            float targetXcenter = targetXmin + 0.5f * (targetXmax - targetXmin);

            int outputCount = 1;    // Todo: needs implementation
            int outputIndex = 0;    // Todo: needs implementation
            float outputWidth = SourceItem.Size.X / outputCount;
            float sourceXmin = sourceX + outputIndex * outputWidth;
            float sourceXmax = sourceXmin + outputWidth;
            float sourceXcenter = sourceXmin + 0.5f * (sourceXmax - sourceXmin);

            // Calculate straight factor from overlap
            const float BLEND_RANGE = 20;
            const float BLEND_BORDER = 10;
            float overlapp = Math.Min(sourceXmax, targetXmax) - Math.Max(sourceXmin, targetXmin);
            float straightFactor = Math.Min(BLEND_RANGE, Math.Max(0, overlapp - BLEND_BORDER)) / BLEND_RANGE;

            // Limit straight connection to a certain y range...
            const float STRAIGHT_MIN_DISTANCE = 80;
            const float STRAIGHT_DISTANCE_BLEND = 50;
            float dy = sourceY - targetY;
            if (dy < -2)
            {
                straightFactor = 0.0f;
            }
            else
            {
                float f = 1 - (Im.Clamp(dy, STRAIGHT_MIN_DISTANCE, STRAIGHT_MIN_DISTANCE + STRAIGHT_DISTANCE_BLEND) - STRAIGHT_MIN_DISTANCE) / STRAIGHT_DISTANCE_BLEND;
                straightFactor *= f;
            }
            return Math.Abs(dy) < 0.001 && straightFactor.CompareTo(1.0) == 0;
        }

        private VisibleInputSlot FindConnectionInputSlot()
        {
            foreach (var area in TargetItem.GetVisibileInputSlots())
            {
                if (area.InputDefinition.Id == Connection.InputDefinitionId)
                    return area;
            }
            return null;

        }

        private int GetMultiInputIndex()
        {
            return 0;
        }
    }
}
