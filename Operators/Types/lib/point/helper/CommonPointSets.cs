using System;
using System.Linq;
using System.Numerics;
//using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataStructures.Point;

namespace T3.Operators.Types.Id_353f63fc_e613_43ca_b037_02d7b9f4e935
{
    public class CommonPointSets : Instance<CommonPointSets>
    {
        [Output(Guid = "2e45df97-e5c9-454d-b6ea-569c16cc04d5")]
        public readonly Slot<StructuredList> Result = new();

        public CommonPointSets()
        {
            Result.UpdateAction = Update;
            if (!_initialized)
                Init();
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = _cubePointBuffer;
        }

        private static void Init()
        {
            _cubePointBuffer = new(CubePoints.Length);
            for (var index = 0; index < CubePoints.Length; index++)
            {
                var p = CubePoints[index];
                _cubePointBuffer[index] = p;
            }

            _initialized = true;
        }

        [Input(Guid = "2BA96AEE-FF89-41BD-90C5-C6C36907B6E4", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> Set = new();

        private enum Shapes
        {
            Cube
        }
        
        
        private static StructuredList<Point> _cubePointBuffer;
        private static bool _initialized;
        
        private const float S = 0.5f;

        private static Point[] CubePoints =
            {
                new() { Position = new Vector3(-S, -S, S), W = 1 },
                new() { Position = new Vector3(S, -S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, S, S), W = 1 },
                new() { Position = new Vector3(S, S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, -S, -S), W = 1 },
                new() { Position = new Vector3(S, -S, -S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, S, -S), W = 1 },
                new() { Position = new Vector3(S, S, -S), W = 1 },
                new() { W = float.NaN },

                new() { Position = new Vector3(-S, -S, S), W = 1 },
                new() { Position = new Vector3(-S, S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S, -S, S), W = 1 },
                new() { Position = new Vector3(S, S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, -S, -S), W = 1 },
                new() { Position = new Vector3(-S, S, -S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S, -S, -S), W = 1 },
                new() { Position = new Vector3(S, S, -S), W = 1 },
                new() { W = float.NaN },

                new() { Position = new Vector3(-S, -S, -S), W = 1 },
                new() { Position = new Vector3(-S, -S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S, -S, -S), W = 1 },
                new() { Position = new Vector3(S, -S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, S, -S), W = 1 },
                new() { Position = new Vector3(-S, S, S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S, S, -S), W = 1 },
                new() { Position = new Vector3(S, S, S), W = 1 },
                new() { W = float.NaN },
            };
    }
}