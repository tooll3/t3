using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Selection;
using T3.Gui.Styling;

namespace T3.Gui.Windows.Variations
{
    public class SnapshotCanvas : VariationBaseCanvas
    {
        protected override Instance InstanceForBlendOperations => VariationHandling.ActiveInstanceForSnapshots;
        protected override SymbolVariationPool PoolForBlendOperations => VariationHandling.ActivePoolForSnapshots;

        public override void DrawToolbarFunctions()
        {
            var s = ImGui.GetFrameHeight();

            if (CustomComponents.IconButton(Icon.Plus, "##addbutton", new Vector2(s, s)))
            {
                CreateVariation();
            }

            var filteredOpCount = 0;

            var compositionId = VariationHandling.ActiveInstanceForSnapshots.Symbol.Id;
            if (VariationHandling.FocusSetsForCompositions.TryGetValue(compositionId, out var filterSet))
            {
                filteredOpCount = filterSet.Count;
            }

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(20, 20));

            ImGui.SameLine();
            if (filteredOpCount == 0)
            {
                if (ImGui.Button("Set focus"))
                {
                    var set = new HashSet<Guid>();
                    foreach (var selectedOp in NodeSelection.GetSelectedInstances())
                    {
                        set.Add(selectedOp.SymbolChildId);
                    }

                    VariationHandling.FocusSetsForCompositions[compositionId] = set;

                    var childrenWhenSettingFocus = new HashSet<Guid>();
                    foreach (var child in InstanceForBlendOperations.Children)
                    {
                        childrenWhenSettingFocus.Add(child.SymbolChildId);
                    }

                    VariationHandling.ChildIdsWhenFocusedForCompositions[compositionId] = childrenWhenSettingFocus;
                }

                CustomComponents.TooltipForLastItem("This will limit the parameters stored in new snapshots to the Operators selected when setting the focus.");
            }
            else
            {
                if (ImGui.Button($"Clear focus ({filteredOpCount})"))
                {
                    // Select focused ops
                    var parentSymbolUi = SymbolUiRegistry.Entries[InstanceForBlendOperations.Symbol.Id];

                    if (filterSet != null)
                    {
                        NodeSelection.Clear();
                        foreach (var symbolChildUi in parentSymbolUi.ChildUis)
                        {
                            if (!filterSet.Contains(symbolChildUi.Id))
                                continue;

                            var instance = InstanceForBlendOperations.Children.FirstOrDefault(c => c.SymbolChildId == symbolChildUi.Id);
                            if (instance != null)
                                NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                        }
                    }
                    FitViewToSelectionHandling.FitViewToSelection();

                    VariationHandling.FocusSetsForCompositions.Remove(compositionId);
                    VariationHandling.ChildIdsWhenFocusedForCompositions.Remove(compositionId);
                }

                if (ImGui.IsItemHovered())
                {
                    if (filterSet != null)
                    {
                        foreach (var id in filterSet)
                        {
                            T3Ui.AddHoveredId(id);
                        }
                    }
                }
            }
        }

        public override string GetTitle()
        {
            if (VariationHandling.ActiveInstanceForSnapshots == null)
                return "";

            return $"...for {VariationHandling.ActiveInstanceForSnapshots.Symbol.Name}";
        }

        protected override void DrawAdditionalContextMenuContent()
        {
            var oneSelected = Selection.SelectedElements.Count == 1;
            var oneOrMoreSelected = Selection.SelectedElements.Count > 0;

            if (ImGui.MenuItem("Select affected Operators",
                               "",
                               false,
                               oneOrMoreSelected))
            {
                NodeSelection.Clear();

                foreach (var element in Selection.SelectedElements)
                {
                    if (element is not Variation selectedVariation)
                        continue;

                    var parentSymbolUi = SymbolUiRegistry.Entries[InstanceForBlendOperations.Symbol.Id];

                    foreach (var symbolChildUi in parentSymbolUi.ChildUis)
                    {
                        if (!selectedVariation.ParameterSetsForChildIds.ContainsKey(symbolChildUi.Id))
                            continue;

                        var instance = InstanceForBlendOperations.Children.FirstOrDefault(c => c.SymbolChildId == symbolChildUi.Id);
                        if (instance != null)
                            NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                    }
                }

                FitViewToSelectionHandling.FitViewToSelection();
            }

            if (ImGui.MenuItem("Remove selected Ops from Variations",
                               "",
                               false,
                               oneOrMoreSelected))
            {
                var selectedInstances = NodeSelection.GetSelectedInstances().ToList();
                var selectedThumbnails = new List<Variation>();
                foreach (var thumbnail in Selection.SelectedElements)
                {
                    if (thumbnail is Variation v)
                    {
                        selectedThumbnails.Add(v);
                    }
                }

                VariationHandling.RemoveInstancesFromVariations(selectedInstances, selectedThumbnails);
            }
        }

        public override Variation CreateVariation()
        {
            var newVariation = VariationHandling.CreateOrUpdateSnapshotVariation();
            if (newVariation == null)
                return new Variation();

            PoolForBlendOperations.SaveVariationsToFile();
            Selection.SetSelection(newVariation);
            ResetView();
            TriggerThumbnailUpdate();
            VariationThumbnail.VariationForRenaming = newVariation;
            return new Variation();
        }
    }
}