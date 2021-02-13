using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;

namespace T3.Gui.Windows
{
    /// <summary>
    /// Shows a tree of all defined symbols sorted by namespace 
    /// </summary>
    public class SymbolLibrary : Window
    {
        public SymbolLibrary()
        {
            _filter.SearchString = "";
            Config.Title = "Symbol Library";
            PopulateTree();
        }

        protected override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 5);
            {


                ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
                if (ImGui.Button("Clear"))
                {
                    _filter.SearchString = "";
                }

                ImGui.SameLine();
                if (ImGui.InputText("##Filter", ref _filter.SearchString, 100))
                {
                    _selectedSymbol = null;
                }
                
                ImGui.Separator();

                ImGui.BeginChild("scrolling");
                {
                    if (string.IsNullOrEmpty(_filter.SearchString))
                    {
                        DrawTree();
                    }
                    else
                    {
                        DrawList();
                    }
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
            
            if (ImGui.IsMouseReleased(0))
            {
                StopDrag();
            }
        }

        private void DrawTree()
        {
            DrawNode(_tree);
        }

        
        private static Symbol _selectedSymbol= null; 
        
        private void DrawList()
        {
            _filter.UpdateIfNecessary();
            foreach (var symbolUi in _filter.MatchingSymbolUis)
            {
                DrawSymbolItem(symbolUi.Symbol);
            }

            var showUsages = _filter.MatchingSymbolUis.Count == 1 || _selectedSymbol != null;
            if (showUsages)
            {
                var symbol = _selectedSymbol ?? _filter.MatchingSymbolUis[0].Symbol;
                ImGui.Separator();
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.Text("Is used these Symbols...");
                ImGui.PopFont();
                CustomComponents.HelpText("Note: This only includes currently loaded (instanced) Operators.");

                var graphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();

                var usagesInSymbolInstances = new Dictionary<Symbol, List<Instance>>();
                foreach (var symbolInstance in symbol.InstancesOfSymbol)
                {
                    var parents = NodeOperations.GetParentInstances(symbolInstance, includeChildInstance: true).ToList();
                    if (parents.Count < 2)
                        continue;
                    
                    var compositionSymbol = parents[parents.Count - 2].Symbol;
                    var instance = parents[parents.Count - 1];

                    if (!usagesInSymbolInstances.TryGetValue(compositionSymbol, out var list))
                    {
                        list = new List<Instance>();
                        usagesInSymbolInstances[compositionSymbol] = list;
                    }

                    if (list.All(c => c.SymbolChildId != instance.SymbolChildId)) 
                    {
                        list.Add(instance);
                    }
                }

                foreach (var (compositionSymbol, instances) in usagesInSymbolInstances)
                {
                    ImGui.PushFont(Fonts.FontBold);
                    ImGui.TextColored(Color.Gray,compositionSymbol.Name);
                    ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.TextColored(Color.Gray, " - " + compositionSymbol.Namespace);
                    
                    foreach (var instance in instances)
                    {
                        ImGui.PushID(instance.SymbolChildId.GetHashCode());
                        var instanceParent = instance.Parent;
                        var symbolChild = instanceParent.Symbol.Children.SingleOrDefault(child => child.Id == instance.SymbolChildId);
                        if (symbolChild == null)
                        {
                            Log.Error($"Can't find SymbolChild of Instance {instance.Symbol.Name} ({instance.SymbolChildId}) in parent {instanceParent.Symbol.Name}");
                            continue;
                        }

                        if(ImGui.Selectable(symbolChild.ReadableName))
                        {
                            graphWindow?.GraphCanvas.SetComposition(NodeOperations.BuildIdPathForInstance(instanceParent),
                                                                     ScalableCanvas.Transition.Undefined);

                            var childUi = SymbolUiRegistry.Entries[compositionSymbol.Id].ChildUis.Single(cUi => cUi.Id == instance.SymbolChildId);
                            SelectionManager.SetSelection(childUi, instance);
                            FitViewToSelectionHandling.FitViewToSelection();
                        }
                        ImGui.PopID();
                    }
                }
            }
        }

        private void StopDrag()
        {
            T3Ui.DraggingIsInProgress = false;
            _dropData = T3Ui.NotDroppingPointer;
        }

        private void DrawNode(NamespaceTree subtree)
        {
            ImGui.PushID(subtree.Name);
            ImGui.SetNextItemWidth(10);
            if (ImGui.TreeNode(subtree.Name))
            {
                HandleDropTarget(subtree);

                foreach (var subspace in subtree.Children)
                {
                    DrawNode(subspace);
                }

                foreach (var symbol in subtree.Symbols)
                {
                    DrawSymbolItem(symbol);
                }

                ImGui.TreePop();
            }
            else
            {
                if (T3Ui.DraggingIsInProgress)
                {
                    ImGui.SameLine();
                    ImGui.PushID("DropButton");
                    ImGui.Button("  <-", new Vector2(50, 15));
                    HandleDropTarget(subtree);
                    ImGui.PopID();
                }
            }

            ImGui.PopID();
        }

        private static void DrawSymbolItem(Symbol symbol)
        {
            ImGui.PushID(symbol.Id.GetHashCode());
            {
                var color = symbol.OutputDefinitions.Count > 0
                                ? TypeUiRegistry.GetPropertiesForType(symbol.OutputDefinitions[0]?.ValueType).Color
                                : Color.Gray;
                ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorHover.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);
                //ImGui.Selectable("", symbol == _selectedSymbol);

                if (ImGui.Button(symbol.Name))
                {
                    _selectedSymbol = symbol;
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.BeginDragDropSource())
                    {
                        if (_dropData == new IntPtr(0))
                        {
                            _guidSting = symbol.Id.ToString() + "|";
                            _dropData = Marshal.StringToHGlobalUni(_guidSting);
                            T3Ui.DraggingIsInProgress = true;
                        }

                        ImGui.SetDragDropPayload("Symbol", _dropData, (uint)(_guidSting.Length * sizeof(Char)));

                        ImGui.Button(symbol.Name + "Dropping");
                        ImGui.EndDragDropSource();
                    }
                }
                

                ImGui.PopStyleColor(4);
            }
            ImGui.PopID();
        }

        private void HandleDropTarget(NamespaceTree subtree)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("Symbol");
                if (ImGui.IsMouseReleased(0))
                {
                    string myString = null;
                    try
                    {
                        myString = Marshal.PtrToStringAuto(payload.Data);
                    }
                    catch (NullReferenceException)
                    {
                        Log.Error("unable to get drop data");                        
                    }
                    
                    if (myString != null)
                    {
                        var guidString = myString.Split('|')[0];
                        var guid = Guid.Parse(guidString);
                        Log.Debug("dropped symbol here" + payload + " " + myString + "  " + guid);
                        MoveSymbolToNamespace(guid, subtree);
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        private void MoveSymbolToNamespace(Guid symbolId, NamespaceTree nameSpace)
        {
            var symbol = SymbolRegistry.Entries[symbolId];
            symbol.Namespace = nameSpace.GetNamespace();
            Log.Debug($"moving {symbol.Name} to {symbol.Namespace}");
            PopulateTree();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private void PopulateTree()
        {
            _tree = new NamespaceTree("root");
            _tree.Clear();

            foreach (var symbol in SymbolRegistry.Entries)
            {
                _tree.SortInOperator(symbol.Value);
            }
        }

        private NamespaceTree _tree;

        /// <summary>
        /// A nested container that can contain further instances of OperatorTypeTree
        /// </summary>
        public class NamespaceTree
        {
            public string Name { get; }
            public List<NamespaceTree> Children { get; } = new List<NamespaceTree>();
            private NamespaceTree Parent { get; }

            public NamespaceTree(string name, NamespaceTree parent = null)
            {
                Name = name;
                Parent = parent;
            }

            public string GetNamespace()
            {
                var list = new List<string>();
                var t = this;
                while (t.Parent != null)
                {
                    list.Insert(0, t.Name);
                    t = t.Parent;
                }

                return string.Join(".", list);
            }

            public void Clear()
            {
                Children.Clear();
                Symbols.Clear();
            }

            public void SortInOperator(Symbol symbol)
            {
                if (symbol?.Namespace == null)
                {
                    return;
                }

                var spaces = symbol.Namespace.Split('.');

                var currentNode = this;
                var expandingSubTree = false;

                foreach (var spaceName in spaces)
                {
                    if (spaceName == "")
                        continue;

                    if (!expandingSubTree)
                    {
                        var node = currentNode.FindNodeDataByName(spaceName);
                        if (node != null)
                        {
                            currentNode = node;
                        }
                        else
                        {
                            expandingSubTree = true;
                        }
                    }

                    if (!expandingSubTree)
                        continue;

                    var newNode = new NamespaceTree(spaceName, currentNode);
                    currentNode.Children.Add(newNode);
                    currentNode = newNode;
                }

                currentNode.Symbols.Add(symbol);
            }

            private NamespaceTree FindNodeDataByName(String name)
            {
                return Children.FirstOrDefault(n => n.Name == name);
            }

            public readonly List<Symbol> Symbols = new List<Symbol>();
        }

        private static IntPtr _dropData = new IntPtr(0);
        private static string _guidSting;
        private SymbolFilter _filter = new SymbolFilter();
    }
}