using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.Variations;

internal class SnapshotCanvas : VariationBaseCanvas
{
    private protected override Instance InstanceForBlendOperations => VariationHandling.ActiveInstanceForSnapshots;
    private protected override SymbolVariationPool PoolForBlendOperations => VariationHandling.ActivePoolForSnapshots;

    public virtual void DrawToolbarFunctions()
    {
        var s = ImGui.GetFrameHeight();

        if (CustomComponents.IconButton(Icon.Plus, new Vector2(s, s)))
        {
            CreateVariation();
        }
    }

    protected override string GetTitle()
    {
        return VariationHandling.ActiveInstanceForSnapshots != null 
                   ? $"...for {VariationHandling.ActiveInstanceForSnapshots.Symbol.Name}" 
                   : string.Empty;
    }

    protected override void DrawAdditionalContextMenuContent(Instance InstanceForBlendOperations)
    {
        var oneSelected = CanvasElementSelection.SelectedElements.Count == 1;
        var oneOrMoreSelected = CanvasElementSelection.SelectedElements.Count > 0;

        var graphWindow = GraphWindow.Focused;
        if (graphWindow == null)
            return;

        var nodeSelection = graphWindow.Components.NodeSelection;

        if (ImGui.MenuItem("Select affected Operators",
                           "",
                           false,
                           oneOrMoreSelected))
        {
            nodeSelection.Clear();

            foreach (var element in CanvasElementSelection.SelectedElements)
            {
                if (element is not Variation selectedVariation)
                    continue;

                var parentSymbolUi = InstanceForBlendOperations.Symbol.GetSymbolUi();

                foreach (var symbolChildUi in parentSymbolUi.ChildUis.Values)
                {
                    if (!selectedVariation.ParameterSetsForChildIds.ContainsKey(symbolChildUi.Id))
                        continue;

                    if (InstanceForBlendOperations.Children.TryGetValue(symbolChildUi.Id, out var instance))
                        nodeSelection.AddSelection(symbolChildUi, instance);
                }
            }

            FitViewToSelectionHandling.FitViewToSelection();
        }

        if (ImGui.MenuItem("Remove selected Ops from Variations",
                           "",
                           false,
                           oneOrMoreSelected))
        {
            var selectedInstances = nodeSelection.GetSelectedInstances().ToList();
            var selectedThumbnails = new List<Variation>();
            foreach (var thumbnail in CanvasElementSelection.SelectedElements)
            {
                if (thumbnail is Variation v)
                {
                    selectedThumbnails.Add(v);
                }
            }

            VariationHandling.RemoveInstancesFromVariations(selectedInstances.Select(i=> i.SymbolChildId), selectedThumbnails);
        }
    }

    private void CreateVariation()
    {
        var newVariation = VariationHandling.CreateOrUpdateSnapshotVariation();
        if (newVariation == null)
            return;

        PoolForBlendOperations.SaveVariationsToFile();
        CanvasElementSelection.SetSelection(newVariation);
        ResetView();
        TriggerThumbnailUpdate();
        VariationThumbnail.VariationForRenaming = newVariation;
    }
}