using System.Diagnostics;
using System.Linq;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9eafb45c_2d1c_4110_aa64_cdef1b27e1a1
{
    public class ListExtractor : Instance<ListExtractor>
    {
        [Output(Guid = "b2cb39f9-7a86-4b26-859e-ab08fe44ddb4")]
        public readonly Slot<int> Index = new Slot<int>();

        [Output(Guid = "5cc91f8b-c710-425c-8f59-c4a22716229e")]
        public readonly Slot<float> IndexUNorm = new Slot<float>();

        [Output(Guid = "df601073-4130-4dec-9d08-63f2b8795538")]
        public readonly Slot<int> Count = new Slot<int>();

        [Output(Guid = "19544074-d142-4db1-a051-e0932d983391")]
        public readonly Slot<string> Element = new Slot<string>();

        public ListExtractor()
        {
            Index.UpdateAction = Update;
            IndexUNorm.UpdateAction = Update;
            // IndexUNorm.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
            Count.UpdateAction = UpdateCount;
            Element.UpdateAction = Update;
            // Element.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        public void UpdateCount(EvaluationContext context)
        {
            Input.Update(context);
            Count.Value = Input.Value.Count;
        }

        public void Update(EvaluationContext context)
        {
            if (!_isIterating)
                IterStart(context);

            Log.Debug($"index: {_index}");
            if (Input.Value.Count > 0)
            {
                Index.Value = _index;
                Index.DirtyFlag.Clear();
                IndexUNorm.Value = (float)_index / (Input.Value.Count - 1);
                IndexUNorm.DirtyFlag.Clear();
                Element.Value = Input.Value[_index];
                Element.DirtyFlag.Clear();
            }

            if (++_index == Input.Value.Count)
                IterEnd();
        }

        
        public void IterStart(EvaluationContext context)
        {
            Input.Update(context);
            _index = 0;
            _isIterating = true;
        }

        public void IterEnd()
        {
            _isIterating = false;
        }
        

        [Input(Guid = "0f77f6e3-b585-4904-b452-d39a18f7e990")]
        public readonly InputSlot<System.Collections.Generic.List<string>> Input = new InputSlot<System.Collections.Generic.List<string>>();

        private bool _isIterating;
        private int _index;
    }
}

