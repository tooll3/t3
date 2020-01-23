using System;
using System.Collections.Generic;
using T3.Core;
using T3.Core.Operator;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Gui.Windows.Variations
{
    public class Variation
    {
        public readonly GridPos GridPos;
        public bool ThumbnailNeedsUpdate;

        private Variation(GridPos pos)
        {
            GridPos = pos;
            ThumbnailNeedsUpdate = true;
        }

        public readonly Dictionary<VariationParameter, object> ValuesForParameters =
            new Dictionary<VariationParameter, object>();

        public void ApplyValues()
        {
            foreach (var (param, value) in ValuesForParameters)
            {
                switch (param.InputSlot)
                {
                    case InputSlot<float> floatInput:
                        floatInput.SetTypedInputValue((float)value);
                        break;
                    case InputSlot<Vector2> vec2Input:
                        vec2Input.SetTypedInputValue((Vector2)value);
                        break;
                    case InputSlot<Vector3> vec3Input:
                        vec3Input.SetTypedInputValue((Vector3)value);
                        break;
                    case InputSlot<Vector4> vec4Input:
                        vec4Input.SetTypedInputValue((Vector4)value);
                        break;
                }
            }
        }

        public void RestoreValues()
        {
            foreach (var param in ValuesForParameters.Keys)
            {
                param.Input.Value.Assign(param.OriginalValue);
                param.InputSlot.DirtyFlag.Invalidate();
            }
        }

        public static Variation Mix(IEnumerable<VariationParameter> variationParameters,
                                    IReadOnlyCollection<Tuple<Variation, float>> neighboursAndWeights, float scatter,
                                    GridPos pos = new GridPos())
        {
            // Collect neighbours
            var newVariation = new Variation(pos);
            var useDefault = (neighboursAndWeights.Count == 0);

            foreach (var param in variationParameters)
            {
                if (useDefault)
                {
                    if (param.OriginalValue is InputValue<float> value)
                    {
                        newVariation.ValuesForParameters.Add(param, value.Value);
                    }
                    else if (param.OriginalValue is InputValue<Vector2> vec2Value)
                    {
                        newVariation.ValuesForParameters.Add(param, vec2Value.Value);
                    }
                    else if (param.OriginalValue is InputValue<Vector3> vec3Value)
                    {
                        newVariation.ValuesForParameters.Add(param, vec3Value.Value);
                    }
                    else if (param.OriginalValue is InputValue<Vector4> vec4Value)
                    {
                        newVariation.ValuesForParameters.Add(param, vec4Value.Value);
                    }

                    continue;
                }

                if (param.Type == typeof(float))
                {
                    var value = 0f;
                    var sumWeight = 0f;
                    foreach (var neighbour in neighboursAndWeights)
                    {
                        value += (float)neighbour.Item1.ValuesForParameters[param] * neighbour.Item2;
                        sumWeight += neighbour.Item2;
                    }

                    value *= 1f / sumWeight + ((float)Random.NextDouble() - 0.5f) * scatter;
                    value += Random.NextFloat(-scatter, scatter);
                    newVariation.ValuesForParameters.Add(param, value);
                }

                if (param.Type == typeof(Vector2))
                {
                    var value = Vector2.Zero;
                    var sumWeight = 0f;
                    foreach (var neighbour in neighboursAndWeights)
                    {
                        value += (Vector2)neighbour.Item1.ValuesForParameters[param] * neighbour.Item2;
                        sumWeight += neighbour.Item2;
                    }

                    value *= 1f / sumWeight;
                    value += new Vector2(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    newVariation.ValuesForParameters.Add(param, value);
                }

                if (param.Type == typeof(Vector3))
                {
                    var value = Vector3.Zero;
                    var sumWeight = 0f;

                    foreach (var neighbour in neighboursAndWeights)
                    {
                        value += (Vector3)neighbour.Item1.ValuesForParameters[param] * neighbour.Item2;
                        sumWeight += neighbour.Item2;
                    }

                    value *= 1f / sumWeight;
                    value += new Vector3(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    newVariation.ValuesForParameters.Add(param, value);
                }

                if (param.Type == typeof(Vector4))
                {
                    var value = Vector4.Zero;
                    var sumWeight = 0f;
                    foreach (var neighbour in neighboursAndWeights)
                    {
                        value += (Vector4)neighbour.Item1.ValuesForParameters[param] * neighbour.Item2;
                        sumWeight += neighbour.Item2;
                    }

                    value *= 1f / sumWeight;
                    value += new Vector4(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    newVariation.ValuesForParameters.Add(param, value);
                }
            }

            return newVariation;
        }
        
        
        public class VariationParameter
        {
            public SymbolChildUi SymbolChildUi;
            public IInputSlot InputSlot { get; set; }
            public InputValue OriginalValue { get; set; }
            public SymbolChild.Input Input;
            public Type Type;
            public float Strength = 1;
        }
        
        private static readonly Random Random = new Random();
    }
}