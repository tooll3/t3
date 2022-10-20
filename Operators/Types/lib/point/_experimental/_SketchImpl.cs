using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

// ReSharper disable RedundantNameQualifier

namespace T3.Operators.Types.Id_b238b288_6e9b_4b91_bac9_3d7566416028
{
    public class _SketchImpl : Instance<_SketchImpl>
    {
        [Output(Guid = "EB2272B3-8B4A-46B1-A193-8B10BDC2B038")]
        public readonly Slot<object> OutPages = new();
        
        [Output(Guid = "974F46E5-B1DC-40AE-AC28-BBB1FB032EFE")]
        public readonly Slot<Vector3> CursorPosInWorld = new();

        [Output(Guid = "532B35D1-4FEE-41E6-AA6A-D42152DCE4A0")]
        public readonly Slot<float> CurrentBrushSize = new();


        public _SketchImpl()
        {
            OutPages.UpdateAction = Update;
            CursorPosInWorld.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {
            var filepath = FilePath.GetValue(context);
            if (filepath != _filepath)
            {
                _filepath = filepath;
                _pages = Core.Utilities.TryLoadingJson<Dictionary<int, Page>>(filepath); 
                
                if (_pages != null)
                {
                    foreach (var page in _pages.Values)
                    {
                        if (page.PointsList == null)
                        {
                            page.PointsList = new StructuredList<Point>(BufferIncreaseStep);
                            continue;
                        }
                        
                        if (page.PointsList.NumElements == page.WriteIndex)
                            continue;
                        
                        Log.Warning($"Adjusting writing index {page.WriteIndex} -> {page.PointsList.NumElements}");
                        page.WriteIndex = page.PointsList.NumElements;
                    }
                }
                else
                {
                    _pages = new Dictionary<int, Page>();
                }
            }

            _pageIndex = PageIndex.GetValue(context);

            // Switch Brush size
            {
                if (BrushSize.DirtyFlag.IsDirty)
                {
                    _brushSize = BrushSize.GetValue(context);
                }

                for (var index = 0; index < _numberKeys.Length; index++)
                {
                    if (KeyHandler.PressedKeys[_numberKeys[index]])
                    {
                        _brushSize = index * 0.1f + 0.05f;
                    }
                }
            }

            // Switch modes
            {
                if (Mode.DirtyFlag.IsDirty)
                {
                    _mode = (Modes)Mode.GetValue(context).Clamp(0, Enum.GetNames(typeof(Modes)).Length - 1);
                }

                if (KeyHandler.PressedKeys[(int)Key.P])
                {
                    _mode = Modes.Draw;
                }
                else if (KeyHandler.PressedKeys[(int)Key.E])
                {
                    _mode = Modes.Erase;
                }
            }

            var brushSizeModeFactor = _mode == Modes.Erase ? 4 : 1;
            
            var mousePos = MousePos.GetValue(context);
            _currentPosInWorld = PointFromMousePos(context, mousePos);
            
            if (_mode != Modes.View)
            {
                if (TriggerReset.GetValue(context))
                {
                    Reset();
                }
                
                var isMouseDown = IsMouseButtonDown.GetValue(context);
                if (!isMouseDown && _mouseWasDown)
                {
                    if (_mode == Modes.Draw)
                    {
                        if (_currentStrokeLength == 1 && CurrentWriteIndex > 0) // add to points for single click to make it visible as a dot
                        {
                            var lastPoint = CurrentPointList.TypedElements[CurrentWriteIndex - 1];
                            lastPoint.Position = PointFromMousePos(context, _lastMousePos + new Vector2(0, 0.01f));
                            AppendPoint(lastPoint);
                        }

                        AppendPoint(Point.Separator());
                        _currentStrokeLength = 0;
                    }
                }
                else if (isMouseDown)
                {
                    var wasValid = !_mouseWasDown || Vector2.Distance(_lastMousePos, mousePos) > 0.001f;
                    if (wasValid)
                    {
                        switch (_mode)
                        {
                            case Modes.Draw:
                                AppendPoint(new Point()
                                                {
                                                    Position = _currentPosInWorld,
                                                    Orientation = new Quaternion(
                                                                                 BrushColor.GetValue(context).X,
                                                                                 BrushColor.GetValue(context).Y,
                                                                                 BrushColor.GetValue(context).Z,
                                                                                 BrushColor.GetValue(context).W
                                                                                ),
                                                    W = _brushSize + 0.001f,
                                                });
                                AppendPoint(Point.Separator(), advanceIndex: false);
                                _currentStrokeLength++;
                                _needsSave = true;
                                break;
                            
                            case Modes.Erase:
                            {
                                for (var index = 0; index < CurrentPointList.NumElements; index++)
                                {
                                    var distanceToPoint = Vector3.Distance(_currentPosInWorld, CurrentPointList.TypedElements[index].Position);
                                    if (!(distanceToPoint < _brushSize * 0.02f * brushSizeModeFactor))
                                        continue;
                                
                                    CurrentPointList.TypedElements[index].W = float.NaN;
                                    _needsSave = true;
                                }

                                break;
                            }
                        }
                    }

                    _lastMousePos = mousePos;
                }

                _mouseWasDown = isMouseDown;
            }

            OutPages.Value = _pages;
            CursorPosInWorld.Value = _currentPosInWorld;
            CurrentBrushSize.Value = _brushSize * brushSizeModeFactor;

            if (_needsSave && Math.Abs(context.LocalTime - _lastSaveTime) > 0.01)
            {
                string filepath1 = FilePath.GetValue(context);
                Core.Utilities.SaveJson(_pages, filepath1);
                _lastSaveTime = context.LocalTime;
                _needsSave = false;
            }
        }

        private static Vector3 PointFromMousePos(EvaluationContext context, Vector2 mousePos)
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
            return new Vector3(p.X, p.Y, p.Z) / p.W;
        }

