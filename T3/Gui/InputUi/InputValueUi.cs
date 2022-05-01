using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using t3.Gui.Commands.Graph;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.InputUi
{
    public abstract class InputValueUi<T> : IInputUi
    {
        private const float ConnectionAreaWidth = 30.0f;
        private float ParameterNameWidth => ImGui.GetTextLineHeight() * 120.0f / 16;

        public SymbolUi Parent { get; set; }
        public Symbol.InputDefinition InputDefinition { get; set; }
        public Guid Id => InputDefinition.Id;
        public Relevancy Relevancy { get; set; } = Relevancy.Optional;
        public virtual bool IsAnimatable => false;
        public virtual bool IsVariable => false;
        protected Type MappedType { get; set; }

        public abstract IInputUi Clone();

        public virtual void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator)
        {
            if (!IsAnimatable)
                Log.Warning("Should only be called for animated input types");
            else
                Log.Warning("Input type has no implementation to insert values into animation curves");
        }

        /// <summary>
        /// Wraps the implementation of an parameter control to handle <see cref="InputEditStateFlags"/>
        /// </summary>
        protected abstract InputEditStateFlags DrawEditControl(string name, ref T value);

        protected abstract void DrawReadOnlyControl(string name, ref T value);

        protected virtual string GetSlotValueAsString(ref T value)
        {
            return String.Empty;
        }

        protected virtual void DrawAnimatedValue(string name, InputSlot<T> inputSlot, Animator animator)
        {
        }

        public virtual string GetSlotValue(IInputSlot inputSlot)
        {
            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                return GetSlotValueAsString(ref typedInputSlot.Value);
            }

            return string.Empty;
        }

        private readonly Icon[] _keyframeButtonIcons = new[]
                                                           {
                                                               Icon.KeyframeToggleOffNone,
                                                               Icon.KeyframeToggleOffLeft,
                                                               Icon.KeyframeToggleOffRight,
                                                               Icon.KeyframeToggleOffBoth,
                                                               Icon.KeyframeToggleOnNone,
                                                               Icon.KeyframeToggleOnLeft,
                                                               Icon.KeyframeToggleOnRight,
                                                               Icon.KeyframeToggleOnBoth,
                                                           };

        public InputEditStateFlags DrawInputEdit(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            var name = inputSlot.Input.Name;
            var editState = InputEditStateFlags.Nothing;
            var typeColor = TypeUiRegistry.Entries[Type].Color;
            var compositionSymbol = compositionUi.Symbol;
            var animator = compositionSymbol.Animator;

            Curve animationCurve = null;
            bool isAnimated = IsAnimatable && animator.TryGetFirstInputAnimationCurve(inputSlot, out animationCurve);
            MappedType = inputSlot.MappedType;

            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                var input = inputSlot.Input;
                if (inputSlot.IsConnected)
                {
                    if (typedInputSlot.IsMultiInput)
                    {
                        // Just show actual value
                        ImGui.Button(name + "##paramName", new Vector2(-1, 0));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditStateFlags.ShowOptions;

                            ImGui.EndPopup();
                        }

                        var multiInput = (MultiInputSlot<T>)typedInputSlot;
                        var allInputs = multiInput.GetCollectedTypedInputs();

                        for (int multiInputIndex = 0; multiInputIndex < allInputs.Count; multiInputIndex++)
                        {
                            ImGui.PushID(multiInputIndex);

                            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                            if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0)))
                            {
                                // TODO: implement with proper SelectionManager
                                //var compositionSymbol = compositionSymbol;
                                var allConnections = compositionSymbol.Connections.FindAll(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                                var connection = allConnections[multiInputIndex];
                                var sourceUi = compositionUi.GetSelectables()
                                                            .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                            }

                            ImGui.PopStyleColor();

                            ImGui.SameLine();

                            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                            ImGui.Button("#" + multiInputIndex, new Vector2(ParameterNameWidth, 0.0f));
                            ImGui.PopStyleVar();
                            ImGui.SameLine();

                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.ConnectedParameterColor.Rgba);
                            var slot = allInputs[multiInputIndex];
                            DrawReadOnlyControl("##multiInputParam", ref slot.Value);
                            ImGui.PopStyleColor();
                            ImGui.PopID();
                        }

                        ImGui.Spacing();
                    }
                    else
                    {
                        // Connected single inputs
                        ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                        if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0.0f)))
                        {
                            // TODO: implement with proper selectionManager
                            //var compositionSymbol = compositionSymbol;
                            var connection = compositionSymbol.Connections.First(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                            var sourceUi = compositionUi.GetSelectables()
                                                        .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                            // Try to find instance
                            if (sourceUi is SymbolChildUi sourceSymbolChildUi)
                            {
                                var selectedInstance = NodeSelection.GetFirstSelectedInstance();
                                var parent = selectedInstance.Parent;
                                var selectionTargetInstance = parent.Children.Single(instance => instance.SymbolChildId == sourceUi.Id);
                                NodeSelection.SetSelectionToChildUi(sourceSymbolChildUi, selectionTargetInstance);
                                FitViewToSelectionHandling.FitViewToSelection();
                            }
                        }

                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // Draw Name
                        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                        ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditStateFlags.ShowOptions;

                            ImGui.EndPopup();
                        }

                        ImGui.PopStyleVar();

                        ImGui.SameLine();

                        // Draw control
                        ImGui.PushItemWidth(200.0f);
                        ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                        ImGui.SetNextItemWidth(-1);

                        DrawReadOnlyControl(name, ref typedInputSlot.Value);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
                }
                else if (isAnimated)
                {
                    var hasKeyframeAtCurrentTime = animationCurve.HasVAt(EvaluationContext.GlobalTimeForKeyframes);
                    var hasKeyframeBefore = animationCurve.ExistVBefore(EvaluationContext.GlobalTimeForKeyframes);
                    var hasKeyframeAfter = animationCurve.ExistVAfter(EvaluationContext.GlobalTimeForKeyframes);

                    var iconIndex = 0;
                    const int leftBit = 1 << 0;
                    const int rightBit = 1 << 1;
                    const int onBit = 1 << 2;

                    if (hasKeyframeBefore) iconIndex |= leftBit;
                    if (hasKeyframeAfter) iconIndex |= rightBit;
                    if (hasKeyframeAtCurrentTime) iconIndex |= onBit;
                    var icon = _keyframeButtonIcons[iconIndex];

                    if (ImGui.Button("##icon", new Vector2(ConnectionAreaWidth, 0.0f)))
                    {
                        if (hasKeyframeAtCurrentTime)
                        {
                            AnimationOperations.RemoveKeyframeFromCurves(animator.GetCurvesForInput(inputSlot),
                                                                         EvaluationContext.GlobalTimeForKeyframes);
                        }
                        else
                        {
                            AnimationOperations.InsertKeyframeToCurves(animator.GetCurvesForInput(inputSlot),
                                                                       EvaluationContext.GlobalTimeForKeyframes);
                        }
                    }

                    Icons.DrawIconOnLastItem(icon);

                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            if (ImGui.MenuItem("Jump To Previous Keyframe", hasKeyframeBefore))
                                                            {
                                                                UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToPreviousKeyframe);
                                                            }

                                                            if (ImGui.MenuItem("Jump To Next Keyframe", hasKeyframeBefore))
                                                            {
                                                                UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToNextKeyframe);
                                                            }

                                                            if (hasKeyframeAtCurrentTime)
                                                            {
                                                                if (ImGui.MenuItem("Remove keyframe"))
                                                                {
                                                                    AnimationOperations.RemoveKeyframeFromCurves(animator.GetCurvesForInput(inputSlot),
                                                                        EvaluationContext.GlobalTimeForKeyframes);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (ImGui.MenuItem("Insert keyframe"))
                                                                {
                                                                    AnimationOperations.InsertKeyframeToCurves(animator.GetCurvesForInput(inputSlot),
                                                                        EvaluationContext.GlobalTimeForKeyframes);
                                                                }
                                                            }

                                                            ImGui.Separator();

                                                            if (ImGui.MenuItem("Remove Animation"))
                                                                animator.RemoveAnimationFrom(inputSlot);

                                                            if (ImGui.MenuItem("Parameters settings"))
                                                                editState = InputEditStateFlags.ShowOptions;
                                                        });
                    ImGui.PopStyleVar();
                    ImGui.SameLine();

                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Orange.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Color.Black.Rgba);

                    ImGui.SetNextItemWidth(-1);

                    DrawAnimatedValue(name, typedInputSlot, animator); // todo: command integration

                    ImGui.PopStyleColor(2);
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(typeColor).Rgba);

                    var hash = Utilities.Hash(symbolChildUi.SymbolChild.Id, input.InputDefinition.Id);
                    var blendGroup = T3Ui.VariationHandling.ActiveOperatorVariation?.GetBlendGroupForHashedInput(hash);

                    var label = blendGroup == null ? "" : "G" + (blendGroup.Index + 1);

                    if (ImGui.Button(label, new Vector2(ConnectionAreaWidth, 0.0f)))
                    {
                        if (IsAnimatable)
                            animator.CreateInputUpdateAction(inputSlot); // todo: create command
                    }

                    if (ImGui.IsItemActive() && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() > UserSettings.Config.ClickThreshold)
                    {
                        if (ConnectionMaker.TempConnections.Count == 0)
                        {
                            ConnectionMaker.StartFromInputSlot(compositionSymbol, symbolChildUi, InputDefinition);
                        }
                    }

                    if (ImGui.IsItemHovered() && IsAnimatable)
                        ImGui.SetTooltip($"Click to animate\n{input.DefaultValue.ValueType}");

                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));

                    if (input.IsDefault)
                    {
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, T3Style.Colors.ButtonColor.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.TextMuted.Rgba);
                        ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                        ImGui.PopStyleColor(2);
                        ImGui.SameLine();
                    }
                    else
                    {
                        var isClicked = ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                        ImGui.SameLine();
                        if (!input.IsDefault)
                        {
                            Icons.DrawIconAtScreenPosition(Icon.Revert, ImGui.GetItemRectMin() + new Vector2(6, 2));
                            if (isClicked)
                            {
                                UndoRedoStack.AddAndExecute(new ResetInputToDefault(compositionSymbol, symbolChildUi.Id, input));
                            }
                        }
                    }

                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            if (ImGui.MenuItem("Set as default", !input.IsDefault))
                                                            {
                                                                input.SetCurrentValueAsDefault();
                                                                var symbolUi = SymbolUiRegistry.Entries[symbolChildUi.SymbolChild.Symbol.Id];
                                                                symbolUi.FlagAsModified();
                                                            }

                                                            if (ImGui.MenuItem("Reset to default", !input.IsDefault))
                                                            {
                                                                UndoRedoStack.AddAndExecute(new ResetInputToDefault(compositionSymbol, symbolChildUi.Id,
                                                                                                input));
                                                            }

                                                            if (blendGroup == null && ImGui.BeginMenu("Add to Blending", true))
                                                            {
                                                                T3Ui.VariationHandling.DrawInputContextMenu(inputSlot, compositionUi, symbolChildUi);
                                                            }

                                                            if (blendGroup != null && ImGui.MenuItem("Remove blending"))
                                                            {
                                                                T3Ui.VariationHandling.ActiveOperatorVariation?.RemoveBlending(hash);
                                                            }

                                                            if (ImGui.MenuItem("Publish as Input"))
                                                            {
                                                                PublishAsInput(inputSlot, symbolChildUi, input);
                                                            }

                                                            if (ImGui.MenuItem("Parameters settings"))
                                                                editState = InputEditStateFlags.ShowOptions;
                                                        });

                    ImGui.PopStyleVar();

                    // Draw control
                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                    if (input.IsDefault)
                    {
                        // handling default values is a bit tricky with ImGui as we want to show the default
                        // value when this is set, but we never want the default value to be modified. But as
                        // editing is already done when the return value of the ImGui edit control tells us
                        // that editing has happened this here is a simple way to ensure that the default value
                        // is always correct but editing is only happening on the input value.
                        // bool isEditableInputType = !input.Value.ValueType.IsValueType && typedInputSlot.TypedDefaultValue.Value is IEditableInputType;
                        input.Value.Assign(input.DefaultValue);
                    }

                    ImGui.SetNextItemWidth(-1);
                    editState |= DrawEditControl(name, ref typedInputSlot.TypedInputValue.Value);

                    if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified ||
                        (editState & InputEditStateFlags.Finished) == InputEditStateFlags.Finished)
                    {
                        compositionSymbol.InvalidateInputInAllChildInstances(inputSlot);
                    }

                    if ((editState & InputEditStateFlags.ResetToDefault) == InputEditStateFlags.ResetToDefault)
                    {
                        input.ResetToDefault();
                        compositionSymbol.InvalidateInputInAllChildInstances(inputSlot);
                    }

                    input.IsDefault &= (editState & InputEditStateFlags.Modified) != InputEditStateFlags.Modified;

                    ImGui.PopStyleColor();
                    ImGui.PopItemWidth();
                }
            }
            else

            {
                Debug.Assert(false);
            }

            return editState;
        }

        private static void PublishAsInput(IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input)
        {
            var composition = NodeSelection.GetSelectedComposition();
            if (composition == null)
            {
                composition = inputSlot.Parent.Parent;
            }

            if (composition == null)
            {
                Log.Warning("Can't publish input to undefined composition");
                return;
            }

            NodeOperations.AddInputToSymbol(input.Name, input.InputDefinition.IsMultiInput, input.DefaultValue.ValueType, composition.Symbol);
            NodeOperations.UpdateChangedOperators();

            var updatedComposition = NodeOperations.GetInstanceFromIdPath(NodeOperations.BuildIdPathForInstance(composition));

            var newInput = updatedComposition.Symbol.InputDefinitions.SingleOrDefault(i => i.Name == input.Name);
            if (newInput != null)
            {
                var cmd = new AddConnectionCommand(updatedComposition.Symbol,
                                                   new Symbol.Connection(sourceParentOrChildId: ConnectionMaker.UseSymbolContainerId,
                                                                         sourceSlotId: newInput.Id,
                                                                         targetParentOrChildId: symbolChildUi.Id,
                                                                         targetSlotId: input.InputDefinition.Id),
                                                   0);
                cmd.Do();
                newInput.DefaultValue = input.Value.Clone();
                inputSlot.DirtyFlag.Invalidate();
            }
        }

        public virtual void DrawSettings()
        {
            Type enumType = typeof(Relevancy);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(Relevancy), values.GetValue(i));
            }

            int index = (int)Relevancy;
            ImGui.Combo("##dropDownRelevancy", ref index, valueNames, valueNames.Length);
            Relevancy = (Relevancy)index;
            ImGui.SameLine();
            ImGui.TextUnformatted("Relevancy");
        }

        public virtual void Write(JsonTextWriter writer)
        {
            if (Relevancy != DefaultRelevancy)
                writer.WriteObject("Relevancy", Relevancy.ToString());

            var vec2writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            writer.WritePropertyName("Position");
            vec2writer(writer, PosOnCanvas);
        }

        public virtual void Read(JToken inputToken)
        {
            Relevancy = (inputToken["Relevancy"] == null)
                            ? DefaultRelevancy
                            : (Relevancy)Enum.Parse(typeof(Relevancy), inputToken["Relevancy"].ToString());

            JToken positionToken = inputToken["Position"];
            PosOnCanvas = new Vector2(positionToken["X"].Value<float>(), positionToken["Y"].Value<float>());
        }

        public Type Type { get; } = typeof(T);

        /// <summary>
        /// Defines position of inputNode within graph 
        /// </summary>
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;

        public Vector2 Size { get; set; } = SymbolChildUi.DefaultOpSize;
        public bool IsSelected => NodeSelection.IsNodeSelected(this);

        // ReSharper disable once StaticMemberInGenericType
        private static readonly string _revertLabel = $"{(char)Icon.Revert}##revert";
        private const Relevancy DefaultRelevancy = Relevancy.Optional;
    }
}