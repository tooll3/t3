using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.InputUi;
using T3.Gui.InputUi.SingleControl;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class DopeSheetArea : AnimationParameterEditing, ITimeObjectManipulation, IValueSnapAttractor
    {
        public DopeSheetArea(ValueSnapHandler snapHandler, TimeLineCanvas timeLineCanvas)
        {
            _snapHandler = snapHandler;
            TimeLineCanvas = timeLineCanvas;
        }

        private TimeLineCanvas.AnimationParameter _currentAnimationParameter;

        public void Draw(Instance compositionOp, List<TimeLineCanvas.AnimationParameter> animationParameters)
        {
            _drawList = ImGui.GetWindowDrawList();
            AnimationParameters = animationParameters;
            _compositionOp = compositionOp;

            ImGui.BeginGroup();
            {
                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    ViewAllOrSelectedKeys(alsoChangeTimeRange: true);

                if (KeyboardBinding.Triggered(UserActions.Duplicate))
                    DuplicateSelectedKeyframes(TimeLineCanvas.Playback.TimeInBars);

                if (KeyboardBinding.Triggered(UserActions.InsertKeyframe))
                {
                    foreach (var p in AnimationParameters)
                    {
                        InsertNewKeyframe(p, (float)TimeLineCanvas.Playback.TimeInBars);
                    }
                }

                if (KeyboardBinding.Triggered(UserActions.InsertKeyframeWithIncrement))
                {
                    foreach (var p in AnimationParameters)
                    {
                        InsertNewKeyframe(p, (float)TimeLineCanvas.Playback.TimeInBars, false, 1);
                    }
                }

                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();

                for (var index = 0; index < animationParameters.Count; index++)
                {
                    var parameter = animationParameters[index];
                    _currentAnimationParameter = parameter;
                    ImGui.PushID(index);
                    DrawProperty(parameter);
                    ImGui.PopID();
                }

                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        private void DrawProperty(TimeLineCanvas.AnimationParameter parameter)
        {
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X, LayerHeight - 1);
            _drawList.AddRectFilled(new Vector2(min.X, max.Y),
                                    new Vector2(max.X, max.Y + 1), Color.Black);

            var mousePos = ImGui.GetMousePos();
            var mouseTime = TimeLineCanvas.InverseTransformX(mousePos.X);
            var layerArea = new ImRect(min, max);
            var layerHovered = ImGui.IsWindowHovered() && layerArea.Contains(mousePos);
            if (layerHovered)
            {
                ImGui.BeginTooltip();

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.Text(parameter.Input.Input.Name);

                foreach (var curve in parameter.Curves)
                {
                    var v = curve.GetSampledValue(mouseTime);
                    ImGui.Text($"{v:0.00}");
                }

                ImGui.PopFont();

                ImGui.EndTooltip();
            }

            ImGui.PushStyleColor(ImGuiCol.Text, layerHovered ? Color.White.Rgba : Color.Gray);
            ImGui.PushFont(Fonts.FontBold);

            var hash = parameter.Input.GetHashCode();
            var pinned = PinnedParameters.Contains(hash);
            if (CustomComponents.ToggleIconButton(Icon.Pin, "pin", ref pinned, new Vector2(16, 16)))
            {
                if (pinned)
                {
                    PinnedParameters.Add(hash);
                }
                else
                {
                    PinnedParameters.Remove(hash);
                }
            }

            ImGui.SameLine();
            ImGui.Text("  " + parameter.ChildUi.SymbolChild.ReadableName);
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("." + parameter.Input.Input.Name);
            ImGui.PopStyleColor();

            if (parameter.Curves.Count() == 4)
            {
                DrawCurveGradient(parameter, layerArea);
            }
            else
            {
                DrawCurveLines(parameter, layerArea);
            }

            HandleCreateNewKeyframes(parameter, layerArea);

            foreach (var curve in parameter.Curves)
            {
                var list = curve.GetPointTable();
                VDefinition lastVDef = null;
                for (var index = 0; index < list.Count; index++)
                {
                    var vDef = list[index].Value;
                    var nextVDef = (index < list.Count - 1) ? list[index + 1].Value : null;
                    DrawKeyframe(vDef, layerArea, parameter, nextVDef);
                    lastVDef = vDef;
                }
            }

            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight)); // Next Line
        }

        public readonly HashSet<int> PinnedParameters = new HashSet<int>();

        private void HandleCreateNewKeyframes(TimeLineCanvas.AnimationParameter parameter, ImRect layerArea)
        {
            var hoverNewKeyframe = !ImGui.IsAnyItemActive()
                                   && ImGui.IsWindowHovered()
                                   && ImGui.GetIO().KeyAlt
                                   && layerArea.Contains(ImGui.GetMousePos());
            if (!hoverNewKeyframe)
                return;

            var hoverTime = TimeLineCanvas.Current.InverseTransformX(ImGui.GetIO().MousePos.X);
            _snapHandler.CheckForSnapping(ref hoverTime, TimeLineCanvas.Current.Scale.X);

            if (ImGui.IsMouseReleased(0))
            {
                var dragDistance = ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length();
                if (dragDistance < 2)
                {
                    TimeLineCanvas.Current.ClearSelection();

                    InsertNewKeyframe(parameter, hoverTime, setPlaybackTime: true);
                }
            }
            else
            {
                var posOnScreen = new Vector2(
                                              TimeLineCanvas.Current.TransformX(hoverTime) - KeyframeIconWidth / 2 + 1,
                                              layerArea.Min.Y);
                Icons.Draw(Icon.KeyFrame, posOnScreen);
            }

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        private void InsertNewKeyframe(TimeLineCanvas.AnimationParameter parameter, float time, bool setPlaybackTime = false, float increment = 0)
        {
            foreach (var curve in parameter.Curves)
            {
                var value = curve.GetSampledValue(time);
                var previousU = curve.GetPreviousU(time);

                var key = (previousU != null)
                              ? curve.GetV(previousU.Value).Clone()
                              : new VDefinition();

                key.Value = value + increment;
                key.U = time;

                var oldKey = key;
                curve.AddOrUpdateV(time, key);
                SelectedKeyframes.Add(oldKey);
            }

            if (setPlaybackTime)
                TimeLineCanvas.Current.Playback.TimeInBars = time;
        }

        private static readonly Color GrayCurveColor = new Color(1f, 1f, 1.0f, 0.3f);

        private readonly Color[] _curveColors =
            {
                new Color(1f, 0.2f, 0.2f, 0.3f),
                new Color(0.1f, 1f, 0.2f, 0.3f),
                new Color(0.1f, 0.4f, 1.0f, 0.5f),
                GrayCurveColor,
            };

        private void DrawCurveLines(TimeLineCanvas.AnimationParameter parameter, ImRect layerArea)
        {
            const float padding = 2;
            // Lines
            var curveIndex = 0;
            foreach (var curve in parameter.Curves)
            {
                var points = curve.GetPointTable();
                if (points.Count == 0)
                    continue;

                var positions = new List<Vector2>();

                var minValue = float.PositiveInfinity;
                var maxValue = float.NegativeInfinity;
                foreach (var (_, vDef) in points)
                {
                    if (minValue > vDef.Value)
                        minValue = (float)vDef.Value;
                    if (maxValue < vDef.Value)
                        maxValue = (float)vDef.Value;
                }

                VDefinition lastVDef = null;
                float lastValue = 0;

                foreach (var (u, vDef) in points)
                {
                    if (lastVDef != null && lastVDef.OutEditMode == VDefinition.EditMode.Constant)
                    {
                        positions.Add(new Vector2(
                                                  TimeLineCanvas.Current.TransformX((float)u) - 1,
                                                  lastValue));
                    }

                    lastValue = MathUtils.RemapAndClamp((float)vDef.Value, maxValue, minValue, layerArea.Min.Y + padding, layerArea.Max.Y - padding);
                    positions.Add(new Vector2(
                                              TimeLineCanvas.Current.TransformX((float)u),
                                              lastValue));

                    lastVDef = vDef;
                }

                _drawList.AddPolyline(
                                      ref positions.ToArray()[0],
                                      positions.Count,
                                      parameter.Curves.Count() > 1 ? _curveColors[curveIndex % 4] : GrayCurveColor,
                                      false,
                                      2);
                curveIndex++;
            }
        }

        private void DrawCurveGradient(TimeLineCanvas.AnimationParameter parameter, ImRect layerArea)
        {
            if (parameter.Curves.Count() != 4)
                return;

            var curve = parameter.Curves.First();
            const float padding = 2;

            var points = curve.GetVDefinitions();
            var times = new float[points.Count];
            var colors = new Color[points.Count];

            var curves = parameter.Curves.ToList();

            var index = 0;
            foreach (var vDef in points)
            {
                times[index] = TimeLineCanvas.Current.TransformX((float)vDef.U);
                colors[index] = new Color(
                                          (float)vDef.Value,
                                          (float)curves[1].GetSampledValue(vDef.U),
                                          (float)curves[2].GetSampledValue(vDef.U),
                                          (float)curves[3].GetSampledValue(vDef.U)
                                         );
                index++;
            }

            for (var index2 = 0; index2 < times.Length - 1; index2++)
            {
                _drawList.AddRectFilledMultiColor(new Vector2(times[index2], layerArea.Min.Y + padding),
                                                  new Vector2(times[index2 + 1], layerArea.Max.Y - padding),
                                                  colors[index2],
                                                  colors[index2 + 1],
                                                  colors[index2 + 1],
                                                  colors[index2]);
            }
        }

        private void DrawKeyframe(VDefinition vDef, ImRect layerArea, TimeLineCanvas.AnimationParameter parameter,
                                  VDefinition nextVDef)
        {
            var posOnScreen = new Vector2(
                                          TimeLineCanvas.Current.TransformX((float)vDef.U) - KeyframeIconWidth / 2 + 1,
                                          layerArea.Min.Y);

            if (vDef.OutEditMode == VDefinition.EditMode.Constant)
            {
                var availableSpace = nextVDef != null
                                         ? TimeLineCanvas.Current.TransformX((float)nextVDef.U) - posOnScreen.X
                                         : 9999;

                if (availableSpace > 30)
                {
                    var labelPos = new Vector2(posOnScreen.X + KeyframeIconWidth / 2 + 1,
                                               layerArea.Min.Y + 5);

                    var color = Color.Orange.Fade(MathUtils.RemapAndClamp(availableSpace, 30, 50, 0, 1).Clamp(0, 1));
                    ImGui.PushFont(Fonts.FontSmall);
                    _drawList.AddText(labelPos, color, $"{vDef.Value:G3}");
                    ImGui.PopFont();
                }
            }

            var keyHash = (int)vDef.GetHashCode();
            ImGui.PushID(keyHash);
            {
                var isSelected = SelectedKeyframes.Contains(vDef);
                if (vDef.OutEditMode == VDefinition.EditMode.Constant)
                {
                    Icons.Draw(isSelected ? Icon.ConstantKeyframeSelected : Icon.ConstantKeyframe, posOnScreen);
                }
                else
                {
                    Icons.Draw(isSelected ? Icon.KeyFrameSelected : Icon.KeyFrame, posOnScreen);
                }

                ImGui.SetCursorScreenPos(posOnScreen);

                // Click released
                var keyframeSize = new Vector2(10, 24);
                if (ImGui.InvisibleButton("##key", keyframeSize))
                {
                    var justClicked = ImGui.GetMouseDragDelta().Length() < UserSettings.Config.ClickTreshold;
                    if (justClicked)
                    {
                        UpdateSelectionOnClickOrDrag(vDef, isSelected);
                        _clickedKeyframeHash = keyHash;

                        if (Math.Abs(TimeLineCanvas.Playback.PlaybackSpeed) < 0.001f)
                        {
                            TimeLineCanvas.Current.Playback.TimeInBars = vDef.U;
                        }
                    }

                    if (_changeKeyframesCommand != null)
                        TimeLineCanvas.Current.CompleteDragCommand();
                }

                HandleCurvePointDragging(vDef, isSelected);

                var valueInputVisible = isSelected && keyHash == _clickedKeyframeHash;
                if (valueInputVisible)
                {
                    var symbolUi = SymbolUiRegistry.Entries[parameter.ChildUi.SymbolChild.Symbol.Id];
                    var inputUi = symbolUi.InputUis[parameter.Input.Id];
                    if (inputUi is FloatInputUi floatInputUi)
                    {
                        var size = new Vector2(60, 25);
                        ImGui.SetCursorScreenPos(posOnScreen + new Vector2(-size.X / 2, keyframeSize.Y - 5));
                        ImGui.BeginChildFrame((uint)keyHash, size, ImGuiWindowFlags.NoScrollbar);
                        ImGui.PushFont(Fonts.FontSmall);
                        var tmp = (float)vDef.Value;
                        var result = floatInputUi.DrawEditControl(ref tmp);
                        if (result == InputEditStateFlags.Started)
                        {
                            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.SymbolChildId, SelectedKeyframes);
                        }

                        if ((result & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                        {
                            foreach (var k in SelectedKeyframes)
                            {
                                k.Value = tmp;
                            }
                        }

                        if ((result & InputEditStateFlags.Finished) == InputEditStateFlags.Finished && _changeKeyframesCommand != null)
                        {
                            _changeKeyframesCommand.StoreCurrentValues();
                            UndoRedoStack.AddAndExecute(_changeKeyframesCommand);
                            _changeKeyframesCommand = null;
                        }

                        //vDef.Value = tmp;
                        ImGui.PopFont();
                        ImGui.EndChildFrame();
                    }
                    else if (inputUi is IntInputUi intInputUi)
                    {
                        var size = new Vector2(60, 25);
                        ImGui.SetCursorScreenPos(posOnScreen + new Vector2(-size.X / 2, keyframeSize.Y - 5));
                        ImGui.BeginChildFrame((uint)keyHash, size, ImGuiWindowFlags.NoScrollbar);
                        ImGui.PushFont(Fonts.FontSmall);
                        var tmp = (int)vDef.Value;
                        var result = intInputUi.DrawEditControl(ref tmp);
                        if (result == InputEditStateFlags.Started)
                        {
                            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.SymbolChildId, SelectedKeyframes);
                        }

                        if ((result & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                        {
                            foreach (var k in SelectedKeyframes)
                            {
                                k.Value = tmp;
                            }
                        }

                        if ((result & InputEditStateFlags.Finished) == InputEditStateFlags.Finished && _changeKeyframesCommand != null)
                        {
                            _changeKeyframesCommand.StoreCurrentValues();
                            UndoRedoStack.AddAndExecute(_changeKeyframesCommand);
                            _changeKeyframesCommand = null;
                        }

                        //vDef.Value = tmp;
                        ImGui.PopFont();
                        ImGui.EndChildFrame();
                    }
                }

                ImGui.PopID();
            }
        }

        private int _clickedKeyframeHash;

        protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 1f))
                return;

            if (UpdateSelectionOnClickOrDrag(vDef, isSelected))
                return;

            if (_changeKeyframesCommand == null)
            {
                TimeLineCanvas.Current.StartDragCommand();
            }

            var newDragTime = TimeLineCanvas.Current.InverseTransformX(ImGui.GetIO().MousePos.X);

            if (!ImGui.GetIO().KeyShift)
                _snapHandler.CheckForSnapping(ref newDragTime, TimeLineCanvas.Current.Scale.X);

            TimeLineCanvas.Current.UpdateDragCommand(newDragTime - vDef.U, 0);
        }

        private bool UpdateSelectionOnClickOrDrag(VDefinition vDef, bool isSelected)
        {
            // Deselect
            if (ImGui.GetIO().KeyCtrl)
            {
                if (!isSelected)
                    return true;

                foreach (var k in FindParameterKeysAtPosition(vDef.U))
                {
                    SelectedKeyframes.Remove(k);
                }

                return true;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                foreach (var k in FindParameterKeysAtPosition(vDef.U))
                {
                    SelectedKeyframes.Add(k);
                }
            }

            return false;
        }

        private IEnumerable<VDefinition> FindParameterKeysAtPosition(double u)
        {
            foreach (var curve in _currentAnimationParameter.Curves)
            {
                var matchingKey = curve.GetVDefinitions().FirstOrDefault(vDef2 => Math.Abs(vDef2.U - u) < 1 / 120f);
                if (matchingKey != null)
                    yield return matchingKey;
            }
        }

        #region implement interface --------------------------------------------
        void ITimeObjectManipulation.ClearSelection()
        {
            SelectedKeyframes.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            if (selectMode == SelectionFence.SelectModes.Replace)
                SelectedKeyframes.Clear();

            var startTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Min.X);
            var endTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Max.X);

            var layerMinIndex = (screenArea.Min.Y - _minScreenPos.Y) / LayerHeight - 1;
            var layerMaxIndex = (screenArea.Max.Y - _minScreenPos.Y) / LayerHeight;

            var index = 0;
            foreach (var parameter in AnimationParameters)
            {
                if (index >= layerMinIndex && index <= layerMaxIndex)
                {
                    foreach (var c in parameter.Curves)
                    {
                        var matchingItems = c.GetPointTable()
                                             .Select(pair => pair.Value)
                                             .ToList()
                                             .FindAll(key => key.U <= endTime && key.U >= startTime);
                        switch (selectMode)
                        {
                            case SelectionFence.SelectModes.Add:
                            case SelectionFence.SelectModes.Replace:
                                SelectedKeyframes.UnionWith(matchingItems);
                                break;
                            case SelectionFence.SelectModes.Remove:
                                SelectedKeyframes.ExceptWith(matchingItems);
                                break;
                        }
                    }
                }

                index++;
            }
        }

        ICommand ITimeObjectManipulation.StartDragCommand()
        {
            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.Symbol.Id, SelectedKeyframes);
            return _changeKeyframesCommand;
        }

        void ITimeObjectManipulation.UpdateDragCommand(double dt, double dv)
        {
            foreach (var vDefinition in SelectedKeyframes)
            {
                vDefinition.U += dt;
            }

            RebuildCurveTables();
        }

        void ITimeObjectManipulation.UpdateDragAtStartPointCommand(double dt, double dv)
        {
        }

        void ITimeObjectManipulation.UpdateDragAtEndPointCommand(double dt, double dv)
        {
        }

        void ITimeObjectManipulation.CompleteDragCommand()
        {
            if (_changeKeyframesCommand == null)
                return;

            // Update reference in Macro-command
            _changeKeyframesCommand.StoreCurrentValues();
            // UndoRedoStack.Add(_changeKeyframesCommand);
            _changeKeyframesCommand = null;
        }

        void ITimeObjectManipulation.DeleteSelectedElements()
        {
            KeyframeOperations.DeleteSelectedKeyframesFromAnimationParameters(SelectedKeyframes, AnimationParameters);
            RebuildCurveTables();
        }
        #endregion

        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            SnapResult best = null;
            foreach (var vDefinition in GetAllKeyframes())
            {
                if (SelectedKeyframes.Contains(vDefinition))
                    continue;

                ValueSnapHandler.CheckForBetterSnapping(targetTime, vDefinition.U, canvasScale, ref best);
            }

            return best;
        }

        private const float KeyframeIconWidth = 10;
        private Vector2 _minScreenPos;
        private static ChangeKeyframesCommand _changeKeyframesCommand;
        public const int LayerHeight = 25;
        private Instance _compositionOp;
        private ImDrawListPtr _drawList;
        private readonly ValueSnapHandler _snapHandler;
    }
}