        private void AppendPoint(Point p, bool advanceIndex = true)
        {
            if (CurrentWriteIndex >= CurrentPointList.NumElements - 1)
            {
                Log.Debug($"Increasing paint buffer length of {CurrentPointList.NumElements} by {BufferIncreaseStep}...");
                CurrentPointList.SetLength(CurrentPointList.NumElements + BufferIncreaseStep);
            }
            
            CurrentPointList.TypedElements[CurrentWriteIndex] = p;

            if (advanceIndex)
                CurrentWriteIndex++;
        }

        private void Reset()
        {
            CurrentPointList.SetLength(0);
            CurrentPointList.SetLength(BufferIncreaseStep);
            CurrentWriteIndex = 0;
        }

        public class Page
        {
            public int WriteIndex;
            
            [JsonConverter(typeof(StructuredListConverter))]
            public StructuredList<Point> PointsList;
        }
        



        
        private Page CurrentPage
        {
            get
            {
                if (_pages.TryGetValue(_pageIndex, out var page))
                {
                    return page;
                }

                var newPage = new Page { PointsList = new StructuredList<Point>(BufferIncreaseStep) };
                _pages[_pageIndex] = newPage;
                return newPage;
            }
        }

        private StructuredList<Point> CurrentPointList => CurrentPage.PointsList;

        private int CurrentWriteIndex { get => CurrentPage.WriteIndex; set => CurrentPage.WriteIndex = value; }

        private bool _needsSave;
        private double _lastSaveTime;
        
        private float _brushSize;
        private Vector3 _currentPosInWorld;
        private Modes _mode = Modes.Draw;
        private int _pageIndex;
        private bool _mouseWasDown;
        private Vector2 _lastMousePos;
        private int _currentStrokeLength;
        private string _filepath;
        private Dictionary<int, Page> _pages = new();
        
        
        private const int BufferIncreaseStep = 100; // low to reduce page file overhead
        
        
        private readonly int[] _numberKeys =
            { (int)Key.D1, (int)Key.D2, (int)Key.D3, (int)Key.D4, (int)Key.D5, (int)Key.D6, (int)Key.D7, (int)Key.D8, (int)Key.D9 };
        
        public enum Modes
        {
            View,
            Draw,
            Erase,
        }
        
        [Input(Guid = "C427F009-7E04-4168-82E6-5EBE2640204D")]
        public readonly InputSlot<Vector2> MousePos = new();

        [Input(Guid = "520A2023-7450-4314-9CAC-850D6D692461")]
        public readonly InputSlot<bool> IsMouseButtonDown = new();

        [Input(Guid = "B0D76CAB-8E7E-4845-B7E4-202424DFA12D")]
        public readonly InputSlot<bool> TriggerReset = new();

        [Input(Guid = "EA245C8A-D7F1-4F40-9D8D-F1D68070FEF2")]
        public readonly InputSlot<int> PageIndex = new();

        [Input(Guid = "7BC91594-AD3B-4C65-A4C1-CF2FA5759599", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "1057313C-006A-4F12-8828-07447337898B")]
        public readonly InputSlot<float> BrushSize = new();

        [Input(Guid = "4718E758-703E-4FF9-AC66-CE03CDCE2904")]
        public readonly InputSlot<System.Numerics.Vector4> BrushColor = new();

        [Input(Guid = "51641425-A2C6-4480-AC8F-2E6D2CBC300A")]
        public readonly InputSlot<string> FilePath = new();
    }
    

}