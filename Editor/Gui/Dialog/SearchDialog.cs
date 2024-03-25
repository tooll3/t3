using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.SystemUi;

namespace T3.Editor.Gui.Dialog
{
    public class SearchDialog : ModalDialog
    {
        public SearchDialog()
        {
            DialogSize = new Vector2(500, 300);
            Padding = 4;
        }

        public void Draw()
        {
            if (BeginDialog("Search"))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                
                if (!_isOpen)
                {
                    _justOpened = true;
                    _isOpen = true;
                }
                else
                {
                    _justOpened = false;
                }

                if (_justOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                }

                var needsUpdate = _justOpened;
                FormInputs.SetIndentToLeft();
                
                FormInputs.SetWidth(0.7f);
                needsUpdate |= FormInputs.AddStringInput("", ref _searchString, "Search", null, null, string.Empty);
                ImGui.SameLine();
                FormInputs.SetWidth(1f);
                needsUpdate |= FormInputs.AddEnumDropdown(ref _searchMode, "");
                FormInputs.ResetWidth();
                
                
                if (needsUpdate)
                {
                    UpdateResults();
                }


                var clickedOutside = ImGui.IsMouseClicked(ImGuiMouseButton.Left) 
                                     && !ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows| ImGuiHoveredFlags.AllowWhenBlockedByActiveItem );
                if (ImGui.IsKeyReleased(ImGuiKey.Enter) || ImGui.IsKeyReleased(ImGuiKey.Escape) || clickedOutside)
                {
                    ImGui.CloseCurrentPopup();
                }
                
                DrawResultsList();
                ImGui.PopStyleVar();
                EndDialogContent();
            }
            else
            {
                _isOpen = false;
            }

