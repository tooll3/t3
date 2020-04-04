using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core;
using T3.Core.Operator;
using SharpDX;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Gui.Windows.Variations
{
    public class Variation
    {
        public GridCell GridCell;
        public bool ThumbnailNeedsUpdate;
        public string Title; 

        private Variation(Dictionary<VariationParameter, InputValue> valuesForParameters)
        {
            ValuesForParameters = valuesForParameters;
            ThumbnailNeedsUpdate = true;
        }

        public Variation Clone()
        {
            var newVariation = new Variation(new Dictionary<VariationParameter, InputValue>(ValuesForParameters));
            newVariation.UpdateUndoCommand();
            return newVariation;
        }

        
        public void KeepCurrentAndApplyNewValues()
        {
            _changeCommand = CreateChangeCommand();
            _changeCommand.Do();
        }

        
        public void ApplyPermanently()
        {
            if (_changeCommand == null)
                return;
            
            UndoRedoStack.AddAndExecute(_changeCommand);
        }
        
        public void RestoreValues()
        {
            _changeCommand?.Undo();
        }

        private void InvalidateParameters()
        {
            foreach (var param in ValuesForParameters.Keys)
            {
                param.InputSlot.DirtyFlag.Invalidate(true);
            }
        }

        public void UpdateUndoCommand()
        {
            _changeCommand = CreateChangeCommand();
        }
        
        public static Variation Mix(IEnumerable<VariationParameter> variationParameters,
                                    IReadOnlyCollection<Tuple<Variation, float>> neighboursAndWeights, float scatter,
                                    GridCell cell = new GridCell())
        {
            // Collect neighbours
            var valuesForParameters = new Dictionary<VariationParameter, InputValue>();
            if (neighboursAndWeights.Count == 0)
            {
                foreach (var param in variationParameters)
                {
                    valuesForParameters.Add(param, param.InputSlot.Input.Value);
                }
                return new Variation(valuesForParameters)
                       {
                           GridCell = cell,
                       };
            }

            foreach (var param in variationParameters)
            {
                //var inconsistentParameters = !useDefault && !neighboursAndWeights.First().Item1.ValuesForParameters.ContainsKey(param);
                // if (useDefault)
                // {
                //     valuesForParameters.Add(param, param.InputSlot.Input.Value);
                //     continue;
                // }

                if (param.Type == typeof(float))
                {
                    var value = 0f;
                    var sumWeight = 0f;
                    foreach (var (neighbourVariation, weight) in neighboursAndWeights)
                    {
                        if (!neighbourVariation.ValuesForParameters.TryGetValue(param, out var matchingParam))
                            matchingParam = param.InputSlot.Input.Value;

                        //var matchingParam = neighbourVariation.ValuesForParameters[param];
                        if (matchingParam is InputValue<float> floatInput)
                        {
                            value += floatInput.Value * weight;
                            sumWeight += weight;
                        }
                    }

                    value *= 1f / sumWeight + ((float)Random.NextDouble() - 0.5f) * scatter;
                    value += Random.NextFloat(-scatter, scatter);
                    valuesForParameters.Add(param, new InputValue<float>(value));
                }

                // if (param.Type == typeof(Vector2))
                // {
                //     var value = Vector2.Zero;
                //     var sumWeight = 0f;
                //     foreach (var (neighbourVariation, weight) in neighboursAndWeights)
                //     {
                //         var matchingParam = neighbourVariation.ValuesForParameters[param];
                //         if (matchingParam is InputValue<Vector2> typedInput)
                //         {
                //             value += typedInput.Value * weight;
                //             sumWeight += weight;
                //         }
                //     }
                //
                //     value *= 1f / sumWeight;
                //     value += new Vector2(
                //                          Random.NextFloat(-scatter, scatter),
                //                          Random.NextFloat(-scatter, scatter)
                //                         );
                //
                //     valuesForParameters.Add(param, new InputValue<Vector2>(value));
                // }

                if (param.Type == typeof(Vector2))
                {
                    var value = Vector2.Zero;
                    var sumWeight = 0f;
                    foreach (var (neighbourVariation, weight) in neighboursAndWeights)
                    {
                        if (!neighbourVariation.ValuesForParameters.TryGetValue(param, out var matchingParam))
                            matchingParam = param.InputSlot.Input.Value;

                        //var matchingParam = neighbourVariation.ValuesForParameters[param];
                        if (matchingParam is InputValue<Vector2> typedInput)
                        {
                            value += typedInput.Value * weight;
                            sumWeight += weight;
                        }
                    }

                    value *= 1f / sumWeight;
                    value += new Vector2(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    valuesForParameters.Add(param, new InputValue<Vector2>(value));
                }

                if (param.Type == typeof(Vector3))
                {
                    var value = Vector3.Zero;
                    var sumWeight = 0f;
                    foreach (var (neighbourVariation, weight) in neighboursAndWeights)
                    {
                        if (!neighbourVariation.ValuesForParameters.TryGetValue(param, out var matchingParam))
                            matchingParam = param.InputSlot.Input.Value;
                        
                        //var matchingParam = neighbourVariation.ValuesForParameters[param];
                        if (matchingParam is InputValue<Vector3> typedInput)
                        {
                            value += typedInput.Value * weight;
                            sumWeight += weight;
                        }
                    }

                    value *= 1f / sumWeight;
                    value += new Vector3(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    valuesForParameters.Add(param, new InputValue<Vector3>(value));
                }

                if (param.Type == typeof(Vector4))
                {
                    var value = Vector4.Zero;
                    var sumWeight = 0f;
                    foreach (var (neighbourVariation, weight) in neighboursAndWeights)
                    {
                        if (!neighbourVariation.ValuesForParameters.TryGetValue(param, out var matchingParam))
                            matchingParam = param.InputSlot.Input.Value;
                            
                        if (matchingParam is InputValue<Vector4> typedInput)
                        {
                            value += typedInput.Value * weight;
                            sumWeight += weight;
                        }
                    }

                    value *= 1f / sumWeight;
                    value += new Vector4(
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter),
                                         Random.NextFloat(-scatter, scatter)
                                        );

                    valuesForParameters.Add(param, new InputValue<Vector4>(value));
                }
            }

            return new Variation(valuesForParameters)
                   {
                       GridCell = cell,
                   };
        }

        private MacroCommand CreateChangeCommand()
        {
            var commands = new List<ICommand>();

            foreach (var (param, value) in ValuesForParameters)
            {
                var newCommand = new ChangeInputValueCommand(param.Instance.Parent.Symbol, param.SymbolChildUi.Id, param.Input)
                                 {
                                     Value = value,
                                 };
                commands.Add(newCommand);
            }

            return new MacroCommand("Set Preset Values", commands);
        }

        public class VariationParameter
        {
            public List<Guid> InstanceIdPath = new List<Guid>();
            public Instance Instance => NodeOperations.GetInstanceFromIdPath(InstanceIdPath);
            public SymbolChildUi SymbolChildUi;
            public IInputSlot InputSlot { get; set; }
            public SymbolChild.Input Input;
            public Type Type;
            public float Strength = 1;
        }

        public readonly Dictionary<VariationParameter, InputValue> ValuesForParameters;

        private static readonly Random Random = new Random();
        private ICommand _changeCommand;
    }
}