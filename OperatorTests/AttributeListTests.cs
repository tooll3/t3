using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using T3.Core.Operator;

namespace OperatorTests
{
    [TestClass]
    public class AttributeListTests
    {
        // Problem war das jeder Zwischenoperator der der Attributliste was hinzufuegt einen
        // neuen Typ ausgeben muss -> super nervig, weil das ziemlich viele Attributlist Typen erzeugt
        public class Try1
        {
            public struct TestAttribute
            {
                public float Time;

                public TestAttribute(float time)
                {
                    Time = 0.0f;
                }
            }

            public class AttributeListGeneratorOperator
            {
                public Slot<TestAttribute[]> Output;

                public AttributeListGeneratorOperator()
                {
                    Output = new Slot<TestAttribute[]>(Update) {Value = new TestAttribute[0]};
                }

                void Update(EvaluationContext context)
                {
                    int size = Size.GetValue(context);
                    if (size != Output.Value.Length)
                    {
                        Array.Resize(ref Output.Value, size);
                        for (int i = 0; i < size; size++)
                        {
                            Output.Value[i] = new TestAttribute(0.0f);
                        }
                    }
                }

                public InputSlot<int> Size = new InputSlot<int>(3);
            }

            public struct TestAttribute2
            {
                public float Time;
                public Vector3 Position;

                public TestAttribute2(float time, Vector3 position)
                {
                    Time = time;
                    Position = position;
                }
            }

            public class AddAttributeOperator
            {
                public Slot<TestAttribute2[]> Output;

                public AddAttributeOperator()
                {
                    Output = new Slot<TestAttribute2[]>(Update) { Value = new TestAttribute2[0] };
                }

                void Update(EvaluationContext context)
                {
                    var inputArray = InputArray.GetValue(context);
                    int inputLength = inputArray.Length;
                    if (inputLength != Output.Value.Length)
                    {
                        Array.Resize(ref Output.Value, inputLength);
                        for (int i = 0; i < inputLength; i++)
                        {
                            Output.Value[i] = new TestAttribute2(inputArray[i].Time, Vector3.Zero);
                        }
                    }
                }

                public InputSlot<TestAttribute[]> InputArray = new InputSlot<TestAttribute[]>(new TestAttribute[0]);
            }
        } // Try1


        // templatesierbare Klassen zum Erzeugen und Setzen von Attributewerten, erzeugen geht, das generische
        // Setzen hatte aber das Problem, dass man nich einfach ueber die unterschiedlichen Spalten iterieren
        // konnte, weil die Daten zusammen in einem Struct gehalten wurden
        class Try2
        {
            public struct Ring
            {
                //public float Thickness;
                //public float Radius;
                //public int Count;
            }

            public enum RingAttributes
            {
                Thickness = 0,
                Radius = 1,
                Count = 2,
            }

            public class AttributeListGeneratorOperator2<TStruct, TEnum>  where TStruct : struct
                                                                          where TEnum : Enum
            {
                public Slot<AttributeList<TStruct, TEnum>> Output;

                public AttributeListGeneratorOperator2()
                {
                    Output = new Slot<AttributeList<TStruct, TEnum>>(UpdateAttributes) { Value = new AttributeList<TStruct, TEnum>()};
                }

                public void UpdateAttributes(EvaluationContext context)
                {
                    int size = Count.GetValue(context);
                    if (size != Output.Value.AttributeEntries.Length)
                    {
                        Output.Value.AttributeEntries = new TStruct[size];
                    }
                }

                public InputSlot<int> Count = new InputSlot<int>(1);
            }

            public class AttributeList<TStruct, TEnum> where TStruct : struct
                                                       where TEnum : Enum
            {
                public AttributeList()
                {
                    EnumType = typeof(TEnum);
                    AttributeNames = Enum.GetNames(EnumType);
                }

                public Type EnumType;
                public string[] AttributeNames;
                public TStruct[] AttributeEntries;
            }

