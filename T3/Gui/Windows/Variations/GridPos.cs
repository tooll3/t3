using System.Collections.Generic;
using SharpDX;

namespace T3.Gui.Windows.Variations
{
    public struct GridPos
    {
        public int X;
        public int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static GridPos operator +(GridPos a, GridPos b)
        {
            return new GridPos(a.X + b.X, a.Y + b.Y);
        }

        public static GridPos operator +(GridPos a, Size2 b)
        {
            return new GridPos(a.X + b.Width, a.Y + b.Height);
        }

        public bool IsWithinGrid()
        {
            return X > 0 && X < VariationGridSize && Y > 0 && Y < VariationGridSize;
        }
            
        public static GridPos[] BuildSortedOffsets()
        {
            var offsets = new List<GridPos>();
            for (var x = -VariationGridSize; x < VariationGridSize; x++)
            {
                for (var y = -VariationGridSize; y < VariationGridSize; y++)
                {
                    offsets.Add(new GridPos(x, y));
                }
            }

            offsets.Sort((a, b) => (a.X * a.X + a.Y * a.Y)
                                   - (b.X * b.X + b.Y * b.Y));

            return offsets.ToArray();
        }


        public int GridIndex => Y * VariationGridSize + X;
        public static GridPos Center = new GridPos(VariationGridSize / 2, VariationGridSize / 2);
        public const int VariationGridSize = 100;
    }
}