using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.Interaction;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Editing of a set of curves and keyframes independent of the actual visualization.
    /// </summary>
    /// <remarks>This provides basic curve editing functionality outside a timeline context, e.g. for CurveParameters</remarks>
    public abstract class CurveEditing
    {
        protected readonly HashSet<VDefinition> SelectedKeyframes = new HashSet<VDefinition>();
        protected abstract IEnumerable<Curve> GetAllCurves();
        protected abstract void ViewAllOrSelectedKeys(bool alsoChangeTimeRange = false);
        protected abstract void DeleteSelectedKeyframes();
        protected internal abstract void HandleCurvePointDragging(VDefinition vDef, bool isSelected);
        
        /// <summary>
        /// Helper function to extract vDefs from all or selected UI controls across all curves in CurveEditor
        /// </summary>
        /// <returns>a list curves with a list of vDefs</returns>
        private IEnumerable<VDefinition> GetSelectedOrAllPoints()
        {
            var result = new List<VDefinition>();

            if (SelectedKeyframes.Count > 0)
            {
                result.AddRange(SelectedKeyframes);
            }
            else
            {
                foreach (var curve in GetAllCurves())
                {
                    result.AddRange(curve.GetVDefinitions());
                }
            }

            return result;
        }

        public void DrawContextMenu()
        {
            CustomComponents.DrawContextMenuForScrollCanvas
                (
                 () =>
                 {
                     var selectedInterpolations = GetSelectedKeyframeInterpolationTypes();

                     var editModes = selectedInterpolations as VDefinition.EditMode[] ?? selectedInterpolations.ToArray();

                     if (ImGui.MenuItem("Smooth", null, editModes.Contains(VDefinition.EditMode.Smooth)))
                         OnSmooth();

                     if (ImGui.MenuItem("Cubic", null, editModes.Contains(VDefinition.EditMode.Cubic)))
                         OnCubic();

                     if (ImGui.MenuItem("Horizontal", null, editModes.Contains(VDefinition.EditMode.Horizontal)))
                         OnHorizontal();

                     if (ImGui.MenuItem("Constant", null, editModes.Contains(VDefinition.EditMode.Constant)))
                         OnConstant();

                     if (ImGui.MenuItem("Linear", null, editModes.Contains(VDefinition.EditMode.Linear)))
                         OnLinear();


                     if (ImGui.BeginMenu("Before curve..."))
                     {
                         foreach (Utils.OutsideCurveBehavior mapping in Enum.GetValues(typeof(Utils.OutsideCurveBehavior)))
                         {
                             if (ImGui.MenuItem(mapping.ToString(), null))
                                 ApplyPreCurveMapping(mapping);
                         }
                         ImGui.EndMenu();
                     }
                     
                     if (ImGui.BeginMenu("After curve..."))
                     {
                         foreach (Utils.OutsideCurveBehavior mapping in Enum.GetValues(typeof(Utils.OutsideCurveBehavior)))
                         {
                             if (ImGui.MenuItem(mapping.ToString(), null))
                                 ApplyPostCurveMapping(mapping);
                         }
                         ImGui.EndMenu();
                     }
                     
                     if (ImGui.MenuItem(SelectedKeyframes.Count > 0 ? "View Selected" : "View All", "F"))
                         ViewAllOrSelectedKeys();

                     if (ImGui.MenuItem("Delete keyframes"))
                         DeleteSelectedKeyframes();
                     
                     if (ImGui.MenuItem("Duplicate keyframes"))
                         DuplicateSelectedKeyframes(TimeLineCanvas.Current.Playback.TimeInBars);
                     
                 }, ref _contextMenuIsOpen
                );
        }
                
        private bool _contextMenuIsOpen;
        
        private delegate void DoSomethingWithKeyframeDelegate(VDefinition v);

        private void ForSelectedOrAllPointsDo(DoSomethingWithKeyframeDelegate doFunc)
        {
            UpdateCurveAndMakeUpdateKeyframeCommands(doFunc);
        }

        private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingWithKeyframeDelegate doFunc)
        {
            foreach (var keyframe in GetSelectedOrAllPoints())
            {
                doFunc(keyframe);
            }
        }

        private void OnSmooth()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Smooth;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.OutEditMode = VDefinition.EditMode.Smooth;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                     });
        }

        private void OnCubic()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Cubic;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.OutEditMode = VDefinition.EditMode.Cubic;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                     });
        }

        private void OnHorizontal()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;

                                         vDef.InEditMode = VDefinition.EditMode.Horizontal;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.InTangentAngle = 0;

                                         vDef.OutEditMode = VDefinition.EditMode.Horizontal;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                         vDef.OutTangentAngle = Math.PI;
                                     });
        }

        private void OnConstant()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = true;
                                         vDef.OutType = VDefinition.Interpolation.Constant;
                                         vDef.OutEditMode = VDefinition.EditMode.Constant;
                                     });
        }

        private void OnLinear()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Linear;
                                         vDef.InType = VDefinition.Interpolation.Linear;
                                         vDef.OutEditMode = VDefinition.EditMode.Linear;
                                         vDef.OutType = VDefinition.Interpolation.Linear;
                                     });
        }

        private void ApplyPostCurveMapping(Utils.OutsideCurveBehavior mapping)
        {
            foreach (var curve in GetAllCurves())
            { 
                curve.PostCurveMapping = mapping;
            }
        }
        
        private void ApplyPreCurveMapping(Utils.OutsideCurveBehavior mapping)
        {
            foreach (var curve in GetAllCurves())
            { 
                curve.PreCurveMapping = mapping;
            }
        }
        
        private IEnumerable<VDefinition.EditMode> GetSelectedKeyframeInterpolationTypes()
        {
            var checkedInterpolationTypes = new HashSet<VDefinition.EditMode>();
            foreach (var point in GetSelectedOrAllPoints())
            {
                checkedInterpolationTypes.Add(point.OutEditMode);
                checkedInterpolationTypes.Add(point.InEditMode);
            }

            return checkedInterpolationTypes;
        }
        
        
        protected IEnumerable<VDefinition> GetAllKeyframes()
        {
            return from curve in GetAllCurves()
                   from keyframe in curve.GetVDefinitions()
                   select keyframe;
        }
        
        
        protected void DuplicateSelectedKeyframes(double targetTime)
        {
            if (!SelectedKeyframes.Any())
            {
                Log.Debug("Select keyframes to duplicate to current time");
                return;
            }

            var minTime = float.PositiveInfinity;
            foreach (var key in SelectedKeyframes)
            {
                minTime = Math.Min((float)key.U, minTime);
            }

            var newSelection = new HashSet<VDefinition>();

            foreach (var curve in GetAllCurves())
            {
                foreach (var key in curve.GetVDefinitions().ToList())
                {
                    if (!SelectedKeyframes.Contains(key))
                        continue;

                    var timeOffset = key.U - minTime;
                    var newKey = key.Clone();
                    curve.AddOrUpdateV(targetTime + timeOffset, newKey);
                    newSelection.Add(newKey);
                }
            }

            RebuildCurveTables();
            SelectedKeyframes.Clear();
            SelectedKeyframes.UnionWith(newSelection);
        }

        /// <summary>
        /// A horrible hack to keep curve table-structure aligned with position stored in key definitions.
        /// </summary>
        protected void RebuildCurveTables()
        {
            foreach (var curve in GetAllCurves())
            {
                foreach (var (u, vDef) in curve.GetPointTable())
                {
                    if (Math.Abs(u - vDef.U) > 0.001f)
                    {
                        curve.MoveKey(u, vDef.U);
                    }
                }
            }
        }

        protected ScalableCanvas.Scope GetScopeForRelevantKeyframes()
        {
            const float curveValuePadding = 0.3f;
            const float curveTimePadding = 0.1f;

            var minU = double.PositiveInfinity;
            var maxU = double.NegativeInfinity;
            var minV = double.PositiveInfinity;
            var maxV = double.NegativeInfinity;
            var numPoints = 0;

            switch (SelectedKeyframes.Count)
            {
                case 0:
                case 1:
                {
                    foreach (var vDef in GetAllKeyframes())
                    {
                        numPoints++;
                        minU = Math.Min(minU, vDef.U);
                        maxU = Math.Max(maxU, vDef.U);
                        minV = Math.Min(minV, vDef.Value);
                        maxV = Math.Max(maxV, vDef.Value);
                    }

                    break;
                }
                // case 1:
                //     return;

                default:
                {
                    foreach (var element in SelectedKeyframes)
                    {
                        numPoints++;
                        minU = Math.Min(minU, element.U);
                        maxU = Math.Max(maxU, element.U);
                        minV = Math.Min(minV, element.Value);
                        maxV = Math.Max(maxV, element.Value);
                    }

                    break;
                }
            }

            if (numPoints == 0)
            {
                minV = -3;
                maxV = +3;
                minU = -2;
                maxU = 10;
            }

            if (Math.Abs(maxU - minU) < 0.001f)
            {
                minU -= 1;
            }

            if (Math.Abs(maxV - minU) < 0.001f)
            {
                maxV += -1;
                minV -= 1;
            }

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var scaleX = (float)(size.X / ((maxU - minU) * (1 + 2 * curveTimePadding)));
            var scaleY = -(float)(size.Y / ((maxV - minV) * (1 + 2 * curveValuePadding)));
            return new ScalableCanvas.Scope(new Vector2(scaleX, scaleY),
                                            new Vector2(
                                                                (float)minU - 150 / scaleX,
                                                                (float)maxV - 20 / scaleY
                                                               )
                                           );
        }
        
    }
}