using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

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
            
            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);

            if (_symbolUsageReference != null)
            {
                ImGui.Text("Usages of " + _symbolUsageReference.Name + ":");
                if (ImGui.Button("Clear"))
                {
                    _symbolUsageReference = null;
                }
                else
                {
                    ImGui.Separator();

                    ImGui.BeginChild("scrolling");
                    {
                        if (SymbolAnalysis.DetailsInitialized && SymbolAnalysis.InformationForSymbolIds.TryGetValue(_symbolUsageReference.Id, out var info))
                        {
                            foreach (var s in info.DependingSymbolIds)
                            {
                                var symbol = SymbolRegistry.Entries[s];
                                SymbolTreeMenu.DrawSymbolItem(symbol);
                            }
                        }
                    }
                    ImGui.EndChild();
                }
            }
            else
            {
                DrawSymbols();
            }
            
            ImGui.PopStyleVar(1);

            if (ImGui.IsMouseReleased(0))
            {
                StopDrag();
            }
        }

        private void DrawSymbols()
        {
            //ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

            CustomComponents.DrawInputFieldWithPlaceholder("Search symbols...", ref _filter.SearchString, -60);

            ImGui.SameLine();
            if (ImGui.Button("Rescan"))
            {
                _treeNode.PopulateCompleteTree();
                ExampleSymbolLinking.UpdateExampleLinks();
                SymbolAnalysis.UpdateDetails();
            }

            CustomComponents.TooltipForLastItem("Rescans the current symbol tree. This can useful after renaming namespaces.");

            ImGui.Separator();

            ImGui.BeginChild("scrolling");
            {
                var completeTree = string.IsNullOrEmpty(_filter.SearchString);
                if (completeTree)
                {
                    DrawNode(_treeNode);
                }
                else if(_filter.SearchString.Contains("?"))
                {
                    DrawRandomPromptList();
                }
                else
                {
                    DrawList();
                }
            }
            ImGui.EndChild();
        }

        private void DrawList()
        {
            _filter.UpdateIfNecessary();
            foreach (var symbolUi in _filter.MatchingSymbolUis)
            {
                SymbolTreeMenu.DrawSymbolItem(symbolUi.Symbol);
            }
        }

        private int _randomSeed;
        private List<Symbol> _libSymbols;
        
        private void DrawRandomPromptList()
        {
            var keep = _filter.SearchString;
            _filter.SearchString = "lib.";
            _filter.UpdateIfNecessary();
            _filter.SearchString = keep;

            if (_libSymbols == null)
            {
                _libSymbols = new List<Symbol>();
                foreach (var s in SymbolRegistry.Entries.Values)
                {
                    if(s.Namespace.StartsWith("lib.") && !s.Name.StartsWith("_"))
                        _libSymbols.Add(s);
                }
            }
            
            ImGui.Indent();
            FormInputs.AddSectionHeader("Random Prompts");
            
            ImGui.AlignTextToFramePadding();
            ImGui.InputInt("## seed", ref _randomSeed);

            FormInputs.AddVerticalSpace();
            if (_randomSeed == 0)
            {
                _randomSeed = (int)(ImGui.GetFrameCount() * 374761393U & 1023U);
            }
            
            var count = _filter.SearchString.Count(c => c == '?');
            var libOperatorCount = _libSymbols.Count;
            for (uint i = 0; i < count; i++)
            {
                var f = MathUtils.Hash01((uint)((i + 42 * _randomSeed * 668265263U) & 0x7fffffff ));
                var randomIndex = (int)(f * libOperatorCount).Clamp(0, libOperatorCount-1);
                SymbolTreeMenu.DrawSymbolItem(_libSymbols[randomIndex]);
            }
        }

        private static void StopDrag()
        {
            T3Ui.DraggingIsInProgress = false;
        }

        private NamespaceTreeNode _subtreeNodeToRename;
        private bool _openedLibFolderOnce;
        
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
                if (subtree.Name == "lib" && !_openedLibFolderOnce)
                {
                    ImGui.SetNextItemOpen(true);
                    _openedLibFolderOnce = true;
                }

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

        private readonly NamespaceTreeNode _treeNode = new(NamespaceTreeNode.RootNodeId);
        private readonly SymbolFilter _filter = new();
        private static readonly RenameNamespaceDialog _renameNamespaceDialog = new();
        public static Symbol _symbolUsageReference;
    }
}