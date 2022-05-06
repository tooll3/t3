using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Styling;

namespace T3.Gui.Windows.Variations
{
    public class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            _presetCanvas = new PresetCanvas();
            _variationCanvas = new VariationCanvas();
            Config.Title = "Variations";
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        private ViewModes _viewMode = 0;

        private void DrawWindowContent()
        {
            // Delete actions need be deferred to prevent collection modification during iteration
            if (_variationsToBeDeletedNextFrame.Count > 0)
            {
                _poolWithVariationToBeDeleted.DeleteVariations(_variationsToBeDeletedNextFrame);
                _variationsToBeDeletedNextFrame.Clear();
            }

            var drawList = ImGui.GetWindowDrawList();
            var keepCursorPos = ImGui.GetCursorScreenPos();

            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);
            {
                ImGui.BeginChild("header", new Vector2(ImGui.GetContentRegionAvail().X, 20));

                var viewModeIndex = (int)_viewMode;
                if (DrawSegmentedToggle(ref viewModeIndex, _options))
                {
                    _viewMode = (ViewModes)viewModeIndex;
                }

                ImGui.SameLine();

                if (CustomComponents.IconButton(Icon.Plus, "##addbutton", new Vector2(20, 20)))
                {
                    if (_viewMode == ViewModes.Presets)
                    {
                        var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetForInstanceSymbol(VariationHandling.ActiveInstanceForPresets);
                        if (newVariation != null)
                        {
                            newVariation.PosOnCanvas = _presetCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForPresets.Variations);
                            VariationThumbnail.VariationForRenaming = newVariation;
                            _presetCanvas.Selection.SetSelection(newVariation);
                            _presetCanvas.ResetView();
                            _presetCanvas.TriggerThumbnailUpdate();
                        }
                    }
                    else if(_viewMode == ViewModes.Variations)
                    {
                        var selectedInstances = NodeSelection.GetSelectedInstances().ToList();
                        var newVariation = VariationHandling.ActivePoolForVariations.CreateVariationForCompositionInstances(selectedInstances);
                        if (newVariation != null)
                        {
                            newVariation.PosOnCanvas = _variationCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForVariations.Variations);
                            VariationThumbnail.VariationForRenaming = newVariation;
                            _variationCanvas.Selection.SetSelection(newVariation);
                            _variationCanvas.ResetView();
                            _variationCanvas.TriggerThumbnailUpdate();
                        }
                    }
                }

                ImGui.EndChild();
            }

            drawList.ChannelsSetCurrent(0);
            {
                ImGui.SetCursorScreenPos(keepCursorPos);

                if (_viewMode == ViewModes.Presets)
                {
                    if (VariationHandling.ActivePoolForPresets == null 
                        || VariationHandling.ActiveInstanceForPresets == null 
                        || VariationHandling.ActivePoolForPresets.Variations.Count == 0)
                    {
                        CustomComponents.EmptyWindowMessage("No presets yet");
                    }
                    else
                    {
                        _presetCanvas.Draw(drawList);
                    }
                }
                else
                {
                    if (VariationHandling.ActivePoolForVariations == null 
                        || VariationHandling.ActiveInstanceForVariations == null 
                        || VariationHandling.ActivePoolForVariations.Variations.Count == 0)
                    {
                        CustomComponents.EmptyWindowMessage("No Variations yet\nVariations save parameters for selected Operators\nin the current composition.");
                    }
                    else
                    {
                        _variationCanvas.Draw(drawList);
                    }
                }
            }

            drawList.ChannelsMerge();
        }

        private enum ViewModes
        {
            Presets,
            Variations,
        }

        private static readonly List<string> _options = new() { "Presets", "Variations" };

        private static bool DrawSegmentedToggle(ref int currentIndex, List<string> options)
        {
            var changed = false;
            for (var index = 0; index < options.Count; index++)
            {
                var isActive = currentIndex == index;
                var option = options[index];

                ImGui.SameLine(0);
                ImGui.PushFont(isActive ? Fonts.FontBold : Fonts.FontNormal);
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.White.Fade(0.1f).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActive ? Color.White : Color.White.Fade(0.5f).Rgba);

                if (ImGui.Button(option))
                {
                    if (!isActive)
                    {
                        currentIndex = index;
                        changed = true;
                    }
                }

                ImGui.PopFont();
                ImGui.PopStyleColor(3);
            }

            return changed;
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public static void DeleteVariationsFromPool(SymbolVariationPool pool, IEnumerable<Variation> selectionSelection)
        {
            _poolWithVariationToBeDeleted = pool;
            _variationsToBeDeletedNextFrame.AddRange(selectionSelection);
            pool.StopHover();
        }

        private static readonly List<Variation> _variationsToBeDeletedNextFrame = new(20);
        private static SymbolVariationPool _poolWithVariationToBeDeleted;
        private readonly PresetCanvas _presetCanvas;
        private readonly VariationCanvas _variationCanvas;
    }
}