using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tooll.Core.PullVariant;

namespace OperatorTests
{
    [TestClass]
    public class OperatorInputFunctionTests
    {
        public class InputFunctionOperator
        {
            public Slot<float[]> Output;

            public InputFunctionOperator()
            {
                Output = new Slot<float[]>(Update, new float[5]);
            }

            public void Update(EvaluationContext context)
            {
                Func<float, float> f = InputFunction.GetValue(context);
                for (int i = 0; i < Output.Value.Length; i++)
                {
                    Output.Value[i] = f(i);
                }
            }

            public InputSlot<Func<float, float>> InputFunction = new InputSlot<Func<float, float>>(f => 2.0f*f);
        }

        public class OutputFunctionOperator
        {
            public Slot<Func<float, float>> OutputFunction = new Slot<Func<float, float>>(f => f*f);
        }

        public class OutputFunctionTemplateOperator
        {
            public Slot<Func<float, float>> OutputFunction;

            OutputFunctionTemplateOperator(Func<float, float> function)
            {
                OutputFunction = new Slot<Func<float, float>>(function);
            }
        }

        [TestMethod]
        public void TestInputFunctionOperator()
        {
            InputFunctionOperator opWithInputFunction = new InputFunctionOperator();
            EvaluationContext context = new EvaluationContext();

            float[] result = opWithInputFunction.Output.GetValue(context);
            CollectionAssert.AreEqual(new[] {0.0f, 2.0f, 4.0f, 6.0f, 8.0f}, result);
        }

        [TestMethod]
        public void TestConnectedInputFunctionOperator()
        {
            InputFunctionOperator opWithInputFunction = new InputFunctionOperator();
            OutputFunctionOperator opWithOutputFunction = new OutputFunctionOperator();
            opWithInputFunction.InputFunction.Input = opWithOutputFunction.OutputFunction;
            EvaluationContext context = new EvaluationContext();

            float[] result = opWithInputFunction.Output.GetValue(context);
            CollectionAssert.AreEqual(new[] {0.0f, 1.0f, 4.0f, 9.0f, 16.0f}, result);
        }
    }
}