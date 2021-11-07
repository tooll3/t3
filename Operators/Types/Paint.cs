using SharpDX;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

//using Quaternion = SharpDX.Quaternion;
//using Vector4 = SharpDX.Vector4;

namespace T3.Operators.Types.Id_b238b288_6e9b_4b91_bac9_3d7566416028
{
    public class Paint : Instance<Paint>
    {
        [Output(Guid = "09d2546e-b6d3-4e5c-884f-eabe8d51c38c")]
        public readonly Slot<StructuredList> PointList = new Slot<StructuredList>();

        public Paint()
        {
            PointList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (IsActve.GetValue(context))
            {
                _maxPointCount = MaxPointCount.GetValue(context);

                if (TriggerReset.GetValue(context))
                {
                    Reset();
                }

                var isMouseDown = IsMouseButtonDown.GetValue(context);
                if (!isMouseDown && _mouseWasDown)
                {
                    if (_currentStrokeLength == 1 && _writeIndex > 0) // add to points for single click to make it visible as a dot
                    {
                        var lastPoint = _pointList.TypedElements[_writeIndex - 1];
                        lastPoint.Position = PointFromMousePos(context, _lastMousePos + new Vector2(0,0.01f)); 
                        AppendPoint(lastPoint);
                    }
                    AppendPoint(Point.Separator());
                    _currentStrokeLength = 0;

                }
                else if (isMouseDown)
                {
                    var mousePos = MousePos.GetValue(context);
                    if (!_mouseWasDown || Vector2.Distance(_lastMousePos, mousePos) > 0.001f)
                    {
                        AppendPoint(new Point()
                                        {
                                            Position = PointFromMousePos(context, mousePos),
                                            Orientation = Quaternion.Identity,
                                            W = 1,
                                        });
                        AppendPoint(Point.Separator(), advanceIndex: false);
                        _currentStrokeLength++;
                    }
                    _lastMousePos = mousePos;
                }

                _mouseWasDown = isMouseDown;
            }

            PointList.Value = _pointList;
        }

        private Vector3 PointFromMousePos(EvaluationContext context, Vector2 mousePos)
        {
            const float offsetFromCamPlane = 0.98f; 
            var posInClipSpace = new SharpDX.Vector4((mousePos.X - 0.5f) * 2, (-mousePos.Y + 0.5f) * 2, offsetFromCamPlane, 1);
            Matrix clipSpaceToCamera = context.CameraToClipSpace;
            clipSpaceToCamera.Invert();
            Matrix cameraToWorld = context.WorldToCamera;
            cameraToWorld.Invert();
            Matrix worldToObject = context.ObjectToWorld;
            worldToObject.Invert();
                
            var clipSpaceToWorld = Matrix.Multiply(clipSpaceToCamera, cameraToWorld);
            var m = Matrix.Multiply(cameraToWorld, clipSpaceToCamera);
            m.Invert();
            var p = SharpDX.Vector4.Transform(posInClipSpace, clipSpaceToWorld);
            return new Vector3(p.X, p.Y, p.Z)/ p.W;
        }

        private void AppendPoint(Point p, bool advanceIndex = true)
        {
            if (_pointList.NumElements >= _maxPointCount)
            {
                Log.Warning($"Cannot append {p} because buffer len of {_maxPointCount} reached");
                return;
            }

            if (_writeIndex >= _pointList.NumElements - 1)
            {
                Log.Debug($"Increasing paint buffer length of {_pointList.NumElements} by {BufferIncreaseStep}...");
                _pointList.SetLength(_pointList.NumElements + BufferIncreaseStep);
            }

            if (advanceIndex)
            {
                Log.Debug($"Writing {p.Position} at # {_writeIndex}");
            }

            _pointList.TypedElements[_writeIndex] = p;

            if (advanceIndex)
                _writeIndex++;
        }

        private void Reset()
        {
            _pointList.SetLength(0);
            _pointList.SetLength(BufferIncreaseStep);
            _writeIndex = 0;
        }

        private bool _mouseWasDown;
        private Vector2 _lastMousePos;
        private int _writeIndex;
        private int _currentStrokeLength;

        // TODO: This should be an input so it can be serialized
        private readonly StructuredList<Point> _pointList = new StructuredList<Point>(BufferIncreaseStep);

        private const int BufferIncreaseStep = 1000;
        private int _maxPointCount = BufferIncreaseStep;

        [Input(Guid = "B81392E2-303E-43F4-985F-DB6E5F923304")]
        public readonly InputSlot<bool> IsActve = new InputSlot<bool>();

        [Input(Guid = "C427F009-7E04-4168-82E6-5EBE2640204D")]
        public readonly InputSlot<Vector2> MousePos = new InputSlot<Vector2>();

        [Input(Guid = "520A2023-7450-4314-9CAC-850D6D692461")]
        public readonly InputSlot<bool> IsMouseButtonDown = new InputSlot<bool>();

        [Input(Guid = "276071F1-0879-4208-8DEB-65EE44E4D1CF")]
        public readonly InputSlot<int> MaxPointCount = new InputSlot<int>(10000);

        [Input(Guid = "B0D76CAB-8E7E-4845-B7E4-202424DFA12D")]
        public readonly InputSlot<bool> TriggerReset = new InputSlot<bool>();
    }
}