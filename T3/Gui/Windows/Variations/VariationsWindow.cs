using System;
using System.Collections.Generic;
using System.Numerics;
using Core.Resource;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.OutputUi;
using T3.Gui.Styling;

namespace T3.Gui.Windows.Variations
{
    public class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            _variationCanvas = new VariationCanvas(this);
            Config.Title = "Variations";
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        private void DrawWindowContent()
        {
            if (VariationHandling.ActiveInstanceForPresets == null || VariationHandling.ActivePoolForPresets == null)
            {
                return;
            }

            if (VariationHandling.ActivePoolForPresets.Variations.Count == 0)
            {
                CustomComponents.EmptyWindowMessage("No presets yet");
            }

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
                ImGui.BeginChild("header", new Vector2(20, 20));
                if (CustomComponents.IconButton(Icon.Plus, "## addbutton", new Vector2(20, 20)))
                {
                    var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetOfInstanceSymbol(VariationHandling.ActiveInstanceForPresets);
                    if (newVariation != null)
                    {
                        newVariation.PosOnCanvas =VariationCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForPresets.Variations);                     
                        VariationThumbnail.VariationForRenaming = newVariation;
                        _variationCanvas.Selection.SetSelection(newVariation);
                        _variationCanvas.ResetView();
                        _variationCanvas.TriggerThumbnailUpdate();
                    }
                }

                ImGui.EndChild();
            }

            drawList.ChannelsSetCurrent(0);
            {
                ImGui.SetCursorScreenPos(keepCursorPos);

                if (VariationHandling.ActivePoolForPresets != null)
                {
                    _variationCanvas.Draw(drawList, VariationHandling.ActivePoolForPresets);
                }
                VariationCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForPresets.Variations);
            }

            drawList.ChannelsMerge();
        }

        private static MatchTypes DoesPresetVariationMatch(Variation variation, Instance instance)
        {
            var setCorrectly = true;
            var foundOneMatch = false;
            var foundUnknownNonDefaults = false;

            foreach (var (symbolChildId, values) in variation.InputValuesForChildIds)
            {
                if (symbolChildId != Guid.Empty)
                    continue;

                foreach (var input in instance.Inputs)
                {
                    var inputIsDefault = input.Input.IsDefault;
                    var variationIncludesInput = values.ContainsKey(input.Id);

                    if (!ValueUtils.CompareFunctions.ContainsKey(input.ValueType))
                        continue;

                    if (variationIncludesInput)
                    {
                        foundOneMatch = true;

                        if (inputIsDefault)
                        {
                            setCorrectly = false;
                        }
                        else
                        {
                            var inputValueMatches = ValueUtils.CompareFunctions[input.ValueType](values[input.Id], input.Input.Value);
                            setCorrectly &= inputValueMatches;
                        }
                    }
                    else
                    {
                        if (inputIsDefault)
                        {
                        }
                        else
                        {
                            foundUnknownNonDefaults = true;
                        }
                    }
                }
            }

            if (!foundOneMatch || !setCorrectly)
            {
                return MatchTypes.NoMatch;
            }

            return foundUnknownNonDefaults ? MatchTypes.PresetParamsMatch : MatchTypes.PresetAndDefaultParamsMatch;
        }

        private static readonly List<Variation> _variationsToBeDeletedNextFrame = new(20);
        private static SymbolVariationPool _poolWithVariationToBeDeleted;

        private readonly VariationCanvas _variationCanvas;
        public IOutputUi OutputUi;

        private enum MatchTypes
        {
            NoMatch,
            PresetParamsMatch,
            PresetAndDefaultParamsMatch,
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public void DeleteVariations(List<Variation> selectionSelection)
        {
            _poolWithVariationToBeDeleted = VariationHandling.ActivePoolForPresets;
            _variationsToBeDeletedNextFrame.AddRange(selectionSelection);
        }
    }
}