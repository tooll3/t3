using System.Diagnostics;
using ImGuiNET;
using T3.Core.SystemUi;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.Variations
{
    internal class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            _presetCanvas = new PresetCanvas();
            _snapshotCanvas = new SnapshotCanvas();
            Config.Title = "Variations";
            MenuTitle = "Presets";
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        private ViewModes _viewMode = 0;
        private int _selectedNodeCount = 0;

        public void DrawWindowContent(bool hideHeader = false)
        {
            // Delete actions need be deferred to prevent collection modification during iteration
            if (_variationsToBeDeletedNextFrame.Count > 0)
            {
                _poolWithVariationToBeDeleted.DeleteVariations(_variationsToBeDeletedNextFrame);
                _variationsToBeDeletedNextFrame.Clear();
            }

            var graphWindow = GraphWindow.Focused;
            if (graphWindow == null)
                return;
            
            var nodeSelection = graphWindow.GraphCanvas.NodeSelection;

            var compositionHasVariations = VariationHandling.ActivePoolForSnapshots != null && VariationHandling.ActivePoolForSnapshots.AllVariations.Count > 0;
            var oneChildSelected = nodeSelection.Selection.Count == 1;
            var selectionChanged = nodeSelection.Selection.Count != _selectedNodeCount;

            if (selectionChanged)
            {
                _selectedNodeCount = nodeSelection.Selection.Count;

                if (oneChildSelected)
                {
                    _viewMode = ViewModes.Presets;
                }
                else if (compositionHasVariations && _selectedNodeCount == 0)
                {
                    _viewMode = ViewModes.Snapshots;
                }
            }

            var drawList = ImGui.GetWindowDrawList();
            var keepCursorPos = ImGui.GetCursorScreenPos();

            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);
            {
                if (!hideHeader)
                {
                    ImGui.BeginChild("header",
                                     new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
                                     false,
                                     ImGuiWindowFlags.NoScrollbar);

                    var viewModeIndex = (int)_viewMode;

                    if (CustomComponents.DrawSegmentedToggle(ref viewModeIndex, _options))
                    {
                        _viewMode = (ViewModes)viewModeIndex;
                        _presetCanvas.RefreshView();
                        _snapshotCanvas.RefreshView();
                    }

                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(10, 10));
                    ImGui.SameLine();
                    switch (_viewMode)
                    {
                        case ViewModes.Presets:
                            _presetCanvas.DrawToolbarFunctions();
                            break;

                        case ViewModes.Snapshots:
                            _snapshotCanvas.DrawToolbarFunctions();
                            break;
                    }

                    ImGui.EndChild();
                }
            }

            drawList.ChannelsSetCurrent(0);
            {
                ImGui.SetCursorScreenPos(keepCursorPos);

                if (_viewMode == ViewModes.Presets)
                {
                    if (VariationHandling.ActivePoolForPresets == null
                        || VariationHandling.ActiveInstanceForPresets == null
                        || VariationHandling.ActivePoolForPresets.AllVariations.Count == 0)
                    {
                        CustomComponents.EmptyWindowMessage("No presets yet.");
                    }
                    else
                    {
                        _presetCanvas.Draw(drawList, hideHeader);
                    }
                }
                else
                {
                    if (VariationHandling.ActivePoolForSnapshots == null
                        || VariationHandling.ActiveInstanceForSnapshots == null
                        || VariationHandling.ActivePoolForSnapshots.AllVariations.Count == 0)
                    {
                        var childUi = VariationHandling.ActiveInstanceForPresets.GetSymbolUi();
                        var shapshotsEnabledForNone = !childUi.ChildUis.Any(s => s.SnapshotGroupIndex > 0);
                        var additionalHint = shapshotsEnabledForNone ? "Use the graph window context menu\nto activate snapshots for operators." : "";

                        if (CustomComponents
                           .EmptyWindowMessage("No Snapshots yet.\n\nWith shapshots you can switch or blend\nbetween parameter sets in your composition.\n\n"
                                               + additionalHint, "Learn More"))
                        {
                            const string url = "https://github.com/tooll3/t3/wiki/PresetsAndSnapshots";
                            CoreUi.Instance.OpenWithDefaultApplication(url);
                        }
                    }
                    else
                    {
                        _snapshotCanvas.Draw(drawList);
                    }
                }
            }

            drawList.ChannelsMerge();
        }

        private enum ViewModes
        {
            Presets,
            Snapshots,
        }

        private static readonly List<string> _options = new() { "Presets", "Snapshots" };

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public static void DeleteVariationsFromPool(SymbolVariationPool pool, IEnumerable<Variation> selectionSelection)
        {
            _poolWithVariationToBeDeleted = pool;
            _variationsToBeDeletedNextFrame.AddRange(selectionSelection); // TODO: mixing Snapshots and variations in same list is dangerous
            pool.StopHover();
            pool.SaveVariationsToFile();
        }

        private static readonly List<Variation> _variationsToBeDeletedNextFrame = new(20);
        private static SymbolVariationPool _poolWithVariationToBeDeleted;
        private readonly PresetCanvas _presetCanvas;
        private readonly SnapshotCanvas _snapshotCanvas;
    }
}