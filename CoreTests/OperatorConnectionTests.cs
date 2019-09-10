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
                foreach (var input in MyMultiInput.GetCollectedTypedInputs())
                {
                    Result.Value += input.GetValue(context);
                }
            }

            public MultiInputSlot<int> MyMultiInput { get; internal set; } = new MultiInputSlot<int>();
        }

        public class FloatOutputOp
        {
            public Slot<float> FloatOutput { get; } = new Slot<float>(104.0f);
        }

        public class IntOutputOp
        {
            public IntOutputOp(int value)
            {
                IntOutput.Value = value;
            }

            public Slot<int> IntOutput { get; } = new Slot<int>();
        }

        public class AnotherMultiIntInputOp
        {
            public MultiInputSlot<int> CompoundMultiInput { get; internal set; } = new MultiInputSlot<int>();
        }

        public class CompositionOpWith2MultiInputs
        {
            public MultiInputSlot<int> MultiInput1 { get; internal set; } = new MultiInputSlot<int>();
            public MultiInputSlot<int> MultiInput2 { get; internal set; } = new MultiInputSlot<int>();
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
            op.IntInput.AddConnection(new ConverterSlot<float, int>(op2.FloatOutput, f => (int)f));
            var result = op.IntInput.GetValue(new EvaluationContext());
            Assert.AreEqual(104, result);
        }

        [TestMethod]
        public void TestOperatorDifferentConnectionTypesFloatToString()
        {
            var op = new StringInputOp();
            var op2 = new FloatOutputOp();
            op.StringInput.AddConnection(new ConverterSlot<float, string>(op2.FloatOutput, f => f.ToString()));
            var result = op.StringInput.GetValue(new EvaluationContext());
            Assert.AreEqual("104", result);
        }

        [TestMethod]
        public void TestOperatorSubValueConnection()
        {
            var op = new Size2InputOp();
            var op2 = new IntOutputOp(64);
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
            var op2 = new IntOutputOp(64);
            var result = op.Result.Value;
            Assert.AreEqual(0, result);

            op.MyMultiInput.AddConnection(op2.IntOutput);
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(64, result);

            op.MyMultiInput.AddConnection(op2.IntOutput);
            op.Result.DirtyFlag.Invalidate();
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(128, result);
        }

        [TestMethod]
        public void TestOperatorMultiInputToMultiInputConnection()
        {
            // test scenario: a compound op with multi input connects to a child op with multi input 
            var op = new MultiIntInputOp();
            var compositionOp = new AnotherMultiIntInputOp();
            var intOutputOp = new IntOutputOp(64);
            var result = op.Result.Value;
            Assert.AreEqual(0, result);

            // Add one input to the composition op multi input
            compositionOp.CompoundMultiInput.AddConnection(intOutputOp.IntOutput);

            op.MyMultiInput.AddConnection(compositionOp.CompoundMultiInput);
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(64, result);

            // Add two more inputs to the composition op multi input
            compositionOp.CompoundMultiInput.AddConnection(intOutputOp.IntOutput);
            compositionOp.CompoundMultiInput.AddConnection(intOutputOp.IntOutput);
            result = op.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(192, result);
        }


        [TestMethod]
        public void TestOperatorMixedMultiInputAndNonMutiInputsConnectedToMultiInput()
        {
            // test scenario: 
            // [          AddWithMI         ]  <- op within composition op with multi input
            // [       MI1       ]  [  MI2  ]  <- composition op multi inputs
            // [O1] [O2] [O3] [O2]  [O3] [O4]  <- several outputs connected to multi inputs of composition op
            var add = new MultiIntInputOp();
            var compositionOp = new CompositionOpWith2MultiInputs();
            var out1 = new IntOutputOp(64);
            var out2 = new IntOutputOp(87);
            var out3 = new IntOutputOp(-15);
            var out4 = new IntOutputOp(123);
            var result = add.Result.Value;
            Assert.AreEqual(0, result);

            // Add the outputs to multi input 1 of composition op
            compositionOp.MultiInput1.AddConnection(out1.IntOutput);
            compositionOp.MultiInput1.AddConnection(out2.IntOutput);
            compositionOp.MultiInput1.AddConnection(out3.IntOutput);
            compositionOp.MultiInput1.AddConnection(out2.IntOutput);

            // Add the output to multi input 2 of composition op
            compositionOp.MultiInput2.AddConnection(out3.IntOutput);
            compositionOp.MultiInput2.AddConnection(out4.IntOutput);

            // connect composition op multi inputs to the op within the composition op with one multi input
            add.MyMultiInput.AddConnection(compositionOp.MultiInput1);
            add.MyMultiInput.AddConnection(compositionOp.MultiInput2);

            result = add.Result.GetValue(new EvaluationContext());
            Assert.AreEqual(64 + 87 - 15 + 87 - 15 + 123, result);
        }

        #endregion
    }

}
