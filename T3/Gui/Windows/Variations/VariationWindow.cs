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

        protected override void DrawContent()
        {
            ImGui.BeginChild("#params", new Vector2(200, -1));
            {
                ImGui.Button("Smoother");
                ImGui.SameLine();

                ImGui.Button("Rougher");
                ImGui.SameLine();

                ImGui.Button("1:1");

                ImGui.DragFloat("Scatter", ref _variationCanvas.Scatter, 0.01f, 0, 3);

                _compositionSymbolId = SelectionManager.GetSelectedInstance()?.Parent.SymbolChildId ?? Guid.Empty;

                foreach (var symbolChildUi in SelectionManager.GetSelectedSymbolChildUis())
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
                                                                //OriginalValue = inputSlot.Input.Value.Clone(),
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
                ImGui.Text("Favs");
                ImGui.PopFont();

                if (_compositionSymbolId != Guid.Empty && _variationsForSymbols.TryGetValue(_compositionSymbolId, out var favorites))
                {
                    foreach (var fav in favorites)
                    {
                        ImGui.PushID(fav.GridCell.GridIndex);
                        if (ImGui.Selectable("fav"))
                        {
                            fav.ApplyPermanently();
                            _hoveredVariation = null;
                        }
                        
                        if (ImGui.IsItemHovered())
                        {
                            if (_hoveredVariation == null)
                            {
                                fav.ApplyValues();
                                _hoveredVariation = fav;
                            }
                            else if (_hoveredVariation != fav)
                            {
                                _hoveredVariation.RestoreValues();
                                fav.ApplyValues();
                                _hoveredVariation = fav;
                            }
                            
                            foreach (var param in _hoveredVariation.ValuesForParameters.Keys)
                            {
                                T3Ui.AddHoveredId(param.SymbolChildUi.Id);                                
                            }
                        }
                        else
                        {
                            var noLongerHovered = _hoveredVariation == fav;
                            if (noLongerHovered)
                            {
                                _hoveredVariation.RestoreValues();
                                _hoveredVariation = null;
                            }
                        }

                        ImGui.PopID();
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
        
        private Variation _hoveredVariation;
        private readonly VariationCanvas _variationCanvas;
        private static readonly Vector2 Spacing = new Vector2(1, 5);
        internal readonly List<Variation.VariationParameter> VariationParameters = new List<Variation.VariationParameter>();
    }
}