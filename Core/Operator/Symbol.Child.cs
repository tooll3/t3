using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    public partial class Symbol
    {
        /// <summary>
        /// Represents an instance of a <see cref="Symbol"/> within a Symbol.
        /// </summary>
        public sealed class Child
        {
            /// <summary>A reference to the <see cref="Symbol"/> this is an instance from.</summary>
            public Symbol Symbol { get; init; }

            public Guid Id { get; init; }

            public Symbol Parent { get; }

            public string Name { get; set; }

            public string ReadableName => string.IsNullOrEmpty(Name) ? Symbol.Name : Name;

            public bool IsBypassed { get => _isBypassed; set => SetBypassed(value); }

            public Dictionary<Guid, Input> Inputs { get; private init; } = new();
            public Dictionary<Guid, Output> Outputs { get; private init; } = new();

            internal Child(Symbol symbol, Guid childId, Symbol parent, string name, bool isBypassed)
            {
                Symbol = symbol;
                Id = childId;
                Parent = parent;
                Name = name ?? string.Empty;
                _isBypassed = isBypassed;

                foreach (var inputDefinition in symbol.InputDefinitions)
                {
                    if (!Inputs.TryAdd(inputDefinition.Id, new Input(inputDefinition)))
                    {
                        throw new ApplicationException($"The ID for symbol input {symbol.Name}.{inputDefinition.Name} must be unique.");
                    }
                }

                foreach (var outputDefinition in symbol.OutputDefinitions)
                {
                    Symbol.OutputDefinition.TryGetNewOutputDataType(outputDefinition, out var outputData);
                    var output = new Output(outputDefinition, outputData) { DirtyFlagTrigger = outputDefinition.DirtyFlagTrigger };
                    if (!Outputs.TryAdd(outputDefinition.Id, output))
                    {
                        throw new ApplicationException($"The ID for symbol output {symbol.Name}.{outputDefinition.Name} must be unique.");
                    }
                }
            }

            #region sub classes =============================================================

            public sealed class Output
            {
                public Symbol.OutputDefinition OutputDefinition { get; }
                public IOutputData OutputData { get; }

                public bool IsDisabled { get; set; }

                public DirtyFlagTrigger DirtyFlagTrigger
                {
                    get => _dirtyFlagTrigger ?? OutputDefinition.DirtyFlagTrigger;
                    set => _dirtyFlagTrigger = (value != OutputDefinition.DirtyFlagTrigger) ? (DirtyFlagTrigger?)value : null;
                }

                private DirtyFlagTrigger? _dirtyFlagTrigger = null;

                public Output(Symbol.OutputDefinition outputDefinition, IOutputData outputData)
                {
                    OutputDefinition = outputDefinition;
                    OutputData = outputData;
                }

                public Output DeepCopy()
                {
                    return new Output(OutputDefinition, OutputData);
                }
            }

            public sealed class Input
            {
                public Symbol.InputDefinition InputDefinition { get; }
                public Guid Id => InputDefinition.Id;
                public bool IsMultiInput => InputDefinition.IsMultiInput;
                public InputValue DefaultValue => InputDefinition.DefaultValue;

                public string Name => InputDefinition.Name;

                /// <summary>The input value used for this symbol child</summary>
                public InputValue Value { get; }

                public bool IsDefault { get; set; }

                public Input(Symbol.InputDefinition inputDefinition)
                {
                    InputDefinition = inputDefinition;
                    Value = DefaultValue.Clone();
                    IsDefault = true;
                }

                public void SetCurrentValueAsDefault()
                {
                    if (DefaultValue.IsEditableInputReferenceType)
                    {
                        DefaultValue.AssignClone(Value);
                    }
                    else
                    {
                        DefaultValue.Assign(Value);
                    }

                    IsDefault = true;
                }

                public void ResetToDefault()
                {
                    if (DefaultValue.IsEditableInputReferenceType)
                    {
                        Value.AssignClone(DefaultValue);
                    }
                    else
                    {
                        Value.Assign(DefaultValue);
                    }

                    IsDefault = true;
                }
            }

            #endregion


            private bool _isBypassed;

            private bool IsBypassable()
            {
                if (Symbol.OutputDefinitions.Count == 0)
                    return false;

                if (Symbol.InputDefinitions.Count == 0)
                    return false;

                var mainInput = Symbol.InputDefinitions[0];
                var mainOutput = Symbol.OutputDefinitions[0];

                if (mainInput.DefaultValue.ValueType != mainOutput.ValueType)
                    return false;

                if (mainInput.DefaultValue.ValueType == typeof(Command))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(Texture2D))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(BufferWithViews))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(MeshBuffers))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(float))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(Vector2))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(Vector3))
                    return true;

                if (mainInput.DefaultValue.ValueType == typeof(string))
                    return true;

                return false;
            }

            private void SetBypassed(bool shouldBypass)
            {
                if (shouldBypass == _isBypassed)
                    return;

                if (!IsBypassable())
                    return;

                if (Parent == null)
                {
                    // Clarify: shouldn't this be shouldBypass?
                    _isBypassed = shouldBypass; // during loading parents are not yet assigned. This flag will later be used when creating instances
                    return;
                }


                if (Parent.InstancesOfSelf.Count == 0)
                {
                    _isBypassed = shouldBypass; // while duplicating / cloning as new symbol there are no instances yet.
                    return;
                }

                foreach (var instance in Parent._instancesOfChildren[Id])
                {
                    var mainInputSlot = instance.Inputs[0];
                    var mainOutputSlot = instance.Outputs[0];

                    var wasByPassed = false;

                    switch (mainOutputSlot)
                    {
                        case Slot<Command> commandOutput when mainInputSlot is Slot<Command> commandInput:
                            if (shouldBypass)
                            {
                                wasByPassed = commandOutput.TrySetBypassToInput(commandInput);
                            }
                            else
                            {
                                commandOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(commandInput);
                            break;

                        case Slot<BufferWithViews> bufferOutput when mainInputSlot is Slot<BufferWithViews> bufferInput:
                            if (shouldBypass)
                            {
                                wasByPassed = bufferOutput.TrySetBypassToInput(bufferInput);
                            }
                            else
                            {
                                bufferOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(bufferInput);
                            break;
                        case Slot<MeshBuffers> bufferOutput when mainInputSlot is Slot<MeshBuffers> bufferInput:
                            if (shouldBypass)
                            {
                                wasByPassed = bufferOutput.TrySetBypassToInput(bufferInput);
                            }
                            else
                            {
                                bufferOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(bufferInput);

                            break;
                        case Slot<Texture2D> texture2dOutput when mainInputSlot is Slot<Texture2D> texture2dInput:
                            if (shouldBypass)
                            {
                                wasByPassed = texture2dOutput.TrySetBypassToInput(texture2dInput);
                            }
                            else
                            {
                                texture2dOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(texture2dInput);

                            break;
                        case Slot<float> floatOutput when mainInputSlot is Slot<float> floatInput:
                            if (shouldBypass)
                            {
                                wasByPassed = floatOutput.TrySetBypassToInput(floatInput);
                            }
                            else
                            {
                                floatOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(floatInput);

                            break;

                        case Slot<System.Numerics.Vector2> vec2Output when mainInputSlot is Slot<System.Numerics.Vector2> vec2Input:
                            if (shouldBypass)
                            {
                                wasByPassed = vec2Output.TrySetBypassToInput(vec2Input);
                            }
                            else
                            {
                                vec2Output.RestoreUpdateAction();
                            }

                            InvalidateConnected(vec2Input);

                            break;
                        case Slot<System.Numerics.Vector3> vec3Output when mainInputSlot is Slot<System.Numerics.Vector3> vec3Input:
                            if (shouldBypass)
                            {
                                wasByPassed = vec3Output.TrySetBypassToInput(vec3Input);
                            }
                            else
                            {
                                vec3Output.RestoreUpdateAction();
                            }

                            InvalidateConnected(vec3Input);

                            break;
                        case Slot<string> stringOutput when mainInputSlot is Slot<string> stringInput:
                            if (shouldBypass)
                            {
                                wasByPassed = stringOutput.TrySetBypassToInput(stringInput);
                            }
                            else
                            {
                                stringOutput.RestoreUpdateAction();
                            }

                            InvalidateConnected(stringInput);
                            break;
                    }

                    _isBypassed = wasByPassed;
                }
            }

            private static void InvalidateConnected<T>(Slot<T> bufferInput)
            {
                if (bufferInput.TryGetAsMultiInputTyped(out var multiInput))
                {
                    foreach (var connection in multiInput.CollectedInputs)
                    {
                        InvalidateParentInputs(connection);
                    }
                }
                else
                {
                    var connection = bufferInput.FirstConnection;
                    InvalidateParentInputs(connection);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void InvalidateParentInputs(ISlot connection)
                {
                    if (connection.ValueType == typeof(string))
                        return;

                    connection.DirtyFlag.Invalidate();
                }
            }

            public override string ToString()
            {
                return Parent.Name + ">" + ReadableName;
            }

            internal static Guid CreateIdDeterministically(Symbol symbol, Symbol? parent)
            {
                //deterministically create a new guid from the symbol id
                using var hashComputer = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
                hashComputer.AppendData(symbol.Id.ToByteArray(), 0, 16);

                if (parent != null)
                {
                    hashComputer.AppendData(parent.Id.ToByteArray(), 0, 16);
                }

                // SHA1 is 20 bytes long, but we only need 16 bytes for a guid
                var newGuidBytes = new ReadOnlySpan<byte>(hashComputer.GetHashAndReset(), 0, 16);
                return new Guid(newGuidBytes);
            }
        }
    }
}
