using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Editor.Gui.Interaction.WithCurves;

namespace T3.Editor.Gui.Windows.TimeLine
{
    /// <summary>
    /// Links to AnimationParameters to editors like DopeSheets or <see cref="TimelineCurveEditArea"/>>
    /// </summary>
    public abstract class AnimationParameterEditing : CurveEditing
    {
        protected override IEnumerable<Curve> GetAllCurves()
        {
            return AnimationParameters.SelectMany(param => param.Curves);
        }

        protected override void DeleteSelectedKeyframes()
        {
            TimeLineCanvas.DeleteSelectedElements();
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in SelectedKeyframes)
            {
                timeRange.Unite((float)s.U);
            }

            return timeRange;
        }

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var vDefinition in SelectedKeyframes)
            {
                vDefinition.U = originU + (vDefinition.U - originU) * scaleU;
            }

            RebuildCurveTables();
        }

        protected override void ViewAllOrSelectedKeys(bool alsoChangeTimeRange = false)
        {
            var bounds = GetBoundsOnCanvas(GetSelectedOrAllPoints());
            TimeLineCanvas.Current.SetScopeToCanvasArea(bounds, flipY:true);
        }

        protected List<TimeLineCanvas.AnimationParameter> AnimationParameters;
        protected TimeLineCanvas TimeLineCanvas; // This gets initialized in constructor of implementations 
        public static bool CurvesTablesNeedsRefresh;
    }
}