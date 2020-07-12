using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Gui.Graph.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Gui.Windows.Variations
{
    /// <summary>
    /// Renders the <see cref="VariationWindow"/>
    /// </summary>
    public class VariationWindow : Window
    {
        public VariationWindow()
        {
            _variationCanvas = new VariationCanvas(this);
            Config.Title = "Presets";
            Config.Visible = true;
        }

        private Guid _compositionSymbolId;

        private bool CheckFavoriteMatchesNodeSelection(Variation variation)
        {
            var match = true;
            foreach (var param in variation.ValuesForParameters.Keys)
            {
                if (!SelectionManager.GetSelectedSymbolChildUis().Contains(param.SymbolChildUi))
                {
                    match = false;
                }
            }

            return match;
        }

        protected override void DrawContent()
        {
            ImGui.BeginChild("#params", new Vector2(200, -1));
            {
                ImGui.DragFloat("Scatter", ref _variationCanvas.Scatter, 0.01f, 0, 3);
                _compositionSymbolId = SelectionManager.GetCompositionForSelection()?.SymbolChildId ?? Guid.Empty;

                var selectedSymbolChildUis = SelectionManager.GetSelectedSymbolChildUis();

                // Remove no longer selected parameters
                var symbolChildUis = selectedSymbolChildUis as SymbolChildUi[] ?? selectedSymbolChildUis.ToArray();
                for (var index = VariationParameters.Count - 1; index >= 0; index--)
                {
                    if (!symbolChildUis.Contains(VariationParameters[index].SymbolChildUi))
                    {
                        VariationParameters.RemoveAt(index);
                    }
                }

                foreach (var symbolChildUi in symbolChildUis)
                {
                    ImGui.PushFont(Fonts.FontBold);
                    ImGui.Selectable(symbolChildUi.SymbolChild.ReadableName);
                    ImGui.PopFont();
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                    ImGui.PushID(symbolChildUi.Id.GetHashCode());

                    foreach (var input in symbolChildUi.SymbolChild.InputValues.Values)
                    {
                        var p = input.DefaultValue;

                        // TODO: check if input is connected
                        if (p.ValueType == typeof(float)
                            || p.ValueType == typeof(Vector2)
                            || p.ValueType == typeof(Vector3)
                            || p.ValueType == typeof(Vector4)
                            )
                        {
                            var matchingParam =
                                VariationParameters.FirstOrDefault(variationParam =>
                                                                       input == variationParam.Input && symbolChildUi.Id == variationParam.SymbolChildUi.Id);
                            var selected = matchingParam != null;

                            if (ImGui.Selectable(input.Name, selected))
                            {
                                if (selected)
                                {
                                    VariationParameters.Remove(matchingParam);
                                }
                                else
                                {
                                    var instance = SelectionManager.GetInstanceForSymbolChildUi(symbolChildUi);
                                    var inputSlot = instance.Inputs.Single(input2 => input2.Id == input.InputDefinition.Id);

                                    VariationParameters.Add(new Variation.VariationParameter()
                                                                {
                                                                    SymbolChildUi = symbolChildUi,
                                                                    Input = input,
                                                                    InstanceIdPath = NodeOperations.BuildIdPathForInstance(instance),
                                                                    Type = p.ValueType,
                                                                    InputSlot = inputSlot,
                                                                    Strength = 1,
                                                                });
                                }

                                _variationCanvas.ClearVariations();
                            }
                        }
                    }

                    ImGui.PopID();

                    ImGui.Dummy(Spacing);
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();
                ImGui.PushFont(Fonts.FontBold);
                ImGui.Text("Saved");
                ImGui.PopFont();

                if (_compositionSymbolId != Guid.Empty && _variationsForSymbols.TryGetValue(_compositionSymbolId, out var savedForComposition))
                {
                    Variation deleteThis = null;
                    foreach (var variation in savedForComposition)
                    {
                        var isMatching = CheckFavoriteMatchesNodeSelection(variation);
                        ImGui.PushStyleColor(ImGuiCol.Text, isMatching ? Color.Gray.Rgba : NonMatchingVarationsColor);
                        ImGui.PushID(variation.GetHashCode());
                        {
                            var isSelected = _blendedVariations.Contains(variation);
                            if (CustomComponents.IconButton(isSelected ? Icon.ChevronRight : Icon.Pin, "selection", new Vector2(16, 16)))
                            {
                                if (isSelected)
                                {
                                    _blendedVariations.Remove(variation);
                                }
                                else
                                {
                                    _blendedVariations.Add(variation);
                                }

                                LayoutBlendedVariations();
                            }

                            ImGui.SameLine();

                            if (ImGui.Selectable(variation.Title, false, 0, new Vector2(ImGui.GetWindowWidth() - 32, 0)))
                            {
                                SelectVariation(variation);
                            }

                            if (ImGui.IsItemHovered())
                            {
                                if (_lastHoveredVariation == null)
                                {
                                    variation.KeepCurrentAndApplyNewValues();
                                    _lastHoveredVariation = variation;
                                }
                                else if (_lastHoveredVariation != variation)
                                {
                                    _lastHoveredVariation.RestoreValues();
                                    variation.KeepCurrentAndApplyNewValues();
                                    _lastHoveredVariation = variation;
                                }

                                // Hover relevant operators
                                foreach (var param in _lastHoveredVariation.ValuesForParameters.Keys)
                                {
                                    T3Ui.AddHoveredId(param.SymbolChildUi.Id);
                                }
                            }
                            else
                            {
                                var wasHoveredBefore = _lastHoveredVariation == variation;
                                if (wasHoveredBefore)
                                {
                                    _lastHoveredVariation.RestoreValues();
                                    _lastHoveredVariation = null;
                                }
                            }

                            ImGui.SameLine();

                            // Delete button
                            if (CustomComponents.IconButton(Icon.Loop, "selection", new Vector2(16, 16)))
                            {
                                deleteThis = variation;
                            }
                        }

                        ImGui.PopStyleColor();
                        ImGui.PopID();
                    }

                    if (deleteThis != null)
                    {
                        savedForComposition.Remove(deleteThis);
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("canvas", new Vector2(-1, -1));
            {
                _variationCanvas.Draw();
            }
            ImGui.EndChild();
        }

        private void LayoutBlendedVariations()
        {
            _variationCanvas.ClearVariations();
            _variationCanvas.ResetView();

            // Merge parameter list
            var parameters = new List<Variation.VariationParameter>();
            foreach (var variation in _blendedVariations)
            {
                foreach (var param in variation.ValuesForParameters.Keys)
                {
                    if (!parameters.Contains(param))
                        parameters.Add(param);
                }
            }

            const int steps = 7;
            if (_blendedVariations.Count == 2)
            {
                for (int stepY = 0; stepY <= steps; stepY++)
                {
                    var ty = (float)stepY / steps;
                    var inputVariationsAndWeights = new List<Tuple<Variation, float>>()
                                                        {
                                                            new Tuple<Variation, float>(_blendedVariations[0], 1 - ty),
                                                            new Tuple<Variation, float>(_blendedVariations[1], ty),
                                                        };
                    var newVariation = Variation.Mix(parameters, inputVariationsAndWeights, 0, GridCell.Center + new GridCell(0, stepY - steps / 2));
                    _variationCanvas.AddVariationToGrid(newVariation);
                }
            }
            else if (_blendedVariations.Count == 3)
            {
                for (int stepY = 0; stepY <= steps; stepY++)
                {
                    for (int stepX = 0; stepX <= steps; stepX++)
                    {
                        var tx = (float)stepX / steps;
                        var ty = (float)stepY / steps;
                        var inputVariationsAndWeights = new List<Tuple<Variation, float>>()
                                                            {
                                                                new Tuple<Variation, float>(_blendedVariations[0], (1 - tx) * (1 - ty)),
                                                                new Tuple<Variation, float>(_blendedVariations[1], (1 - tx) * (ty)),
                                                                new Tuple<Variation, float>(_blendedVariations[2], (tx) * (ty)),
                                                            };

                        var gridCell = GridCell.Center + new GridCell(stepX - steps / 2, stepY - steps / 2);
                        var newVariation = Variation.Mix(parameters, inputVariationsAndWeights, 0, gridCell);
                        _variationCanvas.AddVariationToGrid(newVariation);
                    }
                }
            }
        }

        private void SelectVariation(Variation variation)
        {
            variation.ApplyPermanently();
            variation.UpdateUndoCommand();

            // Select variation
            SelectionManager.Clear();
            VariationParameters.Clear();

            var alreadyAdded = new HashSet<SymbolChildUi>();
            foreach (var param in variation.ValuesForParameters.Keys.Distinct())
            {
                VariationParameters.Add(param);
                if (!alreadyAdded.Contains(param.SymbolChildUi))
                {
                    SelectionManager.AddSelection(param.SymbolChildUi, NodeOperations.GetInstanceFromIdPath(param.InstanceIdPath));
                    alreadyAdded.Add(param.SymbolChildUi);
                }
            }

            _variationCanvas.ClearVariations();
        }

        private List<Variation> _blendedVariations = new List<Variation>();

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public void SaveVariation(Variation variation)
        {
            _savedVariationIndex++;
            variation.Title = GetRandomTitle();
            if (_variationsForSymbols.TryGetValue(_compositionSymbolId, out var list))
            {
                list.Add(variation);
            }
            else
            {
                _variationsForSymbols[_compositionSymbolId] = new List<Variation> { variation };
            }
        }

        private string GetRandomTitle()
        {
            return RandomNames[_random.Next(RandomNames.Length)] + " " + RandomNames[_random.Next(RandomNames.Length)];
        }

        private static Random _random = new Random();
        private static string[] RandomNames =
            {
                "Ace", "Age", "Ego", "Aid", "Aim", "Air", "Ape", "Barf", "Ass", "Axe", "Bad", "Big", "Boa", "Bro", "Bug", "Bum", "Cat", "Cow", "Cult", "Dog",
                "Duck", "Eel", "Egg", "Eye", "Funk", "Fix", "Fox", "Fun", "Gut", "Hack", "Freak", "Cyber", "Dope", "Hip", "Bit",
                "Mega", "Bomb", "Hot", "Jump", "Cult", "Ice", "Mad", "Mix", "Mud", "Off", "Ohm", "Oil", "One", "Two", "Four", "Five", "Six", "Pet", "Pig",
                "Poo", "Pop", "Pot", "Pub", "Raw", "Red", "Green", "Pink", "Black", "White", "Orange", "Fine", "Fog", "Sad", "Sea", "Sex", "Shy", "Sin", "Sir",
                "Ska", "Toy", "Tea", "Retro", "Monkey", "Top", "Wet", "Zoo"
            };

        public IOutputUi OutputUi { get; set; }

        private readonly Dictionary<Guid, List<Variation>> _variationsForSymbols = new Dictionary<Guid, List<Variation>>();
        private Variation _lastHoveredVariation;
        private readonly VariationCanvas _variationCanvas;
        private static readonly Vector2 Spacing = new Vector2(1, 5);
        private static readonly Color NonMatchingVarationsColor = new Color(0.3f);
        private static int _savedVariationIndex = 1;
        internal readonly List<Variation.VariationParameter> VariationParameters = new List<Variation.VariationParameter>();
    }
}