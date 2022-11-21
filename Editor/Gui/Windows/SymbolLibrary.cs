using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using GraphWindow = T3.Editor.Gui.Graph.GraphWindow;

namespace T3.Editor.Gui.Windows
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
            _treeNode.PopulateCompleteTree();
        }

        protected override void DrawContent()
        {
            _renameNamespaceDialog.Draw(_subtreeNodeToRename);
            
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
                
                ImGui.SameLine();
                if (ImGui.Button("Update"))
                {
                    _treeNode.PopulateCompleteTree();
                    ExampleSymbolLinking.UpdateExampleLinks();
                    SymbolAnalysis.Update();
                }
                CustomComponents.TooltipForLastItem("Rescans the current symbol tree. This can useful after renaming namespaces.");

                ImGui.Separator();

                ImGui.BeginChild("scrolling");
                {
                    if (string.IsNullOrEmpty(_filter.SearchString))
                    {
                        DrawNode(_treeNode);
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

        private void DrawList()
        {
            _filter.UpdateIfNecessary();
            foreach (var symbolUi in _filter.MatchingSymbolUis)
            {
                SymbolTreeMenu.DrawSymbolItem(symbolUi.Symbol);
            }

            var showUsages = _filter.MatchingSymbolUis.Count == 1 || _selectedSymbol != null;
            if (showUsages)
            {
                var symbol = _selectedSymbol ?? _filter.MatchingSymbolUis[0].Symbol;
                ImGui.Separator();
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted("Is used these Symbols...");
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
                    ImGui.TextColored(Color.Gray, compositionSymbol.Name);
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

                        if (ImGui.Selectable(symbolChild.ReadableName))
                        {
                            graphWindow?.GraphCanvas.SetComposition(NodeOperations.BuildIdPathForInstance(instanceParent),
                                                                    ICanvas.Transition.Undefined);

                            var childUi = SymbolUiRegistry.Entries[compositionSymbol.Id].ChildUis.Single(cUi => cUi.Id == instance.SymbolChildId);
                            NodeSelection.SetSelectionToChildUi(childUi, instance);
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
            //_dropData = T3Ui.NotDroppingPointer;
        }

        private NamespaceTreeNode _subtreeNodeToRename;
        
        private void DrawNode(NamespaceTreeNode subtree)
        {
            if (subtree.Name == NamespaceTreeNode.RootNodeId)
            {
                DrawNodeItems(subtree);
            }
            else
            {
                ImGui.PushID(subtree.Name);
                ImGui.SetNextItemWidth(10);

                var isOpen = ImGui.TreeNode(subtree.Name);
                CustomComponents.ContextMenuForItem(() =>
                                                    {
                                                        if (ImGui.MenuItem("Rename Namespace"))
                                                        {
                                                            _subtreeNodeToRename = subtree;
                                                            _renameNamespaceDialog.ShowNextFrame();
                                                        }
                                                    });
                
                
                if (isOpen)
                {
                    HandleDropTarget(subtree);

                    DrawNodeItems(subtree);

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
        }

        private void DrawNodeItems(NamespaceTreeNode subtree)
        {
            // Using a for loop to prevent modification during iteration exception
            for (var index = 0; index < subtree.Children.Count; index++)
            {
                var subspace = subtree.Children[index];
                DrawNode(subspace);
            }

            for (var index = 0; index < subtree.Symbols.ToList().Count; index++)
            {
                var symbol = subtree.Symbols.ToList()[index];
                SymbolTreeMenu.DrawSymbolItem(symbol);
            }
        }
        
        private void HandleDropTarget(NamespaceTreeNode subtree)
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

        private void MoveSymbolToNamespace(Guid symbolId, NamespaceTreeNode nameSpace)
        {
            var symbol = SymbolRegistry.Entries[symbolId];
            symbol.Namespace = nameSpace.GetAsString();
            Log.Debug($"moving {symbol.Name} to {symbol.Namespace}");
            _treeNode.PopulateCompleteTree();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private NamespaceTreeNode _treeNode = new NamespaceTreeNode(NamespaceTreeNode.RootNodeId);
        private readonly SymbolFilter _filter = new SymbolFilter();
        private static Symbol _selectedSymbol;
        private static RenameNamespaceDialog _renameNamespaceDialog = new RenameNamespaceDialog();
    }
}