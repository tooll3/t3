using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace T3.Core.DataTypes
{
    public abstract class StructuredList
    {
        public StructuredList(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
        public abstract object Elements { get; }
    }

    // public struct Test
    // {
    //     public Vector3 Position;
    //     public float Age;
    // }
    //
    // public class TestStructuredList : StructuredList
    // {
    //     public TestStructuredList(int count) : base(typeof(Test))
    //     {
    //         TypedElements = new Test[count];
    //     }
    //
    //     public Test[] TypedElements { get; }
    //     public override object Elements => TypedElements;
    // }
    //
    // public class XXXX
    // {
    //     public void TestAbstractArrayCast()
    //     {
    //         StructuredList abstractType = new TestStructuredList(10);
    //         //Assert.IsTrue(abstractType.Elements is Test[]);
    //     }
    //
    //     public void TestAbstractArrayCastToObject()
    //     {
    //         StructuredList abstractType = new TestStructuredList(10);
    //         //Assert.IsFalse(abstractType.Elements is object[]);
    //     }
    //
    //     public void StructuredListTest01()
    //     {
    //         var tsl = new TestStructuredList(10);
    //         foreach (var entry in tsl.TypedElements)
    //         {
    //             Console.WriteLine($"{entry}");
    //         }
    //     }
    // }

    // public class StructuredList
    // {
    //     public Type Type;
    //     public List<object> Entries;
    // }
    //
    //
    // public class Test
    // {
    //     public void Update()
    //     {
    //         var sl = new StructuredList();
    //
    //         var list = new List<object>
    //                        {
    //                            new TestFormat() { Value = 2, Name = "test" },
    //                            new TestFormat() { Value = 2, Name = "test" },
    //                        };
    //
    //         sl.Entries = list;
    //         sl.Type = typeof(TestFormat);
    //
    //         sl.Entries.OfType(sl.Type);
    //         //var newList = new List<sl.Type>();
    //         //var newList = sl.Entries.ConvertAll(o => (typeof(sl.Type))o);
    //     }
    //
    //     public struct TestFormat
    //     {
    //         public float Value;
    //         public string Name;
    //     }
    // }
}