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
                        ImGui.PushStyleColor(ImGuiCol.Text, isMatching ? Color.Gray.Rgba : _nonMatchingVarationsColor);
                        ImGui.PushID(variation.GetHashCode());
                        {
                            if (CustomComponents.IconButton(Icon.Pin, "selection", new Vector2(16, 16)))
                            {
                                
                            }
                            ImGui.SameLine();
                            
                            if (ImGui.Selectable(variation.Title, false,0, new Vector2(ImGui.GetWindowWidth() -32,0)))
                            {
                                variation.ApplyPermanently();
                                variation.UpdateUndoCommand();

                                // Select relevant operators
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

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public void SaveVariation(Variation variation)
        {
            SavedVariationIndex++;
            variation.Title = "Untitled " + SavedVariationIndex;
            if (_variationsForSymbols.TryGetValue(_compositionSymbolId, out var list))
            {
                list.Add(variation);
            }
            else
            {
                _variationsForSymbols[_compositionSymbolId] = new List<Variation> { variation };
            }
        }

        public IOutputUi OutputUi;
        private readonly Dictionary<Guid, List<Variation>> _variationsForSymbols = new Dictionary<Guid, List<Variation>>();

        private Variation _lastHoveredVariation;
        private readonly VariationCanvas _variationCanvas;
        private static readonly Vector2 Spacing = new Vector2(1, 5);
        private static readonly Color _nonMatchingVarationsColor = new Color(0.3f);
        private static int SavedVariationIndex = 1;
        internal readonly List<Variation.VariationParameter> VariationParameters = new List<Variation.VariationParameter>();
    }
}