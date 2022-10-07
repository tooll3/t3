using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Represents the placeholder for a new <see cref="GraphNode"/> on the <see cref="GraphCanvas"/>. 
    /// It can be connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class SymbolBrowser
    {
        #region public API ------------------------------------------------------------------------
        public void OpenAt(Vector2 positionOnCanvas, Type filterInputType, Type filterOutputType, bool onlyMultiInputs, MacroCommand prepareCommand)
        {
            _prepareCommand = prepareCommand;
            IsOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filter.FilterInputType = filterInputType;
            _filter.FilterOutputType = filterOutputType;
            _filter.SearchString = "";
            _selectedSymbolUi = null;
            _filter.OnlyMultiInputs = onlyMultiInputs;
            _filter.UpdateIfNecessary();
            THelpers.DisableImGuiKeyboardNavigation();

            if (_selectedSymbolUi == null && _filter.MatchingSymbolUis.Count > 0)
            {
                _selectedSymbolUi = _filter.MatchingSymbolUis[0];
            }
        }

        public void Draw()
        {
            if (!IsOpen)
            {
                if (!ImGui.IsWindowFocused() || !ImGui.IsKeyReleased((ImGuiKey)Key.Tab))
                    return;

                if (NodeSelection.GetSelectedChildUis().Count() != 1)
                {
                    OpenAt(GraphCanvas.Current.InverseTransformPositionFloat(ImGui.GetIO().MousePos + new Vector2(-4, -20)), null, null, false, null);
                    return;
                }
                
                var childUi = NodeSelection.GetSelectedChildUis().ToList()[0];
                {
                    var instance = NodeSelection.GetInstanceForSymbolChildUi(childUi);
                    ConnectionMaker.OpenBrowserWithSingleSelection(this, childUi, instance);
                }

                return;
            }

            T3Ui.OpenedPopUpName = "SymbolBrowser";

            _filter.UpdateIfNecessary();

            ImGui.PushID(UiId);
            {
                var posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                ImGui.SetNextWindowFocus();
                ImGui.SetCursorPos(posInWindow + new Vector2(1, _size.Y + 1));

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

                DrawResultsList(_resultListSize);

                if (_lastHoveredSymbolUi != null)
                {
                    DrawDescriptionPanelLeftOrRight(posInWindow, _resultListSize, _lastHoveredSymbolUi);
                }

                ImGui.PopStyleVar(2);
                DrawSearchInput(posInWindow, _size.X);
            }

            _drawList.AddRect(_posInScreen, OutputPositionOnScreen, Color.Gray);
            ImGui.PopID();
        }
        #endregion

        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput(Vector2 posInWindow, float width)
        {
            //ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(posInWindow);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.SetNextItemWidth(width);
            ImGui.InputText("##symbolbrowserfilter", ref _filter.SearchString, 10);

            if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
            {
                SelectNextSymbolUi(_selectedSymbolUi);
            }
            else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
            {
                SelectPreviousSymbol(_selectedSymbolUi);
            }
            else if (ImGui.IsKeyPressed((ImGuiKey)Key.Return))
            {
                if (_selectedSymbolUi != null)
                {
                    CreateInstance(_selectedSymbolUi.Symbol);
                }
            }

            if (_filter.WasUpdated)
            {
                _selectedSymbolUi = _filter.MatchingSymbolUis.Count > 0
                                        ? _filter.MatchingSymbolUis[0]
                                        : null;
                _selectedItemWasChanged = true;
            }

            var clickedOutside = ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered();
            var shouldCancelConnectionMaker = clickedOutside
                || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                || ImGui.IsKeyDown((ImGuiKey)Key.Esc);

            if (shouldCancelConnectionMaker)
            {
                ConnectionMaker.Cancel();
                Cancel();
            }

            ImGui.PopStyleVar();

            if (!ImGui.IsItemActive())
                return;

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
            }
        }

        private void SelectNextSymbolUi(SymbolUi selectedSymbolUi) => JumpThroughMatchingSymbolList(selectedSymbolUi, 1);
        private void SelectPreviousSymbol(SymbolUi selectedSymbolUi) => JumpThroughMatchingSymbolList(selectedSymbolUi, -1);
        
        private void JumpThroughMatchingSymbolList(SymbolUi currentSelectedSymbolUi, int jump)
        {
            if (_filter.MatchingSymbolUis.Count == 0)
            {
                return;
            }

            var index = _filter.MatchingSymbolUis.IndexOf(currentSelectedSymbolUi);
            index = MathUtils.WrapIndex(index, jump, _filter.MatchingSymbolUis);
            _selectedSymbolUi = _filter.MatchingSymbolUis[index];
            _selectedItemWasChanged = true;
        }

        private void Cancel()
        {
            if (_prepareCommand != null)
            {
                _prepareCommand.Undo();
                _prepareCommand = null;
            }

            Close();
        }

        private void Close()
        {
            THelpers.RestoreImGuiKeyboardNavigation();
            IsOpen = false;
            var win = GraphWindow.GetPrimaryGraphWindow();
            win?.Focus();
        }

        private void DrawResultsList(Vector2 size)
        {
            var itemForHelpIsHovered = false;

            if (ImGui.BeginChildFrame(999, size))
            {
                var gotAMatch = _filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi);
                if (gotAMatch)
                    _selectedSymbolUi = _filter.MatchingSymbolUis[0];

                if ((_selectedSymbolUi == null && SymbolUiRegistry.Entries.Values.Any()))
                    _selectedSymbolUi = SymbolUiRegistry.Entries.Values.FirstOrDefault();

                PrintTypeFilter();

                foreach (var symbolUi in _filter.MatchingSymbolUis)
                {
                    var symbolHash = symbolUi.Symbol.Id.GetHashCode();
                    ImGui.PushID(symbolHash);
                    {
                        var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                                        ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                                        : Color.Gray;
                        ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.Operator.Apply(color).Rgba);

                        var hoverColor = ColorVariations.OperatorHover.Apply(color).Rgba;
                        hoverColor.W = 0.1f;
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

                        var isSelected = symbolUi == _selectedSymbolUi;

                        if (isSelected && _selectedItemWasChanged)
                        {
                            ScrollToMakeItemVisible();
                            _selectedItemWasChanged = false;
                        }

                        ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                        var isHovered = ImGui.IsItemHovered();

                        if (!itemForHelpIsHovered)
                        {
                            var potentialItemForHelp =  DetermineItemForHelp(symbolUi, isSelected, isHovered);
                            if(potentialItemForHelp != null)
                            {
                                itemForHelpIsHovered = isHovered;
                            }
                        }

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbolUi.Symbol);
                        }

                        ImGui.SameLine();

                        ImGui.TextUnformatted(symbolUi.Symbol.Name);
                        ImGui.SameLine();

                        if (!string.IsNullOrEmpty(symbolUi.Symbol.Namespace))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, _namespaceColor);
                            ImGui.Text(symbolUi.Symbol.Namespace);
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                        }

                        ImGui.NewLine();

                        ImGui.PopStyleColor(4);
                    }
                    ImGui.PopID();
                }
            }

            ImGui.EndChildFrame();
        }

        private SymbolUi DetermineItemForHelp(SymbolUi symbolUi, bool isSelected, bool isHovered)
        {
            if (isHovered)
            {
                _lastHoveredSymbolUi = symbolUi;
                return symbolUi;
            }

            if (symbolUi != _lastHoveredSymbolUi)
                return isSelected ? symbolUi : null;
            
            if (UserSettings.Config.SymbolBrowserDescriptionTimeout)
            {
                ExpireLastHoveredSymbolUi(symbolUi);
            }

            return symbolUi;

        }

        /// <summary>
        /// A method to give some time before expiring a hovered symbol.
        /// This is intended to allow the cursor to cross over the scroll bar
        /// of the symbol browser so examples can be interacted with
        /// </summary>
        /// <param name="hoveredSymbolUi"></param>
        async void ExpireLastHoveredSymbolUi(SymbolUi hoveredSymbolUi)
        {
            //if we've already started calculating this one, we can just return
            if (hoveredSymbolUi != _lastHoveredSymbolUi)
                return;

            DateTime startTime = DateTime.Now;
            const int expireTimeMs = 300;

            //returns true if expireTimeMs have elapsed or if the hovered symbol has changed
            Func<bool> Finished = () =>
                DateTime.Now.Subtract(startTime).Milliseconds > expireTimeMs ||
                _lastHoveredSymbolUi != hoveredSymbolUi;

            while (!Finished())
            {
                await Task.Yield();
            }

            //if time's up and the hovered symbol hasnt changed, let's reset the hovered symbol
            if (hoveredSymbolUi == _lastHoveredSymbolUi)
                _canExpireDescription = true;
        }

        private void PrintTypeFilter()
        {
            if (_filter.FilterInputType == null && _filter.FilterOutputType == null)
                return;
            
            ImGui.PushFont(Fonts.FontSmall);

            var inputTypeName = _filter.FilterInputType != null
                                    ? TypeNameRegistry.Entries[_filter.FilterInputType]
                                    : string.Empty;

            var outputTypeName = _filter.FilterOutputType != null
                                     ? TypeNameRegistry.Entries[_filter.FilterOutputType]
                                     : string.Empty;

            var isMultiInput = _filter.OnlyMultiInputs ? "[..]" : "";

            var headerLabel = $"{inputTypeName}{isMultiInput}  -> {outputTypeName}";
            ImGui.TextDisabled(headerLabel);
            ImGui.PopFont();
        }

        private void DrawDescriptionPanelLeftOrRight(Vector2 posInWindow, Vector2 size, SymbolUi highlightedSymbolUi)
        {
            var width = _resultListSize.X;
            var shouldShiftToRight = posInWindow.X + width > GraphCanvas.Current.WindowSize.X;
            var xPositionOffset = shouldShiftToRight ? -width : width;
            var xPosition = posInWindow.X + xPositionOffset;

            var position = new Vector2(xPosition, posInWindow.Y + _size.Y + 1);

            if (xPosition <= 0)
                return;
            
            ImGui.SetCursorPos(position);
            DrawDescriptionPanel(highlightedSymbolUi, size);
        }

        private void DrawDescriptionPanel(SymbolUi itemForHelp, Vector2 size)
        {
            if (itemForHelp == null)
                return;
            
            ExampleSymbolLinking.ExampleSymbols.TryGetValue(itemForHelp.Symbol.Id, out var examples2);
            var hasExamples = examples2 is { Count: > 0 };
            var hasDescription = !string.IsNullOrEmpty(itemForHelp.Description);

            if (!hasExamples && !hasDescription)
                return;
            
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One); // Padding between panels

            if (ImGui.BeginChildFrame(998, size, ImGuiWindowFlags.ChildWindow))
            {
                if (!string.IsNullOrEmpty(itemForHelp.Description))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                    ImGui.TextWrapped(itemForHelp.Description);
                    ImGui.PopStyleColor();
                }

                if (hasExamples)
                {
                    ImGui.Dummy(new Vector2(10, 10));

                    ListExampleOperators(itemForHelp);
                }

                ImGui.EndChildFrame();

                if(_canExpireDescription && ImGui.IsItemHovered())
                {
                    _lastHoveredSymbolUi = null;
                }
            }
            

            ImGui.PopStyleVar();
        }

        public static void ListExampleOperators(SymbolUi itemForHelp)
        {
            if (!ExampleSymbolLinking.ExampleSymbols.TryGetValue(itemForHelp.Symbol.Id, out var examples))
                return;
            
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            foreach (var guid in examples)
            {
                var label = "Example";
                var exampleId = guid;
                DrawExampleOperator(exampleId, label);
            }

            ImGui.PopStyleVar();
        }

        public static void DrawExampleOperator(Guid exampleId, string label)
        {
            var symbolUi = SymbolUiRegistry.Entries[exampleId];
            var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                            ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                            : Color.Gray;

            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorHover.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

            ImGui.SameLine();
            ImGui.Button(label);
            SymbolTreeMenu.HandleDragAndDropForSymbolItem(symbolUi.Symbol);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }
            if (!string.IsNullOrEmpty(symbolUi.Description))
            {
                CustomComponents.TooltipForLastItem(symbolUi.Description);
            }

            ImGui.PopStyleColor(4);
        }

        private void ScrollToMakeItemVisible()
        {
            var scrollTarget = ImGui.GetCursorPos();
            var windowSize = ImGui.GetWindowSize();
            var scrollPos = ImGui.GetScrollY();

            if (scrollTarget.Y < scrollPos)
            {
                ImGui.SetScrollY(scrollTarget.Y);
            }
            else if (scrollTarget.Y + 20 > scrollPos + windowSize.Y)
            {
                ImGui.SetScrollY(scrollPos + windowSize.Y - 20);
            }
        }

        private void CreateInstance(Symbol symbol)
        {
            var commands = new List<ICommand>();
            var parent = GraphCanvas.Current.CompositionOp.Symbol;

            var addSymbolChildCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = PosOnCanvas };
            commands.Add(addSymbolChildCommand);
            addSymbolChildCommand.Do();
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addSymbolChildCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var newChildUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);

            var newInstance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == newChildUi.Id);
            NodeSelection.SetSelectionToChildUi(newChildUi, newInstance);

            if (_prepareCommand != null)
            {
                commands.Add(_prepareCommand);
            }

            foreach (var c in ConnectionMaker.TempConnections)
            {
                switch (c.GetStatus())
                {
                    case ConnectionMaker.TempConnection.Status.SourceIsDraftNode:
                        var outputDef = newSymbolChild.Symbol.GetOutputMatchingType(c.ConnectionType);
                        if (outputDef == null)
                        {
                            Log.Error("Failed to find matching output connection type " + c.ConnectionType);
                            return;
                        }

                        var newConnectionToSource = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                                          sourceSlotId: outputDef.Id,
                                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                                          targetSlotId: c.TargetSlotId);
                        var addConnectionCommand = new AddConnectionCommand(parent, newConnectionToSource, c.MultiInputIndex);
                        addConnectionCommand.Do();
                        commands.Add(addConnectionCommand);
                        break;

                    case ConnectionMaker.TempConnection.Status.TargetIsDraftNode:
                        var inputDef = newSymbolChild.Symbol.GetInputMatchingType(c.ConnectionType);
                        if (inputDef == null)
                        {
                            Log.Warning("Failed to complete node creation");
                            return;
                        }

                        var newConnectionToInput = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                                         sourceSlotId: c.SourceSlotId,
                                                                         targetParentOrChildId: newSymbolChild.Id,
                                                                         targetSlotId: inputDef.Id);
                        var connectionCommand = new AddConnectionCommand(parent, newConnectionToInput, c.MultiInputIndex);
                        connectionCommand.Do();
                        commands.Add(connectionCommand);
                        break;
                }
            }

            var newCommand = new MacroCommand("Insert Op", commands);
            UndoRedoStack.Add(newCommand);
            ConnectionMaker.Cancel();
            Close();
        }

        /// <summary>
        /// required to correctly restore original state when closing the browser  
        /// </summary>
        private MacroCommand _prepareCommand;
        #endregion

        private readonly SymbolFilter _filter = new SymbolFilter();

        public Vector2 PosOnCanvas { get; private set; }
        public Vector2 OutputPositionOnScreen => _posInScreen + _size;
        public bool IsOpen;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;
        
        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private bool _selectedItemWasChanged;

        private static readonly Vector2 _resultListSize = new Vector2(250, 300);
        private readonly Vector4 _namespaceColor = new Color(1, 1, 1, 0.4f);


        private SymbolUi _selectedSymbolUi;
        private SymbolUi _lastHoveredSymbolUi;
        private bool _canExpireDescription;
        private static readonly int UiId = "DraftNode".GetHashCode();
    }
}