            EndDialog();
        }

        private bool _isOpen = false;

        private void DrawResultsList()
        {
            var size = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            var matchingInstances = _matchingInstances;

            if (ImGui.BeginChildFrame(999, size))
            {
                if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
                {
                    UiListHelpers.AdvanceSelectedItem(matchingInstances, ref _selectedInstance, 1);
                    _selectedItemChanged = true;
                    var index = matchingInstances.IndexOf(_selectedInstance);
                    if(index == 0)
                        ImGui.SetScrollY(0);
                }
                else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
                {
                    UiListHelpers.AdvanceSelectedItem(matchingInstances, ref _selectedInstance, -1);
                    _selectedItemChanged = true;
                    
                    var index = matchingInstances.IndexOf(_selectedInstance);

                    if (index * ImGui.GetTextLineHeight() > ImGui.GetScrollY() + ImGui.GetContentRegionAvail().Y)
                    {
                        Log.Debug("Would scroll down");
                        ImGui.SetScrollY(ImGui.GetScrollY() + ImGui.GetContentRegionAvail().Y);
                    }
                }

                unsafe
                {
                    var clipperData = new ImGuiListClipper();
                    var listClipperPtr = new ImGuiListClipperPtr(&clipperData);

                    listClipperPtr.Begin(matchingInstances.Count, ImGui.GetTextLineHeight());
                    while (listClipperPtr.Step())
                    {
                        for (var i = listClipperPtr.DisplayStart; i < listClipperPtr.DisplayEnd; ++i)
                        {
                            if (i < 0 || i >= matchingInstances.Count)
                                continue;

                            DrawItem(matchingInstances[i]);
                        }
                    }
                    
                    listClipperPtr.End();
                }
            }

            ImGui.EndChildFrame();

            ImGui.PopStyleVar(2);
        }

        private void DrawItem(FoundInstance foundInstance)
        {
            var instance = foundInstance.Instance;
            var canvas = foundInstance.GraphCanvas;
            var symbolHash = instance.Symbol.Id.GetHashCode();
            ImGui.PushID(symbolHash);
            {
                var symbolNamespace = instance.Symbol.Namespace;
                var isRelevantNamespace = symbolNamespace.StartsWith("lib.")
                                          || symbolNamespace.StartsWith("examples.lib.");

                var color = instance.Symbol.OutputDefinitions.Count > 0
                                ? TypeUiRegistry.GetPropertiesForType(instance.Symbol.OutputDefinitions[0]?.ValueType).Color
                                : UiColors.Gray;

                if (!isRelevantNamespace)
                {
                    color = color.Fade(0.4f);
                }

                ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.OperatorBackground.Apply(color).Rgba);

                var hoverColor = ColorVariations.OperatorBackgroundHover.Apply(color).Rgba;
                hoverColor.W = 0.1f;
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
                //ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 4));

                var isSelected = foundInstance == _selectedInstance;
                var hasBeenClicked = ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                _selectedItemChanged |= hasBeenClicked; 

                var path = OperatorUtils.BuildIdPathForInstance(instance);
                var readablePath = string.Join(" / ", canvas.Structure.GetReadableInstancePath(path, false));
                
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    canvas.OpenAndFocusInstance(path);
                    _selectedInstance = foundInstance;
                    _selectedItemChanged = false;
                }
                else if (_selectedItemChanged && _selectedInstance == foundInstance)
                {
                    UiListHelpers.ScrollToMakeItemVisible();
                    canvas.OpenAndFocusInstance(path);
                    _selectedItemChanged = false;
                }


                ImGui.SameLine();

                ImGui.TextUnformatted(instance.Symbol.Name);
                ImGui.SameLine(0, 10);

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Gray.Fade(0.5f).Rgba);
                ImGui.TextUnformatted(readablePath);
                ImGui.PopStyleColor();
                
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
            }
            ImGui.PopID();
        }

        private void FindAllChildren(Instance instance, Action<Instance> callback)
        {
            foreach (var child in instance.Children.Values)
            {
                callback(child);
                if (_searchMode == SearchModes.Local)
                    continue;
                FindAllChildren(child, callback);
            }
        }

        private void UpdateResults()
        {
            _matchingInstances.Clear();

            if (GraphWindow.GraphWindowInstances.Count == 0)
                return;
            
            
            foreach (var graphWindow in GraphWindow.GraphWindowInstances)
            {
                var canvas = graphWindow.GraphCanvas;
                var package = graphWindow.Package;

                if (string.IsNullOrEmpty(_searchString))
                {
                    var previousInstances = canvas.NavigationHistory.GetPreviouslySelectedInstances()
                                                  .Select(instance => new FoundInstance(instance, graphWindow.GraphCanvas));
                    _matchingInstances.AddRange(previousInstances);

                    if (_matchingInstances.Count > 0)
                        _selectedInstance = _matchingInstances[0];

                    continue;
                }

                var compositionOp = graphWindow.CompositionOp;

                var composition = _searchMode switch
                                      {
                                          SearchModes.Global             => graphWindow.RootInstance.Instance,
                                          SearchModes.Local              => compositionOp,
                                          SearchModes.LocalAndInChildren => compositionOp,
                                          _                              => throw new ArgumentOutOfRangeException()
                                      };

                if (composition == null)
                    continue;

                FindAllChildren(composition,
                                instance =>
                                {
                                    if (string.IsNullOrEmpty(_searchString)
                                        || instance.Symbol.Name.Contains(_searchString, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        _matchingInstances.Add(new FoundInstance(instance, canvas));
                                    }
                                });

                _matchingInstances.Sort((foundA, foundB) =>
                                        {
                                            var a = foundA.Instance;
                                            var b = foundB.Instance;
                                            return string.Compare(a.Symbol.Name, b.Symbol.Name, StringComparison.Ordinal);
                                        });
            }
        }

        private readonly List<FoundInstance> _matchingInstances = new();
        private bool _justOpened;
        private static string _searchString;
        private FoundInstance _selectedInstance;
        private bool _selectedItemChanged;
        private SearchModes _searchMode = SearchModes.Local;

        private readonly record struct FoundInstance(Instance Instance, GraphCanvas GraphCanvas);

        private enum SearchModes
        {
            Local,
            LocalAndInChildren,
            Global,
        }
    }
}