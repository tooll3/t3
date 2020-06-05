using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;

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
        public void OpenAt(Vector2 positionOnCanvas, Type filterInputType, Type filterOutputType)
        {
            _isOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filter.FilterInputType = filterInputType;
            _filter.FilterOutputType = filterOutputType;
            _filter.SearchString = "";
            _selectedSymbolUi = null;
            _filter.UpdateIfNecessary();
            DisableImGuiKeyboardNavigation();

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

                Log.Debug("open create with tab");
                OpenAt(GraphCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos), null, null);
                return;
            }

            Current = this;

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
            
            
            if(_filter.WasUpdated)
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
                Log.Debug("Closing...");
                Close();
            }

            ImGui.PopStyleVar();

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(0))
                {
                    PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                }
            }
        }

        private void Close()
        {
            RestoreImGuiKeyboardNavigation();
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

                    var headerLabel = $"{inputTypeName} -> {outputTypeName}";
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
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorVariations.OperatorHover.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

                        var isSelected = symbolUi == _selectedSymbolUi;
                        if (isSelected && _selectedItemWasChanged)
                        {
                            ScrollToMakeItemVisible();
                            _selectedItemWasChanged = false;
                        }

                        ImGui.Selectable("", isSelected);

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbolUi.Symbol);
                        }

                        ImGui.SameLine();
                        ImGui.Text(symbolUi.Symbol.Name);
                        if (!String.IsNullOrEmpty(symbolUi.Symbol.Namespace))
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled(symbolUi.Symbol.Namespace);
                        }

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
            var parent = GraphCanvas.Current.CompositionOp.Symbol;
            var newChildUi = NodeOperations.CreateInstance(symbol, parent, PosOnCanvas);
            var newInstance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == newChildUi.Id);
            SelectionManager.SetSelection(newChildUi, newInstance);

            // TODO: Refactor this by moving it to connectionMaker
            // if (ConnectionMaker.TempConnections != null && symbol.InputDefinitions.Any())
            // {
            //     var temp = ConnectionMaker.TempConnections;
            //     if (temp.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
            //     {
            //         // connecting to output
            //         ConnectionMaker.CompleteConnectionFromBuiltNode(parent, newChildUi.SymbolChild,
            //                                                         symbol.GetOutputMatchingType(_filter.FilterInputType));
            //     }
            //     else
            //     {
            //         // connecting to input
            //         ConnectionMaker.CompleteConnectionsIntoBuiltNode(parent, newChildUi.SymbolChild, symbol.GetInputMatchingType(_filter.FilterInputType));
            //     }
            // }
            // else
            // {
            //     ConnectionMaker.Cancel();
            // }
            ConnectionMaker.CompleteConnectsToBuiltNode(parent, newChildUi.SymbolChild);
            
            
            ConnectionMaker.Cancel();

            Close();
        }

        private readonly SymbolFilter _filter = new SymbolFilter();
        #endregion

        // This has to be called on open
        private void DisableImGuiKeyboardNavigation()
        {
            // Keep navigation setting to restore after window gets closed
            _keepNavEnableKeyboard = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableKeyboard) != ImGuiConfigFlags.None;
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
        }

        private void RestoreImGuiKeyboardNavigation()
        {
            if (_keepNavEnableKeyboard)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        }

        public Vector2 PosOnCanvas { get; private set; }

        public Vector2 OutputPositionOnScreen => _posInScreen + _size;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        private bool _isOpen;
        private static readonly Vector2 ResultListSize = new Vector2(300, 200);

        private SymbolUi _selectedSymbolUi;
        private static readonly int UiId = "DraftNode".GetHashCode();
        private bool _keepNavEnableKeyboard;
        public static SymbolBrowser Current;
    }
}