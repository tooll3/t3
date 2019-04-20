using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    /// <summary>
    /// This handler provides the snapping functionality for OperatorWidgets. The heart of this
    /// feature is a recursive method AddSnappedNeighboursToPool that starts from an OperatorWidgets 
    /// and and finds other OperatorWidgets snapped to a a block.
    /// 
    /// </summary>
    public class OperatorSnappingHelper
    {
        public IStackable MovingOperator { get; private set; }
        public bool SnappedGroupIsMoving { get; private set; }
        public List<IStackable> DragGroup => _dragGroup;

        public OperatorSnappingHelper(IStackable movedOperator)
        {
            MovingOperator = movedOperator;
        }

        public void Start(List<IStackable> selectedElements)
        {
            SnappedGroupIsMoving = false;
            _dragGroup.Clear();

            // Move either block or all selected operators
            if (!MovingOperator.IsSelected)
            {
                AddSnappedNeighboursToPool(_dragGroup, MovingOperator);
            }
            else
            {
                _dragGroup.AddRange(selectedElements);
            }

            // Keep Original Positions
            _positionBeforeDrag.Clear();
            foreach (var ow in _dragGroup)
            {
                _positionBeforeDrag[ow] = ow.Position;
            }

            // Build COMMAND
            //var dragOps = (from opWidget in _dragGroup select opWidget.Operator).ToArray();
            //var properties = (from op in dragOps
            //                  let propertyEntry = new UpdateOperatorPropertiesCommand.Entry(op)
            //                  select propertyEntry).ToArray();
            //_updateOperatorPropertiesCommand = new UpdateOperatorPropertiesCommand(dragOps, properties);
        }


        public void UpdateBeforeMoving(Vector2 offset)
        {
            SnappedGroupIsMoving = TryToMoveSnappedGroupWhenNotSelected(offset);
        }


        public void Stop(Vector2 offset)
        {
            SnappedGroupIsMoving = false;
            _dragGroup.Clear();
            var wasDragged = offset.Length() > 0f;
            if (wasDragged)
            {
                // Apply COMMAND
                // App.Current.UndoRedoStack.AddAndExecute(_updateOperatorPropertiesCommand);
            }
        }

        #region implementation
        private static void AddSnappedNeighboursToPool(List<IStackable> pool, IStackable el)
        {
            pool.Add(el);
            var parentsAndChildren = new List<IStackable>();
            parentsAndChildren.AddRange(GetOpsConnectedAbove(el, true));
            parentsAndChildren.AddRange(GetOpsConnectedAbove(el, true));

            foreach (var opWi in parentsAndChildren)
            {
                if (!pool.Contains(opWi))
                    AddSnappedNeighboursToPool(pool, opWi);
            }
        }

        private bool TryToMoveSnappedGroupWhenNotSelected(Vector2 offset)
        {
            if (_dragGroup.Count() < 1) // include self
                return false;

            var somethingSnapped = false;

            // First move all to new position
            foreach (var opWidget in _dragGroup)
            {
                opWidget.Position = _positionBeforeDrag[opWidget] - offset;
            }

            // Then check if one would snap
            foreach (var opWidgetForSnapping in _dragGroup)
            {
                var positionBeforeSnapping = opWidgetForSnapping.Position;

                if (TrySnappingToOperatorsConnectedAbove(opWidgetForSnapping, _dragGroup) || TrySnappingToOperatorsConnectedBelow(opWidgetForSnapping, _dragGroup))
                {
                    var offsetBySnapping = opWidgetForSnapping.Position - positionBeforeSnapping;

                    // Adjust all other positions to snapped one
                    foreach (var opWidget in _dragGroup)
                    {
                        if (opWidget != opWidgetForSnapping)
                        {
                            opWidget.Position += offsetBySnapping;
                        }
                    }
                    somethingSnapped = true;
                    break;
                }
            }

            for (int idx = 0; idx < _dragGroup.Count; ++idx)
            {
                var opWidget = _dragGroup[idx];
                //_updateOperatorPropertiesCommand.ChangeEntries[idx].Position = opWidget.Position;
            }

            //_updateOperatorPropertiesCommand.Do();

            return somethingSnapped;
        }

        private static bool TrySnappingToOperatorsConnectedAbove(IStackable src, List<IStackable> dragGroup)
        {
            var op = (from opAbove in GetOpsConnectedAbove(src)
                      where !dragGroup.Contains(opAbove) &&
                            !IsElementDragged(opAbove) &&
                            (Math.Abs(opAbove.Position.Y + GraphCanvas.GridSize - src.Position.Y) < VERTICAL_DISTANCE_SNAP_THRESHOLD) &&
                            (src.GetHorizontalOverlapWith(opAbove) > HORIZONTAL_OVERLAP_SNAP_THRESHOD)
                      select opAbove).FirstOrDefault();

            if (op == null)
                return false;

            var dx = src.Position.X - op.Position.X;
            src.Position = new Vector2(
                src.Position.X - ((dx + 0.5f * GraphCanvas.GridSize) % GraphCanvas.GridSize) + 0.5f * GraphCanvas.GridSize,
                op.Position.Y + GraphCanvas.GridSize);
            return true;
        }

        private static bool TrySnappingToOperatorsConnectedBelow(IStackable target, List<IStackable> dragGroup)
        {
            var op = (from opBelow in GetOpsConnectedBelow(target)
                      where !dragGroup.Contains(opBelow)
                        && !IsElementDragged(opBelow)
                        && Math.Abs(target.Position.Y + GraphCanvas.GridSize - opBelow.Position.Y) < VERTICAL_DISTANCE_SNAP_THRESHOLD
                        && target.GetHorizontalOverlapWith(opBelow) > HORIZONTAL_OVERLAP_SNAP_THRESHOD
                      select opBelow).FirstOrDefault();

            if (op == null)
                return false;

            var dx = target.Position.X - op.Position.X;
            var gridAlignedX = target.Position.X
                            - ((dx + 0.5f * GraphCanvas.GridSize) % GraphCanvas.GridSize)
                            + 0.5f * GraphCanvas.GridSize;
            target.Position = new Vector2(
                gridAlignedX,
                op.Position.Y - GraphCanvas.GridSize);
            return true;
        }

        private static bool IsElementDragged(IStackable el)
        {
            // FIXME: Needs implementation
            return true;
        }

        private static List<IStackable> GetOpsConnectedBelow(IStackable stackable, bool onlySnapped = false)
        {
            var stackableOpsBelow = new List<IStackable>();
            foreach (var connectionIn in stackable.GetInputConnections())
            {
                if (onlySnapped && connectionIn.IsSnapped())
                    continue;

                var t = connectionIn.SourceItem as IStackable;
                if (t != null)
                    stackableOpsBelow.Add(t);
            }
            return stackableOpsBelow;
        }

        private static List<IStackable> GetOpsConnectedAbove(IStackable stackable, bool onlySnapped = false)
        {
            var stackableOpsAbove = new List<IStackable>();
            foreach (var connectionOut in stackable.GetOutputConnections())
            {
                if (onlySnapped && connectionOut.IsSnapped())
                    continue;

                var t = connectionOut.TargetItem as IStackable;
                if (t != null)
                    stackableOpsAbove.Add(t);
            }
            return stackableOpsAbove;
        }



        private static List<IStackable> GetOperatorsSnappedAbove(IStackable s)
        {
            //foreach (var opAbove in s.GetOpsConnectedToOutputs())
            //{

            //}
            //return from opAbove in s.GetOpsConnectedToOutputs()
            //return (from cl in  ConnectionsOut
            //        where cl.IsSnapped && cl.Target is OperatorWidget
            //        select cl.Target as IStackable).ToList();
            return new List<IStackable>();
        }

        public List<IStackable> GetOperatorsSnappedBelow()
        {
            //return (from cl in ConnectionsIn
            //        where cl.IsSnapped && cl.Source is OperatorWidget
            //        select cl.Source as IStackable).ToList();
            return new List<IStackable>();
        }



        #endregion

        #region constants
        private const double SNAP_RANGE = 15;
        private const double VERTICAL_OVERLAP_SNAP_THRESHOLD = 0.4f * GraphCanvas.GridSize;
        private const double HORIZONTAL_OVERLAP_SNAP_THRESHOLD = 0.4f * GraphCanvas.GridSize;
        private const double HORIZONTAL_OVERLAP_SNAP_THRESHOD = 1f * GraphCanvas.GridSize;
        private const double VERTICAL_DISTANCE_SNAP_THRESHOLD = 0.4f * GraphCanvas.GridSize;
        #endregion

        //private UpdateOperatorPropertiesCommand _updateOperatorPropertiesCommand;
        private List<IStackable> _dragGroup = new List<IStackable>();
        private Dictionary<IStackable, Vector2> _positionBeforeDrag = new Dictionary<IStackable, Vector2>();
    }
}

