using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="ConnectionLine"/>s. 
    /// It provides accessors for highlighting matching input slots and methods that need to be
    /// called when connections are completed or aborted.
    /// </summary>
    public static class ConnectionMaker
    {
        public static Symbol.Connection TempConnection = null;

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.TargetSlotId == NotConnectedId
                   //&& inputDef.DefaultValue.ValueType == _draftConnectionType;
                   && DraftConnectionType == valueType;
        }

        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnection != null
                   && TempConnection.SourceSlotId == NotConnectedId
                   && DraftConnectionType == valueType;
        }

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                   && TempConnection.SourceParentOrChildId == sourceUi.SymbolChild.Id
                   && TempConnection.SourceSlotId == outputDef.Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            // return ConnectionSnapEndHelper.IsNextBestTarget(targetUi, inputDef.Id, 0);
            return TempConnection != null
                   && TempConnection.TargetParentOrChildId == targetUi.SymbolChild.Id
                   && TempConnection.TargetSlotId == inputDef.Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnection != null
                   && TempConnection.SourceParentOrChildId == UseSymbolContainerId
                   && TempConnection.SourceSlotId == inputDef.Id;
        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnection != null
                   && TempConnection.TargetParentOrChildId == UseSymbolContainerId
                   && TempConnection.TargetSlotId == outputDef.Id;
        }

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            var existingConnections = FindConnectionsFromOutputSlot(parentSymbol, sourceUi, outputDef);
            TempConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                   sourceSlotId: outputDef.Id,
                                                   targetParentOrChildId: NotConnectedId,
                                                   targetSlotId: NotConnectedId);
            DraftConnectionType = outputDef.ValueType;
            _isDisconnectinFromInput = false;
        }

        private static bool _isDisconnectinFromInput;

        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputDef, multiInputIndex);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, multiInputIndex));

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnectedId,
                                                       targetSlotId: NotConnectedId);
                _isDisconnectinFromInput = true;
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnectedId,
                                                       sourceSlotId: NotConnectedId,
                                                       targetParentOrChildId: targetUi.SymbolChild.Id,
                                                       targetSlotId: inputDef.Id);
                _isDisconnectinFromInput = false;
            }

            DraftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            TempConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                   sourceSlotId: inputDef.Id,
                                                   targetParentOrChildId: NotConnectedId,
                                                   targetSlotId: NotConnectedId);
            DraftConnectionType = inputDef.DefaultValue.ValueType;
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainerId
                                                                        && c.TargetSlotId == outputDef.Id);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, 0));

                TempConnection = new Symbol.Connection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                       sourceSlotId: existingConnection.SourceSlotId,
                                                       targetParentOrChildId: NotConnectedId,
                                                       targetSlotId: NotConnectedId);
            }
            else
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: NotConnectedId,
                                                       sourceSlotId: NotConnectedId,
                                                       targetParentOrChildId: UseSymbolContainerId,
                                                       targetSlotId: outputDef.Id);
            }

            DraftConnectionType = outputDef.ValueType;
        }

        public static void Update()
        {
            ConnectionSnapEndHelper.PrepareNewFrame();
        }

        public static void Cancel()
        {
            TempConnection = null;
            DraftConnectionType = null;
        }

        public static void CompleteAtInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input, int multiInputIndex = 0,
                                               bool insertMultiInput = false)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: targetUi.SymbolChild.Id,
                                                      targetSlotId: input.Id);

            bool replaceConnection = multiInputIndex % 2 != 0;
            multiInputIndex /= 2; // divide by 2 to get correct insertion index in existing connections
            var addCommand = new AddConnectionCommand(parentSymbol, newConnection, multiInputIndex);

            if (replaceConnection)
            {
                // get the previous connection
                var allConnectionsToSlot = parentSymbol.Connections.FindAll(c => c.TargetParentOrChildId == targetUi.SymbolChild.Id &&
                                                                                 c.TargetSlotId == input.Id);
                var connectionToRemove = allConnectionsToSlot[multiInputIndex];
                var deleteCommand = new DeleteConnectionCommand(parentSymbol, connectionToRemove, multiInputIndex);
                var replaceCommand = new MacroCommand("Replace Connection", new ICommand[] { deleteCommand, addCommand });
                UndoRedoStack.AddAndExecute(replaceCommand);
            }
            else
            {
                UndoRedoStack.AddAndExecute(addCommand);
            }

            TempConnection = null;
        }

        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                      sourceSlotId: output.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void InitSymbolBrowserAtPosition(SymbolBrowser symbolBrowser, Vector2 canvasPosition)
        {
            if (TempConnection == null)
                return;

            if (_isDisconnectinFromInput)
            {
                TempConnection = null;
                return;
            }

            if (TempConnection.TargetParentOrChildId == NotConnectedId)
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                       sourceSlotId: TempConnection.SourceSlotId,
                                                       targetParentOrChildId: UseDraftChildId,
                                                       targetSlotId: Guid.Empty);
                symbolBrowser.OpenAt(canvasPosition, DraftConnectionType, null);
            }
            else if (TempConnection.SourceParentOrChildId == NotConnectedId)
            {
                TempConnection = new Symbol.Connection(sourceParentOrChildId: UseDraftChildId,
                                                       sourceSlotId: Guid.Empty,
                                                       targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                       targetSlotId: TempConnection.TargetSlotId);
                symbolBrowser.OpenAt(canvasPosition, null, DraftConnectionType);
            }
        }

        public static void CompleteConnectionIntoBuiltNode(Symbol parentSymbol, SymbolChild newOp, Symbol.InputDefinition inputDef)
        {
            if (inputDef == null)
                return;

            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: newOp.Id,
                                                      targetSlotId: inputDef.Id);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteConnectionFromBuiltNode(Symbol parentSymbol, SymbolChild newOp, Symbol.OutputDefinition outputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: newOp.Id,
                                                      sourceSlotId: outputDef.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                      sourceSlotId: inputDef.Id,
                                                      targetParentOrChildId: TempConnection.TargetParentOrChildId,
                                                      targetSlotId: TempConnection.TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnection = null;
        }

        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnection.SourceParentOrChildId,
                                                      sourceSlotId: TempConnection.SourceSlotId,
                                                      targetParentOrChildId: UseSymbolContainerId,
                                                      targetSlotId: outputDef.Id);
            parentSymbol.AddConnection(newConnection);
            TempConnection = null;
        }

        private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        {
            var outputId = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
            return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
                                                         && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        }

        private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            var outputId = output.Id;
            return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
                                                         && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        }

        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        {
            var inputId = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
            return parentSymbol.Connections.Find(c => c.TargetSlotId == inputId
                                                      && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
        }

        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input,
                                                                   int multiInputIndex = 0)
        {
            var inputId = input.Id;
            var connections = parentSymbol.Connections.FindAll(c => c.TargetSlotId == inputId
                                                                    && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
            return (connections.Count > 0) ? connections[multiInputIndex] : null;
        }

        /// <summary>
        /// This is a cached value to highlight matching inputs or outputs
        /// </summary>
        public static Type DraftConnectionType = null;

        /// <summary>
        /// A spectial Id the flags a connection as incomplete because either the source or the target is not yet connected.
        /// </summary>
        public static Guid NotConnectedId = Guid.NewGuid();

        /// <summary>
        /// A special Id that indicates that the source of target of a connection is not a child but an input or output node
        /// </summary>
        public static Guid UseSymbolContainerId = Guid.Empty;

        /// <summary>
        /// A special id indicating that the connection is ending in the <see cref="SymbolBrowser"/>
        /// </summary>
        public static Guid UseDraftChildId = Guid.NewGuid();

        /// <summary>
        /// A helper that collects potential collection targets during connection drag operations.
        /// </summary>
        public static class ConnectionSnapEndHelper
        {
            public static void PrepareNewFrame()
            {
                var drawList = ImGui.GetWindowDrawList();
                _mousePosition = ImGui.GetMousePos();
                _bestMatchLastFrame = _bestMatchYetForCurrentFrame;
                if (_bestMatchLastFrame != null)
                {
                    drawList.AddRect(_bestMatchLastFrame.Area.Min, _bestMatchLastFrame.Area.Max, Color.Orange);
                    var textSize = ImGui.CalcTextSize(_bestMatchLastFrame.Name);
                    ImGui.SetNextWindowPos(_mousePosition - new Vector2(textSize.X + 10, textSize.Y / 2));
                    ImGui.BeginTooltip();
                    ImGui.Text(_bestMatchLastFrame.Name);
                    ImGui.EndTooltip();
                }
                
                _bestMatchYetForCurrentFrame = null;
                _bestMatchDistance = float.PositiveInfinity;
            }

            public static void RegisterAsConnectionTarget(SymbolChildUi childUi, IInputUi inputUi, int slotIndex, ImRect areaOnScreen)
            {
                if (ConnectionMaker.TempConnection == null)
                    return;

                if (inputUi.Type != ConnectionMaker.DraftConnectionType)
                    return;

                var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
                if (distance > 100 || distance > _bestMatchDistance)
                {
                    return;
                }

                _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                 {
                                     TargetParentOrChildId = childUi.SymbolChild.Id,
                                     TargetInputId = inputUi.InputDefinition.Id,
                                     Area = areaOnScreen,
                                     Name = inputUi.InputDefinition.Name,
                                     SlotIndex = slotIndex
                                 };
                _bestMatchDistance = distance;
            }

            private static PotentialConnectionTarget _bestMatchLastFrame;
            private static PotentialConnectionTarget _bestMatchYetForCurrentFrame;
            private static float _bestMatchDistance = float.PositiveInfinity;

            private static Vector2 _mousePosition;

            private class PotentialConnectionTarget
            {
                public Guid TargetParentOrChildId;
                public Guid TargetInputId;
                public ImRect Area;
                public string Name;
                public int SlotIndex;
            }

            public static bool IsNextBestTarget(SymbolChildUi childUi, Guid inputDefinitionId, int socketIndex)
            {
                return _bestMatchLastFrame != null && _bestMatchLastFrame.TargetParentOrChildId == childUi.SymbolChild.Id
                                          && _bestMatchLastFrame.TargetInputId == inputDefinitionId
                                          && _bestMatchLastFrame.SlotIndex == socketIndex;
            }
        }
    }
}