using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using T3.Core.Operator;

namespace OperatorTests
{
    [TestClass]
    public class OperatorConnectionTests
    {
        #region Test type definitions
        public class IntInputOp
        {
            public InputSlot<int> IntInput { get; } = new InputSlot<int>(17);
        }

        public class StringInputOp
        {
            public InputSlot<string> StringInput { get; } = new InputSlot<string>("aber hallo!");
        }

        public class MultiIntInputOp
        {
            public Slot<int> Result;

            public MultiIntInputOp()
            {
                Result = new Slot<int>(UpdateResult, 0);
            }

            public void UpdateResult(EvaluationContext context)
            {
                Result.Value = 0;
                foreach (var input in MyMultiInput)
                {
                    Result.Value += input.GetValue(context);
                }
            }

            // commands/ui would look for IList<T> implementers with generic type (T) of InputSlot<R>
            public List<InputSlot<int>> MyMultiInput { get; internal set; } = new List<InputSlot<int>>();
        }

        public class FloatOutputOp
        {
            public Slot<float> FloatOutput { get; } = new Slot<float>(104.0f);
        }

        public class IntOutputOp
        {
            public Slot<int> IntOutput { get; } = new Slot<int>(64);
        }

        public class AnotherMultiIntInputOp
        {
            public List<InputSlot<int>> CompoundMultiInput { get; internal set; } = new List<InputSlot<int>>();
        }

        public class Size2InputOp
        {
//             public Size2Slot Size { get; } = new Size2Slot(new InputValue<Size2>{Value = Size2(128,128)});
        }
        #endregion

        #region The tests
        [TestMethod]
        public void TestOperatorDifferentConnectionTypesFloatToInt()
        {
            var op = new IntInputOp();
            var op2 = new FloatOutputOp();
            op.IntInput.Input = new ConverterSlot<float, int>(op2.FloatOutput, f => (int)f);
            var result = op.IntInput.GetValue(new EvaluationContext());
            Assert.AreEqual(104, result);
        }

        [TestMethod]
        public void TestOperatorDifferentConnectionTypesFloatToString()
        {
            var op = new StringInputOp();
            var op2 = new FloatOutputOp();
            op.StringInput.Input = new ConverterSlot<float, string>(op2.FloatOutput, f => f.ToString());
            var result = op.StringInput.GetValue(new EvaluationContext());
            Assert.AreEqual("104", result);
        }

        [TestMethod]
        public void TestOperatorSubValueConnection()
        {
            var op = new Size2InputOp();
            var op2 = new IntOutputOp();
//             var result = op.Size.Value;
//             Assert.AreEqual(new Size2(128, 128), result);
//             result = op.Size.GetValue(new EvaluationContext());
//             Assert.AreEqual(new Size2(128, 128), result);
// 
//             op.Size.Height.Input = op2.IntOutput;
//             op.Size.IsDirty = true;
//             result = op.Size.GetValue(new EvaluationContext());
//             Assert.AreEqual(new Size2(128, 64), result);
        }

        [TestMethod]
        public void TestOperatorMultiInputConnection()
        {
            var op = new MultiIntInputOp();
            var op2 = new IntOutputOp();
            var result = op.Result.Value;
            Assert.AreEqual(0, result);

            op.MyMultiInput.Add(new InputSlot<int>(0));
            op.MyMultiInput[0].Input = op2.IntOutput;
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(64, result);

            op.MyMultiInput.Add(new InputSlot<int>(0));
            op.MyMultiInput[1].Input = op2.IntOutput;
            op.Result.IsDirty = true;
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(128, result);
        }

        [TestMethod]
        public void TestOperatorMultiInputToMultiInputConnection()
        {
            // test scenario: a compound op with multi input connects to a child op with multi input 
            var op = new MultiIntInputOp();
            var op2 = new AnotherMultiIntInputOp();
            var result = op.Result.Value;
            Assert.AreEqual(0, result);

            op.MyMultiInput = op2.CompoundMultiInput;
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(0, result);

            // add inputs to compound input
            op2.CompoundMultiInput.Add(new InputSlot<int>(10));
            op2.CompoundMultiInput.Add(new InputSlot<int>(30));
            op2.CompoundMultiInput.Add(new InputSlot<int>(60));
            op.Result.IsDirty = true;
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(100, result);
        }
        #endregion
    }

}
