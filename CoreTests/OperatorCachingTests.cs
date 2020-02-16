using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace OperatorTests
{
    [TestClass]
    public class OperatorCachingTests
    {
        public class CompositionOperator : Instance<CompositionOperator>
        {
        }

        public class TestOperator : Instance<TestOperator>
        {
            [Output(Guid = "{72BCF9DA-EE8C-41F1-B494-D9D883E305CB}")]
            public Slot<int> SumResult = new Slot<int>(0);

            public TestOperator()
            {
                SumResult.UpdateAction = UpdateResult;
            }

            public void UpdateResult(EvaluationContext context)
            {
                SumResult.Value = 0;
                int[] array = IntArray.GetValue(context);
                foreach (var entry in array)
                {
                    SumResult.Value += entry;
                }

                UpdateCallCount++;
            }

            public int UpdateCallCount = 0;

            [Input(Guid = "{2A2B8CDF-1034-4744-87F6-283AAF719379}")]
            public InputSlot<int[]> IntArray = new InputSlot<int[]>();
        }

        [TestMethod]
        public void TestSeveralGetValueCalls_WillCallUpdateOnlyOnce()
        {
            InputValueCreators.Entries.Add(typeof(int[]), () => new InputValue<int[]>(new[] { 1, 2, 3, 4, 5, 6 }));
            var symbol = new Symbol(typeof(TestOperator), Guid.NewGuid());
            var compositionSymbol = new Symbol(typeof(CompositionOperator), Guid.NewGuid());
            compositionSymbol.AddChild(symbol, Guid.NewGuid());
            var compositionInstance = compositionSymbol.CreateInstance(Guid.NewGuid());
            var op = (TestOperator)compositionInstance.Children[0];

            Assert.AreEqual(0, op.UpdateCallCount);
            var result = op.SumResult.GetValue(new EvaluationContext());
            Assert.AreEqual(21, result);
            Assert.AreEqual(1, op.UpdateCallCount);

            result = op.SumResult.GetValue(new EvaluationContext());
            Assert.AreEqual(21, result);
            Assert.AreEqual(1, op.UpdateCallCount);

            result = op.SumResult.GetValue(new EvaluationContext());
            Assert.AreEqual(21, result);
            Assert.AreEqual(1, op.UpdateCallCount);
        }
    }
}