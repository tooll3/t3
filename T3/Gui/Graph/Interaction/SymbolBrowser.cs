using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Represents the placeholder for a new <see cref="GraphNode"/> on the <see cref="GraphCanvas"/>. 
    /// It can be connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class SymbolBrowser
    {
        #region public API ------------------------------------------------------------------------
        public void OpenAt(Vector2 positionOnCanvas, Type type)
        {
            IsOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filterType = type;
            _searchString = "";
            _selectedSymbol = null;
            _filter.Update();

            if (_selectedSymbol == null && _filter.MatchingSymbols.Count > 0)
            {
                _selectedSymbol = _filter.MatchingSymbols[0];
            }
        }


        public void Draw()
        {
            if (!IsOpen)
                return;

            Current = this;

            _filter.Update();

            ImGui.PushID(_uiId);
            {
                _posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                DrawMatchesList();
                DrawSearchInput();
            }
            ImGui.PopID();
        }

        public void Cancel()
        {
            IsOpen = false;
        }
        #endregion


        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput()
        {
            _drawList.AddRect(_posInScreen, _posInScreen + Size, Color.Gray);

            ImGui.SetCursorPos(_posInWindow);
            ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.InputText("##filter", ref _searchString, 10);

            if (ImGui.IsKeyReleased((int)Key.CursorRight))
            {
                if (_filter.MatchingSymbols.Count > 0)
                {
                    var index = _filter.MatchingSymbols.IndexOf(_selectedSymbol);
                    index++;
                    index %= _filter.MatchingSymbols.Count;
                    _selectedSymbol = _filter.MatchingSymbols[index];
                }
            }
            else if (ImGui.IsKeyReleased((int)Key.CursorLeft))
            {
                if (_filter.MatchingSymbols.Count > 0)
                {
                    var index = _filter.MatchingSymbols.IndexOf(_selectedSymbol);
                    index--;
                    if (index < 0)
                        index = _filter.MatchingSymbols.Count - 1;

                    _selectedSymbol = _filter.MatchingSymbols[index];
                }

            }
            else if (ImGui.IsKeyPressed((int)Key.Return))
            {
                if (_selectedSymbol != null)
                {
                    CreateInstance(_selectedSymbol);
                }
            }

            if (ImGui.IsItemDeactivated() || ImGui.GetIO().KeysDownDuration[(int)Key.Esc] > 0)
            {
                IsOpen = false;
                ConnectionMaker.Cancel();
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


        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(91 + 8, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            var typeUi = TypeUiRegistry.Entries[_filterType];


            if (ImGui.BeginChildFrame(234, new Vector2(150, 200)))
            {
                ImGui.PushFont(ImGuiDx11Impl.FontSmall);
                ImGui.TextDisabled(_filterType.Name);
                ImGui.PopFont();


                if (_selectedSymbol == null && SymbolRegistry.Entries.Values.Any())
                    _selectedSymbol = SymbolRegistry.Entries.Values.FirstOrDefault();

                foreach (var symbol in _filter.MatchingSymbols)
                {
                    ImGui.PushID(symbol.Id.GetHashCode());
                    {
                        ImGui.Selectable("", symbol == _selectedSymbol);

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbol);
                        }
                        ImGui.SameLine();
                        ImGui.Text(symbol.Name);
                        if (!String.IsNullOrEmpty(symbol.Namespace))
                        {
                            ImGui.TextDisabled(symbol.Namespace);
                            ImGui.SameLine();
                        }
                    }
                    ImGui.PopID();
                }
            }
            ImGui.EndChildFrame();
            ImGui.PopStyleVar(2);
        }


        private void CreateInstance(Symbol symbol)
        {
            var parent = GraphCanvas.Current.CompositionOp.Symbol;
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = PosOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            if (symbol.InputDefinitions.Any())
            {
                var temp = ConnectionMaker.TempConnection;
                ConnectionMaker.CompleteConnectionIntoBuiltNode(
                    parent,
                    newSymbolChild,
                    _filter.GetInputMatchingType(symbol, _filterType));
            }
            else
            {
                ConnectionMaker.Cancel();
            }

            IsOpen = false;
        }

        /// <summary>
        /// Provides a regular expression to filter for matching <see cref="Symbol"/>s
        /// </summary>
        private class Filter
        {
            public void Update()
            {
                bool needsUpdate = false;

                if (_currentSearchString != _searchString)
                {
                    _currentSearchString = _searchString;
                    var pattern = string.Join(".*", _currentSearchString.ToCharArray());
                    _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                    needsUpdate = true;
                }

                if (_currentType != _filterType)
                {
                    _currentType = _filterType;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    UpdateMatchingSymbols();
                }
            }

            private Type _currentType;
            public Symbol SelectedSymbol = null;

            private void UpdateMatchingSymbols()
            {
                var parentSymbols = new List<Symbol>(GraphCanvas.Current.GetParentSymbols());

                MatchingSymbols = new List<Symbol>();

                foreach (var symbol in SymbolRegistry.Entries.Values)
                {
                    if (parentSymbols.Contains(symbol))
                        continue;

                    var matchingInputDef = GetInputMatchingType(symbol, _filterType);
                    if (matchingInputDef == null)
                        continue;

                    if (!_currentRegex.IsMatch(symbol.Name))
                        continue;

                    MatchingSymbols.Add(symbol);
                }
            }

            public Symbol.InputDefinition GetInputMatchingType(Symbol symbol, Type type)
            {
                foreach (var inputDefinition in symbol.InputDefinitions)
                {
                    if (inputDefinition.DefaultValue.ValueType == type)
                        return inputDefinition;
                }
                return null;
            }

            public List<Symbol> MatchingSymbols { private set; get; } = null;

            private Regex _currentRegex;
            private string _currentSearchString = null;

            internal bool Match(Symbol symbol)
            {
                return _currentRegex.IsMatch(symbol.Name);
            }
        }
        private Filter _filter = new Filter();

        #endregion

        private static Type _filterType;

        public List<Symbol.Connection> ConnectionsIn = new List<Symbol.Connection>();
        public Symbol.Connection ConnectionOut = null;

        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;

        private bool _focusInputNextTime = false;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        public bool IsOpen { get; private set; } = false;

        private Symbol _selectedSymbol = null;
        private readonly static int _uiId = "DraftNode".GetHashCode();
        private static string _searchString = "";

        public static SymbolBrowser Current;
    }
}
