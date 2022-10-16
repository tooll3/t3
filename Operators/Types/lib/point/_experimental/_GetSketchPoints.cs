using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_b238b288_6e9b_4b91_bac9_3d7566416028;

// ReSharper disable RedundantNameQualifier

namespace T3.Operators.Types.Id_271e397e_051c_473f_968f_a2251fed65d1
{
    public class _GetSketchPoints : Instance<_GetSketchPoints>
    {
        [Output(Guid = "9b1adfd0-2a94-438e-a0b9-6f6ae69f5690")]
        public readonly Slot<StructuredList> PointList = new();
        
        public _GetSketchPoints()
        {
            PointList.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {

            var pageIndex = PageIndex.GetValue(context);

            if (Pages.GetValue(context) is not Dictionary<int, _SketchImpl.Page> pages)
            {
                Log.Warning("Can't get pages from sketch implementation", SymbolChildId);
                PointList.Value = _emptyList;
                return;
            }

            if (!pages.TryGetValue(pageIndex, out var page))
            {
                //Log.Warning($"Nothing on page {pageIndex}", SymbolChildId);
                PointList.Value = _emptyList;
                return;
            }

            PointList.Value = page.PointsList;
        }

        private static readonly StructuredList<Point> _emptyList = new(0);

        [Input(Guid = "C7C71DE4-A9A8-4194-B490-AB48004565D1")]
        public readonly InputSlot<object> Pages = new();
        
        [Input(Guid = "1b8d9a00-f337-431e-bab3-24d45ed9d191")]
        public readonly InputSlot<int> PageIndex = new();

    }
}