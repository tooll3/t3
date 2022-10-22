using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SharpDX;
using T3.Core;
using T3.Core.Animation;
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

        [Output(Guid = "E1B35EFA-3A49-4AB3-83AE-A2DED1CEF908")]
        public readonly Slot<int> ActivePageIndex = new();

        [Output(Guid = "BD29C7D2-1296-48CB-AD85-F96C27A35B92")]
        public readonly Slot<string> StatusMessage = new();

        public _SketchImpl()
        {
            OutPages.UpdateAction = Update;
            CursorPosInWorld.UpdateAction = Update;
            StatusMessage.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var isFilePathDirty = FilePath.DirtyFlag.IsDirty;
            var filepath = FilePath.GetValue(context);

            if (isFilePathDirty)
            {
                UpdatePagesFromDisk(filepath);
            }

            var pageIndexNeedsUpdate = Math.Abs(_lastUpdateContextTime - context.LocalTime) > 0.001;
            if (pageIndexNeedsUpdate || isFilePathDirty)
            {
                UpdatePagesForTime(context.LocalTime);
                _lastUpdateContextTime = context.LocalTime;
            }

            // Switch Brush size
            {
                if (BrushSize.DirtyFlag.IsDirty)
                {
                    _brushSize = BrushSize.GetValue(context);
                }

                for (var index = 0; index < _numberKeys.Length; index++)
                {
                    if (!KeyHandler.PressedKeys[_numberKeys[index]])
                        continue;

                    _brushSize = (index * index + 0.5f) * 0.1f;
                }
            }

            // Switch modes
            {
                if (Mode.DirtyFlag.IsDirty)
                {
                    _drawMode = (DrawModes)Mode.GetValue(context).Clamp(0, Enum.GetNames(typeof(DrawModes)).Length - 1);
                }

                if (KeyHandler.PressedKeys[(int)Key.P])
                {
                    _drawMode = DrawModes.Draw;
                }
                else if (KeyHandler.PressedKeys[(int)Key.E])
                {
                    _drawMode = DrawModes.Erase;
                }
                else if (KeyHandler.PressedKeys[(int)Key.X])
                {
                    if (IsTimeAtPage)
                    {
                        _cuttedPage = ActivePage;
                        _orderedPages.Remove(ActivePage);
                        UpdatePagesForTime(context.LocalTime);
                    }
                }
                else if (KeyHandler.PressedKeys[(int)Key.V])
                {
                    if (_cuttedPage != null)
                    {
                        if (IsTimeAtPage)
                            _orderedPages.Remove(ActivePage);

                        _cuttedPage.Time = context.LocalTime;
                        _orderedPages.Add(_cuttedPage);
                        UpdatePagesForTime(context.LocalTime);
                    }
                }
            }

            var wasModified = DoSketch(context, out CursorPosInWorld.Value, out CurrentBrushSize.Value);

            OutPages.Value = _orderedPages;
            ActivePageIndex.Value = _activePageIndex;
            var pageTitle = IsTimeAtPage ? $"PAGE{_activePageIndex}" : "EMPTY PAGE";
            var tool = !IsOpSelected
                           ? "Not selected"
                           : _drawMode == DrawModes.Draw
                               ? "PEN"
                               : "ERASE";

            var cutSomething = _cuttedPage != null ? "/ PASTE WITH V" : "";
            StatusMessage.Value = $"{pageTitle}: {tool} {cutSomething}";

            if (wasModified)
            {
                _lastModificationTime = Playback.RunTimeInSecs;
                _needsSave = true;
            }

            if (_needsSave && Playback.RunTimeInSecs - _lastModificationTime > 2)
            {
                var filepath1 = FilePath.GetValue(context);
                Core.Utilities.SaveJson(_orderedPages, filepath1);
                _needsSave = false;
            }
        }

        private bool DoSketch(EvaluationContext context, out Vector3 posInWorld, out float visibleBrushSize)
        {
            visibleBrushSize = _brushSize;
            if (_drawMode == DrawModes.Erase)
                visibleBrushSize *= 4;

            posInWorld = CalcPosInWorld(context, MousePos.GetValue(context));

            if (_drawMode == DrawModes.View || !IsOpSelected)
            {
                _isMouseDown = false;
                _currentStrokeLength = 0;
                return false;
            }

            var isMouseDown = IsMouseButtonDown.GetValue(context);
            var justReleased = !isMouseDown && _isMouseDown;
            var justPressed = isMouseDown && !_isMouseDown;
            _isMouseDown = isMouseDown;
            
            if (justReleased)
            {
                if (_drawMode != DrawModes.Draw || !IsTimeAtPage)
                    return false;

                // Add to points for single click to make it visible as a dot
                var wasClick = _currentStrokeLength == 1;
                if (wasClick)
                {
                    if (!GetPreviousStrokePoint(out var clickPoint))
                    {
                        return false;
                    }

                    clickPoint.Position += Vector3.UnitY * 0.02f * 2 * visibleBrushSize;
                    AppendPoint(clickPoint);
                }

                AppendPoint(Point.Separator());
                _currentStrokeLength = 0;
                return true;
            }

            if (!_isMouseDown)
                return false;

            if (_currentStrokeLength > 0 && GetPreviousStrokePoint(out var lastPoint))
            {
                var distance = Vector3.Distance(lastPoint.Position, posInWorld);
                var minDistanceForBrushSize = 0.01f;

                var updateLastPoint = distance < visibleBrushSize * minDistanceForBrushSize;
                if (updateLastPoint)
                {
                    // Sadly, adding intermedia points causes too many artifacts
                    // lastPoint.Position = posInWorld;
                    // AppendPoint(lastPoint, advanceIndex: false);
                    return false;
                }
            }

            switch (_drawMode)
            {
                case DrawModes.Draw:
                    if (!IsTimeAtPage)
                        InsertNewPage(context.LocalTime);

                    if(justPressed && KeyHandler.PressedKeys[(int)Key.ShiftKey] && ActivePage.WriteIndex > 1)
                    {
                        // Discard last separator point
                        ActivePage.WriteIndex--;
                        _currentStrokeLength = 1;
                    }
                    

                    AppendPoint(new Point()
                                    {
                                        Position = posInWorld,
                                        // Orientation = new Quaternion(
                                        //                              BrushColor.GetValue(context).X,
                                        //                              BrushColor.GetValue(context).Y,
                                        //                              BrushColor.GetValue(context).Z,
                                        //                              BrushColor.GetValue(context).W
                                        //                             ),
                                        W = visibleBrushSize / 2 + 0.002f, // prevent getting too small
                                    });
                    AppendPoint(Point.Separator(), advanceIndex: false);
                    _currentStrokeLength++;
                    return true;

                case DrawModes.Erase:
                {
                    if (!IsTimeAtPage)
                        return false;

                    var wasModified = false;
                    for (var index = 0; index < CurrentPointList.NumElements; index++)
                    {
                        var distanceToPoint = Vector3.Distance(posInWorld, CurrentPointList.TypedElements[index].Position);
                        if (!(distanceToPoint < visibleBrushSize * 0.02f))
                            continue;

                        CurrentPointList.TypedElements[index].W = float.NaN;
                        wasModified = true;
                    }

                    return wasModified;
                }
            }

            return false;
        }

        private void InsertNewPage(double time)
        {
            _orderedPages.Add(new Page()
                                  {
                                      Time = time,
                                      PointsList = new StructuredList<Point>(BufferIncreaseStep),
                                  });
            _orderedPages = _orderedPages.OrderBy(p => p.Time).ToList();
            UpdatePagesForTime(time);
        }

        private void UpdatePagesFromDisk(string filepath)
        {
            _orderedPages = Core.Utilities.TryLoadingJson<List<Page>>(filepath);

            if (_orderedPages != null)
            {
                foreach (var page in _orderedPages)
                {
                    if (page.PointsList == null)
                    {
                        page.PointsList = new StructuredList<Point>(BufferIncreaseStep);
                        continue;
                    }

                    if (page.PointsList.NumElements > page.WriteIndex)
                        continue;

                    //Log.Warning($"Adjusting writing index {page.WriteIndex} -> {page.PointsList.NumElements}");
                    page.WriteIndex = page.PointsList.NumElements+1;
                }
            }
            else
            {
                _orderedPages = new List<Page>();
            }
        }

        private static Vector3 CalcPosInWorld(EvaluationContext context, Vector2 mousePos)
        {
            const float offsetFromCamPlane = 0.99f;
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
            if (!IsTimeAtPage)
            {
                Log.Warning("Tried writing to undefined sketch page");
                return;
            }

            if (ActivePage.WriteIndex >= CurrentPointList.NumElements - 1)
            {
                //Log.Debug($"Increasing paint buffer length of {CurrentPointList.NumElements} by {BufferIncreaseStep}...");
                CurrentPointList.SetLength(CurrentPointList.NumElements + BufferIncreaseStep);
            }

            CurrentPointList.TypedElements[ActivePage.WriteIndex] = p;

            if (advanceIndex)
                ActivePage.WriteIndex++;
        }

        public class Page
        {
            public int WriteIndex;
            public double Time;

            [JsonConverter(typeof(StructuredListConverter))]
            public StructuredList<Point> PointsList;
        }

        private void UpdatePagesForTime(double contextLocalTime)
        {
            for (var pageIndex = 0; pageIndex < _orderedPages.Count; pageIndex++)
            {
                var page = _orderedPages[pageIndex];
                if (!(Math.Abs(page.Time - contextLocalTime) < 0.05))
                    continue;

                _activePageIndex = pageIndex;
                return;
            }

            _activePageIndex = NoPageIndex;
        }

        private bool GetPreviousStrokePoint(out Point point)
        {
            if (!IsTimeAtPage || _currentStrokeLength == 0 || ActivePage.WriteIndex == 0)
            {
                Log.Warning("Can't get previous stroke point");
                point = new Point();
                return false;
            }

            point = CurrentPointList.TypedElements[ActivePage.WriteIndex - 1];
            return true;
        }

        private Page _cuttedPage;

        private double _lastModificationTime;
        private StructuredList<Point> CurrentPointList => ActivePage.PointsList;

        private float _brushSize;
        private bool _needsSave;
        private DrawModes _drawMode = DrawModes.Draw;
        private bool _isMouseDown;

        private int _currentStrokeLength;

        private List<Page> _orderedPages = new();

        private double _lastUpdateContextTime = -1;

        private bool IsOpSelected => MouseInput.SelectedChildId == Parent.SymbolChildId;

        private bool IsTimeAtPage => _activePageIndex != NoPageIndex;
        private Page ActivePage => IsTimeAtPage ? _orderedPages[_activePageIndex] : null;
        private int _activePageIndex = NoPageIndex;

        private const int BufferIncreaseStep = 100; // low to reduce page file overhead

        private const int NoPageIndex = -1;

        private readonly int[] _numberKeys =
            { (int)Key.D1, (int)Key.D2, (int)Key.D3, (int)Key.D4, (int)Key.D5, (int)Key.D6, (int)Key.D7, (int)Key.D8, (int)Key.D9 };

        public enum DrawModes
        {
            View,
            Draw,
            Erase,
        }

        [Input(Guid = "C427F009-7E04-4168-82E6-5EBE2640204D")]
        public readonly InputSlot<Vector2> MousePos = new();

        [Input(Guid = "520A2023-7450-4314-9CAC-850D6D692461")]
        public readonly InputSlot<bool> IsMouseButtonDown = new();

        // [Input(Guid = "EA245C8A-D7F1-4F40-9D8D-F1D68070FEF2")]
        // public readonly InputSlot<int> PageIndex = new();
        //
        [Input(Guid = "7BC91594-AD3B-4C65-A4C1-CF2FA5759599", MappedType = typeof(DrawModes))]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "1057313C-006A-4F12-8828-07447337898B")]
        public readonly InputSlot<float> BrushSize = new();

        [Input(Guid = "4718E758-703E-4FF9-AC66-CE03CDCE2904")]
        public readonly InputSlot<System.Numerics.Vector4> BrushColor = new();

        [Input(Guid = "51641425-A2C6-4480-AC8F-2E6D2CBC300A")]
        public readonly InputSlot<string> FilePath = new();
    }
}