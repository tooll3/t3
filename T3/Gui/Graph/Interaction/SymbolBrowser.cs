using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
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
            _isOpen = true;
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
            if (!_isOpen)
            {
                if (!ImGui.IsWindowFocused() || !ImGui.IsKeyReleased((int)Key.Tab))
                    return;

                if (SelectionManager.GetSelectedChildUis().Count() == 1)
                {
                    var childUi = SelectionManager.GetSelectedChildUis().ToList()[0];
                    {
                        var instance = SelectionManager.GetInstanceForSymbolChildUi(childUi);
                        ConnectionMaker.OpenBrowserWithSingleSelection(this, childUi, instance);
                    }
                }
                else
                {
                    OpenAt(GraphCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos + new Vector2(-4, -20)), null, null, false, null);
                }

                return;
            }

            T3Ui.OpenedPopUpName = "SymbolBrowser";
            //Current = this;

            _filter.UpdateIfNecessary();
            
            ImGui.PushID(UiId);
            {
                _posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                DrawMatchesList();
                DrawSearchInput();
            }
            ImGui.PopID();
        }
        #endregion

        private bool _selectedItemWasChanged;

        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput()
        {
            ImGui.SetCursorPos(_posInWindow);
            ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.SetNextItemWidth(_size.X);
            ImGui.InputText("##filter", ref _filter.SearchString, 10);
            _drawList.AddRect(_posInScreen, _posInScreen + _size, Color.Gray);

            if (ImGui.IsKeyReleased((int)Key.CursorDown))
            {
                if (_filter.MatchingSymbolUis.Count > 0)
                {
                    var index = _filter.MatchingSymbolUis.IndexOf(_selectedSymbolUi);
                    index++;
                    index %= _filter.MatchingSymbolUis.Count;
                    _selectedSymbolUi = _filter.MatchingSymbolUis[index];

                    _selectedItemWasChanged = true;
                }
            }
            else if (ImGui.IsKeyReleased((int)Key.CursorUp))
            {
                if (_filter.MatchingSymbolUis.Count > 0)
                {
                    var index = _filter.MatchingSymbolUis.IndexOf(_selectedSymbolUi);
                    index--;
                    if (index < 0)
                        index = _filter.MatchingSymbolUis.Count - 1;

                    _selectedSymbolUi = _filter.MatchingSymbolUis[index];
                    _selectedItemWasChanged = true;
                }
            }
            else if (ImGui.IsKeyPressed((int)Key.Return))
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
            if (clickedOutside
                || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                || ImGui.GetIO().KeysDownDuration[(int)Key.Esc] > 0)
            {
                ConnectionMaker.Cancel();
                //Log.Debug("Closing...");
                Cancel();
            }

            ImGui.PopStyleVar();

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                }
            }
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
            //T3Ui.OpenedPopUpName = null;
            _isOpen = false;
        }

        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(1, _size.Y + 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            if (ImGui.BeginChildFrame(999, ResultListSize))
            {
                if (_filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi))
                    _selectedSymbolUi = _filter.MatchingSymbolUis[0];

                if ((_selectedSymbolUi == null && SymbolUiRegistry.Entries.Values.Any()))
                    _selectedSymbolUi = SymbolUiRegistry.Entries.Values.FirstOrDefault();

                //  Print type filter
                if (_filter.FilterInputType != null || _filter.FilterOutputType != null)
                {
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

                foreach (var symbolUi in _filter.MatchingSymbolUis)
                {
                    ImGui.PushID(symbolUi.Symbol.Id.GetHashCode());
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

                        
                        ImGui.Selectable("", isSelected);
                        
                        if(!string.IsNullOrEmpty(symbolUi.Description))
                            CustomComponents.TooltipForLastItem(symbolUi.Description);

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbolUi.Symbol);
                        }

                        ImGui.SameLine();
                        
                        ImGui.Text(symbolUi.Symbol.Name);
                        ImGui.SameLine();
                        
                        if (!string.IsNullOrEmpty(symbolUi.Symbol.Namespace))
                        {
                            ImGui.TextDisabled(symbolUi.Symbol.Namespace);
                            ImGui.SameLine();
                        }

                        if (!string.IsNullOrEmpty(symbolUi.Description))
                        {
                            ImGui.TextDisabled("(?)");
                            ImGui.SameLine();
                        }

                        if (ExampleSymbolLinking.ExampleSymbols.TryGetValue(symbolUi.Symbol.Id, out var examples))
                        {
                            ImGui.TextDisabled($"{examples.Count} examples");
                            ImGui.SameLine();
                        }
                        
                        ImGui.NewLine();

                        ImGui.PopStyleColor(4);
                    }
                    ImGui.PopID();
                }
            }

            ImGui.EndChildFrame();
            ImGui.PopStyleVar(2);
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
            SelectionManager.SetSelectionToChildUi(newChildUi, newInstance);

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
                            Log.Error("Failed to find matching output connection type "+ c.ConnectionType);
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

            var newCommand = new MacroCommand("Insert and connect node", commands);
            UndoRedoStack.Add(newCommand);
            ConnectionMaker.Cancel();
            Close();
        }

        

        /// <summary>
        /// required correctly restore original state when closing the browser  
        /// </summary>
        private MacroCommand _prepareCommand;
        #endregion


        private readonly SymbolFilter _filter = new SymbolFilter();
        
        public Vector2 PosOnCanvas { get; private set; }
        public Vector2 OutputPositionOnScreen => _posInScreen + _size;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        public bool _isOpen;
        private static readonly Vector2 ResultListSize = new Vector2(400, 300);

        private SymbolUi _selectedSymbolUi;
        private static readonly int UiId = "DraftNode".GetHashCode();
    }
}