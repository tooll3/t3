using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using T3.Core.Operator;

namespace OperatorTests
{
    [TestClass]
    public class TestOperatorEvaluation
    {
        //public class TestOperator
        //{
        //    //
        //    public Slot<float> Result { get; }

        //    public TestOperator()
        //    {
        //        Result = new Slot<float>(Update) { Value = 17.0f };
        //    }

        //    public void Update(EvaluationContext context)
        //    {
        //        // Result.Value = Count.GetValue(context) * (Bla.GetValue(context) == "Hallo" ? 1 : 2)*IntArray.GetValue(context).Sum();
        //        // or
        //        Count.Update(context);
        //        Bla.Update(context);
        //        IntArray.Update(context);
        //        Result.Value = Count.Value * (Bla.Value == "Hallo" ? 1 : 2) * IntArray.Value.Sum();
        //    }

        //    //[Operator.Input]
        //    [Operator(typeof(InputSlot<int>))]
        //    public InputSlot<int> Count { get; } = new InputSlot<int>(1);

        //    //[Operator.Input]
        //    public InputSlot<string> Bla { get; } = new InputSlot<string>("hahaha");

        //    [Operator(typeof(InputSlot<int[]>))]
        //    // name, id, relevance, description, multi input, default value,  
        //    // min value, max value, scale, scale type, 
        //    public InputSlot<int[]> IntArray { get; } = new InputSlot<int[]>(new[] { 1, 2, 3, 4, 5, 6 });
        //}

        public class TestOperator
        {
            public Slot<float> Result { get; }

            public TestOperator()
            {
                Result = new Slot<float>(Update) { Value = 17.0f };
            }

            public void Update(EvaluationContext context)
            {
                Result.Value = Count.GetValue(context)*(Bla.GetValue(context) == "Hallo" ? 1 : 2)*IntArray.GetValue(context).Sum();
            }

            public InputSlot<int> Count { get; } = new InputSlot<int>(1);
            public InputSlot<string> Bla { get; } = new InputSlot<string>("hahaha");
            public InputSlot<int[]> IntArray { get; } = new InputSlot<int[]>(new[] { 1, 2, 3, 4, 5, 6 });
        }

        public class TestArrayOutputOperator
        {
            public Slot<int[]> ArrayResult { get; }

            public TestArrayOutputOperator()
            {
                ArrayResult = new Slot<int[]>(Update, new[] { 2, 4, 6, 8, 10 });
            }

            public void Update(EvaluationContext context)
            {
                int[] array1 = Input1.GetValue(context);
                int[] array2 = Input2.GetValue(context);
                int resultLength = Math.Min(array1.Length, array2.Length);
                if (resultLength != ArrayResult.Value.Length)
                    ArrayResult.Value = new int[resultLength];
                for (int i = 0; i < resultLength; i++)
                {
                    ArrayResult.Value[i] = array1[i] * array2[i];
                }
            }

            public InputSlot<int[]> Input1 { get; } = new InputSlot<int[]>(new[] { 1, 2, 3, 4, 5, 6 });
            public InputSlot<int[]> Input2 { get; } = new InputSlot<int[]>(new[] { 1, 2, 3, 4, 5, 6 });
        }

        [TestMethod]
        public void TestOperatorDefaultOutput()
        {
            var op = new TestOperator();
            Assert.AreEqual(17.0f, op.Result.Value);
        }

        [TestMethod]
        public void TestOperatorUpdatedOutput()
        {
            var op = new TestOperator();
            op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(42.0f, op.Result.Value);
        }

        [TestMethod]
        public void TestArrayOperatorDefaultOutput()
        {
            var arrayOp = new TestArrayOutputOperator();
            Assert.AreEqual(5, arrayOp.ArrayResult.Value.Length);
            CollectionAssert.AreEqual(new[] {2, 4, 6, 8, 10}, arrayOp.ArrayResult.Value);
        }

        [TestMethod]
        public void TestArrayOperatorEvaluation()
        {
            var arrayOp = new TestArrayOutputOperator();
            var result = arrayOp.ArrayResult.GetValue(new EvaluationContext());
            Assert.AreSame(result, arrayOp.ArrayResult.Value);
            Assert.AreEqual(6, result.Length);
            CollectionAssert.AreEqual(new[] {1, 4, 9, 16, 25, 36}, result);
        }

        [TestMethod]
        public void TestArrayOperatorInputChangeWithShorterArray()
        {
            var arrayOp = new TestArrayOutputOperator();
            arrayOp.Input2.Value = new[] {1, 1, 1, 1};
            var result = arrayOp.ArrayResult.GetValue(new EvaluationContext());
            Assert.AreEqual(4, result.Length);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result);
        }

        [TestMethod]
        public void TestOperatorConnectionWithArray()
        {
            var testOp = new TestOperator();
            var testArrayOp = new TestArrayOutputOperator();
            testOp.IntArray.Input = testArrayOp.ArrayResult;
            var result = testOp.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(182.0f, result);
        }

        [TestMethod]
        public void TestOperatorConnectionWithArrayAndChangedInput()
        {
            var testOp = new TestOperator();
            var testArrayOp = new TestArrayOutputOperator();
            testOp.IntArray.Input = testArrayOp.ArrayResult;
            testArrayOp.Input1.Value = new[] { 5, 6, 7, 8, 9, 10 };
            var result = testOp.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(350.0f, result);
        }

    }
}
