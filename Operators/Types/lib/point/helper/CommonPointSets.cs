using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
//using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_353f63fc_e613_43ca_b037_02d7b9f4e935
{
    public class CommonPointSets : Instance<CommonPointSets>
    {
        [Output(Guid = "2e45df97-e5c9-454d-b6ea-569c16cc04d5")]
        public readonly Slot<StructuredList> CpuBuffer = new();

        [Output(Guid = "E5DC2CD0-C57F-4E72-9452-E162FE1C37D5")]
        public readonly Slot<BufferWithViews> GpuBuffer = new();
        
        
        public CommonPointSets()
        {
            if (!_initialized)
                Init();
            
            CpuBuffer.UpdateAction = Update;
            GpuBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var bufferIndex = (int)Set.GetEnumValue<Shapes>(context);
            CpuBuffer.Value = _cpuPointBuffers[bufferIndex];
            GpuBuffer.Value = _gpuBuffersWithViews[bufferIndex];
        }

        private static void Init()
        {
            var bufferCount = Enum.GetNames(typeof(Shapes)).Length;

            _cpuPointBuffers = new StructuredList<Point>[bufferCount];
            _gpuBuffers = new Buffer[bufferCount];
            _gpuBuffersWithViews = new BufferWithViews[bufferCount];
            
            // Setup CPU Buffers
            for (var bufferIndex = 0; bufferIndex < bufferCount; bufferIndex++)
            {
                var definitionPoints = Definitions[bufferIndex];
                var tmpBuffer = new StructuredList<Point>(definitionPoints.Length);
                
                for (var pointIndex = 0; pointIndex < definitionPoints.Length; pointIndex++)
                {
                    var p = definitionPoints[pointIndex];
                    tmpBuffer.TypedElements[pointIndex] = p;
                }

                _cpuPointBuffers[bufferIndex] = tmpBuffer;
            }
            
            // Initialize GPU Buffers
            try
            {
                for (var bufferIndex = 0; bufferIndex < bufferCount; bufferIndex++)
                {
                    _gpuBuffersWithViews[bufferIndex] = new BufferWithViews();
                    
                    const int strideOfPointStructure = 2 * 16;
                    
                    var pointBuffer = _cpuPointBuffers[bufferIndex];
                    
                    ResourceManager.SetupStructuredBuffer(pointBuffer.TypedElements, 
                                                          strideOfPointStructure * pointBuffer.NumElements, 
                                                          strideOfPointStructure, 
                                                          ref _gpuBuffers[bufferIndex]);
                    
                    
                    ResourceManager.CreateStructuredBufferSrv(_gpuBuffers[bufferIndex], ref _gpuBuffersWithViews[bufferIndex].Srv);
                    ResourceManager.CreateStructuredBufferUav(_gpuBuffers[bufferIndex], UnorderedAccessViewBufferFlags.None, ref _gpuBuffersWithViews[bufferIndex].Uav);
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to setup buffer:" + e);
            }

            _initialized = true;
        }
        
        private static Buffer[] _gpuBuffers;
        private static BufferWithViews[] _gpuBuffersWithViews;

        [Input(Guid = "2BA96AEE-FF89-41BD-90C5-C6C36907B6E4", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> Set = new();

        private static StructuredList<Point>[] _cpuPointBuffers;

        //private static StructuredList<Point> _newBuffer;
        private static bool _initialized;

        private const float S = 0.5f;

        private static readonly Point[] CrossPoints =
            {
                new() { Position = new Vector3(0, -S, 0), W = 1 },
                new() { Position = new Vector3(0, S, 0), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, 0, 0), W = 1 },
                new() { Position = new Vector3(S, 0, 0), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(0, 0, -S), W = 1 },
                new() { Position = new Vector3(0, 0, S), W = 1 },
                new() { W = float.NaN },
            };

        private static readonly Point[] CrossXYPoints =
            {
                new() { Position = new Vector3(0, -S, 0), W = 1 },
                new() { Position = new Vector3(0, S, 0), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(-S, 0, 0), W = 1 },
                new() { Position = new Vector3(S, 0, 0), W = 1 },
                new() { W = float.NaN },
            };

        private static readonly Point[] CubePoints =
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

        private static readonly Point[] QuadPoints =
            {
                new() { Position = new Vector3(-S, -S, 0), W = 1 },
                new() { Position = new Vector3(+S, -S, 0), W = 1 },
                new() { Position = new Vector3(+S, +S, 0), W = 1 },
                new() { Position = new Vector3(-S, +S, 0), W = 1 },
                new() { Position = new Vector3(-S, -S, 0), W = 1 },
                new() { W = float.NaN },
            };
        
        private static readonly Point[] ArrowXPoints =
            {
                new() { Position = new Vector3(-S, 0, 0), W = 1 },
                new() { Position = new Vector3(+S, 0, 0), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S/1.5f, -S/4, 0), W = 1 },
                new() { Position = new Vector3(+S, 0, 0), W = 1 },
                new() { Position = new Vector3(S/1.5f, S/4, 0), W = 1 },
                new() { W = float.NaN },
            };       
        private static readonly Point[] ArrowYPoints =
            {
                new() { Position = new Vector3(-S, 0, 0), W = 1 },
                new() { Position = new Vector3(+S, 0, 0), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(S/1.5f, -S/4, 0), W = 1 },
                new() { Position = new Vector3(+S, 0, 0), W = 1 },
                new() { Position = new Vector3(S/1.5f, S/4, 0), W = 1 },
                new() { W = float.NaN },
            };   

        private static readonly Point[] ArrowZPoints =
            {
                new() { Position = new Vector3(0, 0, -S), W = 1 },
                new() { Position = new Vector3(0, 0, +S), W = 1 },
                new() { W = float.NaN },
                new() { Position = new Vector3(0, -S/4, S/1.5f), W = 1 },
                new() { Position = new Vector3(0, 0, +S), W = 1 },
                new() { Position = new Vector3(0, S/4, S/1.5f), W = 1 },
                new() { W = float.NaN },
            };   

        
        private enum Shapes
        {
            Cross,
            CrossXY,
            Cube,
            Quad,
            ArrowX,
            ArrowY,
            ArrowZ,
        }

        private static readonly List<Point[]> Definitions = new()
                                                                {
                                                                    CrossPoints,
                                                                    CrossXYPoints,
                                                                    CubePoints,
                                                                    QuadPoints,
                                                                    ArrowXPoints,
                                                                    ArrowYPoints,
                                                                    ArrowZPoints,
                                                                };
    }
}