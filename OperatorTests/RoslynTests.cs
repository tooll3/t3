using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace OperatorTests
{
    [TestClass]
    public class RoslynTests
    {
        [TestMethod]
        public void TestTrivialScript()
        {
            int result = CSharpScript.EvaluateAsync<int>("1 + 2").Result;
            Assert.AreEqual(3, result);
        }

        public class Globals
        {
            public int X;
            public int Y;
        }

        [TestMethod]
        public void TestScriptDelegateWithVariables()
        {
            var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
            ScriptRunner<int> runner = script.CreateDelegate();
            var globals = new Globals();
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                globals.X = i;
                globals.Y = i;
                sum += runner(globals).Result;
            }
            Console.WriteLine($"sum: {sum}");
        }


        [TestMethod]
        public void TestScriptDelegateWithVariablesComparison()
        {
            int LocalFunc(Globals g) { return g.X*g.Y;}

            var globals = new Globals();
            int sum = 0;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                globals.X = i;
                globals.Y = i;
                sum += LocalFunc(globals);
            }
            watch.Stop();
            Console.WriteLine($"sum: {sum}");
            Console.WriteLine($"calling took: {(double)watch.ElapsedTicks / Stopwatch.Frequency / 100.0}s");
            Console.WriteLine($"calling took: {(double)watch.ElapsedMilliseconds / 100.0}ms");
        }

    }
}
