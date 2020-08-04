using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Windows;

namespace T3.Gui.Selection
{
    public static class SelectionManager
    {
        public static void Clear()
        {
            Selection.ForEach(RemoveTransformCallback);
            Selection.Clear();
        }

        public static void SetSelectionToParent(Instance instance)
        {
            Clear();
            _parent = instance;
        }

        public static void SetSelection(ISelectableNode node)
        {
            if (node is SymbolChildUi)
            {
                Log.Warning("Setting selection to a SymbolChildUi without providing instance will lead to problems.");
            }
            Clear();
            AddSelection(node);
        }

        public static void SetSelection(SymbolChildUi node, Instance instance)
        {
            Clear();
            AddSelection(node, instance);
        }

        public static void AddSelection(ISelectableNode node)
        {
            _parent = null;
            if (Selection.Contains(node))
                return;
            
            Selection.Add(node);
        }

        public static void AddSelection(SymbolChildUi node, Instance instance)
        {
            _parent = null;
            if (Selection.Contains(node))
                return;
            
            Selection.Add(node);
            if (instance != null)
            {
                ChildUiInstanceIdPaths[node] = NodeOperations.BuildIdPathForInstance(instance);
                if (instance is ITransformable transformable)
                {
                    transformable.TransformCallback = TransformCallback;
                    RegisteredTransformCallbacks[node] = transformable;
                }
            }
        }

        // todo: move this to the right place when drawing is clear
        private static void TransformCallback(ITransformable transform, EvaluationContext context)
        {
            var objectToClipSpace = context.ObjectToWorld * context.WorldToCamera * context.CameraToClipSpace;
            var t = transform.Translation;
            Vector4 originInClipSpace = Vector4.Transform(new Vector4(t.X, t.Y, t.Z, 1.0f), objectToClipSpace);
            originInClipSpace *= 1.0f / originInClipSpace.W;
            var viewports = ResourceManager.Instance().Device.ImmediateContext.Rasterizer.GetViewports<RawViewportF>();
            var originInViewport = new System.Numerics.Vector2(viewports[0].Width * (originInClipSpace.X * 0.5f + 0.5f),
                                                               viewports[0].Height * (1.0f - (originInClipSpace.Y * 0.5f + 0.5f)));

            var canvas = ImageOutputCanvas.Current;
            var originInCanvas = canvas.TransformDirection(originInViewport);
            var topLeftOnScreen = ImageOutputCanvas.Current.TransformPosition(System.Numerics.Vector2.Zero);
            var originInScreen = topLeftOnScreen + originInCanvas;

            // ImGui.GetWindowDrawList().AddCircleFilled(textPos, 6.0f, 0xFFFFFFFF);
            // need foreground draw list atm as texture is drawn afterwards to output view
            ImGui.GetForegroundDrawList().AddCircleFilled(originInScreen, 6.0f, 0xFFFFFFFF);
            // Log.Debug($"origin: {originInViewport}");
        }


        public static void RemoveSelection(ISelectableNode node)
        {
            Selection.Remove(node);
            RemoveTransformCallback(node);
        }

        private static void RemoveTransformCallback(ISelectableNode node)
        {
            if (RegisteredTransformCallbacks.TryGetValue(node, out var transformable))
            {
                transformable.TransformCallback = null;
            }
        }

        public static IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableNode
        {
            foreach (var item in Selection)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        }

        public static bool IsNodeSelected(ISelectableNode node)
        {
            return Selection.Contains(node);
        }

        public static bool IsAnythingSelected()
        {
            return Selection.Count > 0;
        }

        
        /// <summary>
        /// This is called at the beginning of each frame.
        /// 
        /// For some events we have to use a frame delay machanism to ui-elements can
        /// respond to updates in a controlled manner (I.e. when rendering the next frame) 
        /// </summary>
        public static void ProcessNewFrame()
        {
            //NodesSelectedLastFrame.Clear();
            // foreach (var n in Selection)
            // {
            //     if (!_lastFrameSelection.Contains(n))
            //         NodesSelectedLastFrame.Add(n);
            // }
        
            //_lastFrameSelection = Selection;
            FitViewToSelectionRequested = _fitViewToSelectionTriggered;
            _fitViewToSelectionTriggered = false;
        }

        public static Instance GetSelectedInstance()
        {
            if (Selection.Count == 0)
                return _parent;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                if (!ChildUiInstanceIdPaths.ContainsKey(firstNode))
                {
                    Log.Error("Failed to access id-path of selected childUi " + firstNode.SymbolChild.Name);
                    Clear();
                    return null;
                }
                var idPath = ChildUiInstanceIdPaths[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        public static Instance GetCompositionForSelection()
        {
            if (Selection.Count == 0)
                return _parent;

            if (!(Selection[0] is SymbolChildUi firstNode))
                return null;
            
            var idPath = ChildUiInstanceIdPaths[firstNode];
            var instanceFromIdPath = NodeOperations.GetInstanceFromIdPath(idPath);
            return instanceFromIdPath?.Parent;
        }
        

        public static IEnumerable<SymbolChildUi> GetSelectedSymbolChildUis()
        {
            var result = new List<SymbolChildUi>();
            foreach (var s in Selection)
            {
                if (!(s is SymbolChildUi symbolChildUi))
                    continue;

                result.Add(symbolChildUi);
            }

            return result;
        }

        public static Instance GetInstanceForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            var idPath = ChildUiInstanceIdPaths[symbolChildUi];
            return (NodeOperations.GetInstanceFromIdPath(idPath));
        }
        
        public static void FitViewToSelection()
        {
            _fitViewToSelectionTriggered = true;
        }


        public static bool FitViewToSelectionRequested { get; private set; }
        private static bool _fitViewToSelectionTriggered = false;
        private static Instance _parent;
        private static readonly List<ISelectableNode> Selection = new List<ISelectableNode>();
        //private static readonly List<ISelectableNode> NodesSelectedLastFrame = new List<ISelectableNode>();
        //private static List<ISelectableNode> _lastFrameSelection = new List<ISelectableNode>();
        private static readonly Dictionary<SymbolChildUi, List<Guid>> ChildUiInstanceIdPaths = new Dictionary<SymbolChildUi, List<Guid>>();
        private static readonly Dictionary<ISelectableNode, ITransformable> RegisteredTransformCallbacks = new Dictionary<ISelectableNode, ITransformable>(10);
    }
}