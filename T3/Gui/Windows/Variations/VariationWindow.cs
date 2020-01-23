using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
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

        internal readonly List<Variation.VariationParameter> VariationParameters = new List<Variation.VariationParameter>();



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
                            var matchingParam = VariationParameters.FirstOrDefault(variationParam => input == variationParam.Input && symbolChildUi.Id == variationParam.SymbolChildUi.Id);
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
                                                                 SymbolChildUi =  symbolChildUi,
                                                                 Input = input,
                                                                 //Instance = SelectionManager.GetInstanceForSymbolChildUi(symbolChildUi),
                                                                 Type = p.ValueType,
                                                                 InputSlot = inputSlot,
                                                                 OriginalValue = inputSlot.Input.Value.Clone(),
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

        public IOutputUi OutputUi;

        private readonly VariationCanvas _variationCanvas;
        private static readonly Vector2 Spacing = new Vector2(1, 5);
    }
}