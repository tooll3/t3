using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Rendering;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.Windows.Output;
using UiHelpers;
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

        internal readonly List<VariationParameter> VariationParameters = new List<VariationParameter>();

        internal class VariationParameter
        {
            public SymbolChildUi SymbolChildUi;
            public IInputSlot InputSlot { get; set; }
            public InputValue OriginalValue { get; set; }
            public SymbolChild.Input Input;
            public Type Type;
            public Instance Instance;
            public float Strength = 1;
        }

        protected override void DrawContent()
        {
            ImGui.BeginChild("#params", new Vector2(200, -1));
            {
                ImGui.Button("Smoother");
                ImGui.SameLine();
                
                ImGui.Button("Rougher");
                ImGui.SameLine();
                
                ImGui.Button("1:1");
                
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
                                    
                                    VariationParameters.Add(new VariationParameter()
                                                             {
                                                                 SymbolChildUi =  symbolChildUi,
                                                                 Input = input,
                                                                 Instance = SelectionManager.GetInstanceForSymbolChildUi(symbolChildUi),
                                                                 Type = p.ValueType,
                                                                 InputSlot = inputSlot,
                                                                 OriginalValue = inputSlot.Input.Value.Clone(),
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