            public class SetAttributeOperator2<TStruct, TEnum, TAttribute> where TStruct : struct
                                                                           where TEnum : Enum
                                                                           where TAttribute : struct
            {
                public Slot<AttributeList<TStruct, TEnum>> Output;

                public SetAttributeOperator2()
                {
                    Output = new Slot<AttributeList<TStruct, TEnum>>(Update) { Value = new AttributeList<TStruct, TEnum>() };
                }

                public void Update(EvaluationContext context)
                {
                    var inputList = Input.GetValue(context);
                    int inputSize = inputList.AttributeEntries.Length;
                    if (inputSize != Output.Value.AttributeEntries.Length)
                    {
                        Output.Value.AttributeEntries = new TStruct[inputSize];
                    }

                    TStruct[] inputData = Input.Value.AttributeEntries;
                    TStruct[] outputData = Output.Value.AttributeEntries;
                    for (int i = 0; i < inputSize; i++)
                    {
                        outputData[i] = inputData[i];
                    }
                }

                public InputSlot<AttributeList<TStruct, TEnum>> Input = new InputSlot<AttributeList<TStruct, TEnum>>(new AttributeList<TStruct, TEnum>());
                public InputSlot<TEnum> Attribute = new InputSlot<TEnum>(default(TEnum));
                public InputSlot<TAttribute[]> AttributeValues = new InputSlot<TAttribute[]>(new TAttribute[0]);
            }
        } // Try2


        public interface IAttributeColumn
        {
            string Name { get; }
            IList ListData { get; }
            Type Type { get; }
            void Resize(int newRowCount);
            void SetValues(object values);
            IAttributeColumn Clone();
        }

        public class AttributeColumn<T> : IAttributeColumn
        {
            public AttributeColumn(string name, int count, T defaultValue)
            {
                Type = typeof(T);
                Name = name;
                DefaultValue = defaultValue;
                Resize(count);
            }

            public void Resize(int newRowCount)
            {
                int actualCount = Data.Count;
                if (newRowCount > Data.Capacity)
                    Data.Capacity = newRowCount;
                if (newRowCount > Data.Capacity)
                    Data.Capacity = newRowCount;
                for (int i = 0; i < actualCount; i++)
                    Data[i] = DefaultValue;
                for (int i = actualCount; i < newRowCount; i++)
                    Data.Add(DefaultValue);
            }

            public void SetValues(object values)
            {
                T[] newValues = (T[])values;
                int length = Math.Min(Data.Count, newValues.Length);
                for (int i = 0; i < length; i++)
                {
                    Data[i] = newValues[i];
                }
            }

            public IAttributeColumn Clone()
            {
                return new AttributeColumn<T>(Name, Data.Count, DefaultValue);
            }

            public List<T> Data = new List<T>(10);
            public IList ListData => Data;
            public string Name { get; }
            public Type Type { get; }
            public T DefaultValue;
        }

        public class AttributeList
        {
            public int RowCount => Columns[0].ListData.Count;
            public List<IAttributeColumn> Columns { get; }

            public AttributeList(int count, List<IAttributeColumn> columns)
            {
                Columns = columns;
                Resize(count);
            }

            public AttributeList Clone()
            {
                var clonedColumns = new List<IAttributeColumn>(Columns.Count);
                foreach (var column in Columns)
                {
                    clonedColumns.Add(column.Clone());
                }

                return new AttributeList(RowCount, clonedColumns);
            }

            public void Resize(int newRowCount)
            {
                foreach (var column in Columns)
                {
                    column.Resize(newRowCount);
                }
            }

            public void SetColumnValues(int columnIdx, object values)
            {
                Columns[columnIdx].SetValues(values);
            }
        }

        public class AttributeListGenerator
        {
            public Slot<AttributeList> Output;

            public AttributeListGenerator()
            {
                Output = new Slot<AttributeList>(UpdateAttributes);
            }

            public void UpdateAttributes(EvaluationContext context)
            {
                int count = Count.GetValue(context);
                Output.Value = new AttributeList(count, Columns.GetValue(context));
            }

            public InputSlot<int> Count = new InputSlot<int>(0);
             public InputSlot<List<IAttributeColumn>> Columns;// = new InputSlot<List<IAttributeColumn>>(null);
        }

