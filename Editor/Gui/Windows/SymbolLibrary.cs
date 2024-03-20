using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

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
            EditableSymbolProject.CompilationComplete += _treeNode.PopulateCompleteTree;
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
                            foreach (var symbol in info.DependingSymbols)
                            {
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
            _filter.UpdateIfNecessary(null);
            foreach (var symbolUi in _filter.MatchingSymbolUis)
            {
                SymbolTreeMenu.DrawSymbolItem(symbolUi.Symbol);
            }
        }

        private int _randomSeed;
        private List<Symbol> _allLibSymbols;
        private float _promptComplexity= 0.25f;
        
        private void DrawRandomPromptList()
        {
            
            ImGui.Indent();
            FormInputs.AddSectionHeader("Random Prompts");

            var listNeedsUpdate = _allLibSymbols == null;
            
            //ImGui.AlignTextToFramePadding();f
            FormInputs.SetIndent(80 * T3Ui.UiScaleFactor);
            //listNeedsUpdate |= ImGui.InputInt("## seed", ref _randomSeed);
            FormInputs.AddInt("Seed", ref _randomSeed);
            listNeedsUpdate |= FormInputs.AddFloat("Complexity", ref _promptComplexity, 0, 1,0.02f,true);
            FormInputs.SetIndentToLeft();
            
            FormInputs.AddVerticalSpace();
            
            // Rebuild list if necessary
            if (listNeedsUpdate)
            {
                // Count all lib ops
                if (_allLibSymbols == null)
                {
                    _allLibSymbols = new List<Symbol>();
                    foreach (var s in EditorSymbolPackage.AllSymbols)
                    {
                        if(s.Namespace.StartsWith("lib.") && !s.Name.StartsWith("_"))
                            _allLibSymbols.Add(s);
                    }
                }
                
                // Filter 
                var limit = (int)(_allLibSymbols.Count * _promptComplexity).Clamp(1, _allLibSymbols.Count-1);
                var keep = _filter.SearchString;
                _filter.SearchString = "lib.";
                _filter.UpdateIfNecessary(null, true, limit);
                _filter.SearchString = keep;
            }
            
            var relevantCount = _filter.MatchingSymbolUis.Count;
            
            if (_randomSeed == 0)
            {
                _randomSeed = (int)(ImGui.GetFrameCount() * 374761393U & 1023U);
            }
            
            var promptCount = _filter.SearchString.Count(c => c == '?');
            for (uint i = 0; i < promptCount; i++)
            {
                var f = MathUtils.Hash01((uint)((i + 42 * _randomSeed * 668265263U) & 0x7fffffff ));
                var randomIndex = (int)(f * relevantCount).Clamp(0, relevantCount-1);
                SymbolTreeMenu.DrawSymbolItem(_filter.MatchingSymbolUis[randomIndex].Symbol);
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
                        if(!MoveSymbolToNamespace(guid, subtree.GetAsString(), out var reason))
                            EditorUi.Instance.ShowMessageBox(reason, "Could not move symbol's namespace");
                    }
                }

                ImGui.EndDragDropTarget();
            }

            return;

            static bool MoveSymbolToNamespace(Guid symbolId, string nameSpace, out string reason)
            {
                if (!SymbolUiRegistry.TryGetValue(symbolId, out var symbolUi))
                {
                    reason = $"Could not find symbol with id '{symbolId}'";
                    return false;
                }
                
                if (symbolUi!.Symbol.Namespace == nameSpace)
                {
                    reason = string.Empty;
                    return true;
                }
                
                if (symbolUi.Symbol.SymbolPackage.IsReadOnly)
                {
                    reason = $"Could not move symbol [{symbolUi.Symbol.Name}] because its package is not modifiable";
                    return false;
                }
                
                return EditableSymbolProject.ChangeSymbolNamespace(symbolUi.Symbol, nameSpace, out reason);
            }
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