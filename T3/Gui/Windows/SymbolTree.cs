using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Windows
{
    /// <summary>
    /// Shows a tree of all defined symbols sorted by namespace 
    /// </summary>
    public class SymbolTree : Window
    {
        public SymbolTree()
        {
            Config.Title = "Symbols";
            PopulateTree();
        }

        protected override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 5);
            {
                ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
                if (ImGui.Button("Clear"))
                {
                    _filterString = "";
                }

                ImGui.SameLine();
                ImGui.InputText("##Filter", ref _filterString, 100);
                ImGui.Separator();
                ImGui.BeginChild("scrolling");
                {
                    DrawTree();
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        private void DrawTree()
        {
            if (!ImGui.IsMouseDown(0))
            {
                StopDrag();
            }

            DrawNode(_tree);
        }

        private static IntPtr _dropData = new IntPtr(0);
        private static string _guidSting;

        private void StopDrag()
        {
            _dropData = new IntPtr(0);
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
                    ImGui.PushID(symbol.Id.GetHashCode());
                    {
                        ImGui.Button(symbol.Name);

                        if (ImGui.IsItemActive())
                        {
                            if (ImGui.BeginDragDropSource())
                            {
                                if (_dropData == new IntPtr(0))
                                {
                                    _guidSting = symbol.Id.ToString() + "|";
                                    _dropData = Marshal.StringToHGlobalUni(_guidSting);
                                }

                                ImGui.SetDragDropPayload("Symbol", _dropData, (uint)(_guidSting.Length * sizeof(Char)));

                                ImGui.Button(symbol.Name + "Dropping");
                                ImGui.EndDragDropSource();
                            }
                        }
                    }
                    ImGui.PopID();
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.SameLine();
                ImGui.Button("  <-");
                HandleDropTarget(subtree);
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
                    var myString = Marshal.PtrToStringAuto(payload.Data);
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

        private string _filterString = "";
    }
}