        public class SetAttributeOperator
        {
            public Slot<AttributeList> Output;

            public SetAttributeOperator()
            {
                Output = new Slot<AttributeList>(Update);
            }

            public void Update(EvaluationContext context)
            {
                bool listHasChanged = AttributeListInput.IsDirty;
                AttributeList inputAttributeList = AttributeListInput.GetValue(context);
                if (inputAttributeList == null)
                    return;

                if (listHasChanged || AttributeName.IsDirty)
                {
                    // update index
                    string name = AttributeName.GetValue(context);
                    var columns = inputAttributeList.Columns;
                    _attributeIndex = -1;
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (columns[i].Name == name)
                        {
                            _attributeIndex = i;
                            break;
                        }
                    }

                    if (_attributeIndex == -1)
                    {
                        Console.WriteLine($"SetAttributeOperator::Update: no matching attribute '{name}' found in attribute list.");
                        return;
                    }
                }

                if (listHasChanged)
                {
                    Output.Value = inputAttributeList.Clone(); // copy the whole attribute list
                }

                var setter = AttributeValueSetter.GetValue(context);
                setter?.Invoke(Output.Value.Columns[_attributeIndex]);
            }

            public InputSlot<AttributeList> AttributeListInput;// = new InputSlot<AttributeList>(null);
            public InputSlot<string> AttributeName = new InputSlot<string>(string.Empty);
            private int _attributeIndex = -1;
            public InputSlot<Action<IAttributeColumn>> AttributeValueSetter;// = new InputSlot<Action<IAttributeColumn>>(default);
        }

        public class AttributeRandomColumn
        {
            public Slot<Action<IAttributeColumn>> Output;

            public AttributeRandomColumn()
            {
                Output = new Slot<Action<IAttributeColumn>>(FillColumnWithRandomValues);
            }

            public void FillColumnWithRandomValues(IAttributeColumn column)
            {
                var random = new Random(Seed.GetValue(null));
                AttributeColumn<float> floatColumn = (AttributeColumn<float>)column;
                List<float> data = floatColumn.Data;
                float max = Max.GetValue(null);
                float min = Min.GetValue(null);
                float range = Math.Abs(max - min);
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = (float)random.NextDouble() * range - min;
                }
            }

            public InputSlot<int> Seed = new InputSlot<int>(0);
            public InputSlot<float> Min = new InputSlot<float>(0.0f);
            public InputSlot<float> Max = new InputSlot<float>(1.0f);
        }

        public abstract class AddAttributeOperator
        {
            public Slot<AttributeList> Output;

            protected AddAttributeOperator() => Output = new Slot<AttributeList>(Update);

            public abstract void Update(EvaluationContext context);

            public InputSlot<AttributeList> AttributeListInput;//= new InputSlot<AttributeList>(null);
            public InputSlot<string> AttributeName = new InputSlot<string>("<new attribute>");
            //private int _attributeIndex = -1;
            public InputSlot<Action<AttributeList>> AttributeValueSetter;// = new InputSlot<Action<AttributeList>>(default);
        }

        public class AccumulateAndNormalize : AddAttributeOperator
        {
            public override void Update(EvaluationContext context)
            {

            }
        }


        [TestMethod]
        public void Test2()
        {
            var randomAttributeSetter = new AttributeRandomColumn();
            var genAttrOp = new AttributeListGenerator();
            var setAttrOp = new SetAttributeOperator();

            genAttrOp.Count.Value = 3;
            genAttrOp.Columns.Value = new List<IAttributeColumn>(5)
                                      {
                                          new AttributeColumn<float>("Thickness", 3, 5.0f),
                                          new AttributeColumn<float>("Radius", 3, 10.0f),
                                          new AttributeColumn<int>("Count", 3, 7)
                                      };

            setAttrOp.AttributeName.Value = "Thickness";
            setAttrOp.AttributeValueSetter.InputConnection = randomAttributeSetter.Output;
            setAttrOp.AttributeListInput.InputConnection = genAttrOp.Output;

            EvaluationContext context = new EvaluationContext();
            var value = setAttrOp.Output.GetValue(context);
        }

    }
}
