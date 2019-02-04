using System.Numerics;

namespace t3.graph
{
    class Node
    {
        public int ID;
        public string Name;

        public Vector2 Pos;
        public Vector2 Size = new Vector2(120, 20);
        public float Value;
        public Vector4 Color;
        public int InputsCount;
        public int OutputsCount;
        public bool IsSelected;

        public Node() { }
        public Node(int id, string name, Vector2 pos, float value, Vector4 color, int inputs_count, int outputs_count)
        {
            ID = id;
            Name = name;
            Pos = pos;
            Value = value;
            Color = color;
            InputsCount = inputs_count;
            OutputsCount = outputs_count;
            Size = new Vector2(100, 20);
        }

        public Vector2 GetInputSlotPos(int slot_no)
        {
            return new Vector2(Pos.X, Pos.Y + Size.Y * ((float)slot_no + 1) / ((float)InputsCount + 1));
        }
        public Vector2 GetOutputSlotPos(int slot_no)
        {
            return new Vector2(Pos.X + Size.X, Pos.Y + Size.Y * ((float)slot_no + 1) / ((float)OutputsCount + 1));
        }
    }
}