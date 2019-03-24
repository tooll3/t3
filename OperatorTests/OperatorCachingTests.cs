using Microsoft.VisualStudio.TestTools.UnitTesting;
using T3.Core.Operator;

namespace OperatorTests
{
    [TestClass]
    public class OperatorCachingTests
    {
        public class TestOperator
        {
            public Slot<int> SumResult { get; }

            public TestOperator()
            {
                SumResult = new Slot<int>(UpdateResult) { Value = 0 };
            }

            public void UpdateResult(EvaluationContext context)
            {
                SumResult.Value = 0;
                int[] array = IntArray.GetValue(new EvaluationContext());
                foreach (var entry in array)
                {
                    SumResult.Value += entry;
                }

                UpdateCallCount++;
            }

            public int UpdateCallCount = 0;

            public InputSlot<int[]> IntArray { get; } = new InputSlot<int[]>(new[] { 1, 2, 3, 4, 5, 6 });
        }

        [TestMethod]
        public void TestSeveralGetValueCalls_WillCallUpdateOnlyOnce()
        {
            var op = new TestOperator();
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
