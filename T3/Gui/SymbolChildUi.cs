using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.ChildUi;
using T3.Gui.Graph;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectableNode
    {
        public enum Styles
        {
            Default,
            Expanded,
            Resizable,
            WithThumbnail,
        }

        public enum ConnectionStyles
        {
            Default,
            FadedOut,
        }
        
        public static Vector2 DefaultOpSize { get; } = new Vector2(110, 25);
        
        public Dictionary<Guid, ConnectionStyles> ConnectionStyleOverrides { get; } = new Dictionary<Guid, ConnectionStyles>();
        
        public SymbolChild SymbolChild;
        public Guid Id => SymbolChild.Id;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = DefaultOpSize;
        public bool IsSelected => SelectionManager.IsNodeSelected(this);
        public Styles Style { get; set; }

        public bool IsDisabled {
            get => SymbolChild.Outputs.FirstOrDefault().Value?.IsDisabled ?? false;
            set 
                {
                    var childOutput = SymbolChild.Outputs.FirstOrDefault().Value;
                    if (childOutput != null)
                        childOutput.IsDisabled = value;

                    foreach (var parentInstance in SymbolChild.Parent.InstancesOfSymbol)
                    {
                        var childInstance = parentInstance.Children.Single(child => child.SymbolChildId == Id);
                        var output = childInstance.Outputs.FirstOrDefault();
                        if (output != null)
                            output.IsDisabled = value;
                    }
                }
        }

        public CustomUiResult DrawCustomUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!CustomChildUiRegistry.Entries.TryGetValue(instance.Type, out var drawFunction))
                return CustomUiResult.None;

            return drawFunction(instance, drawList, selectableScreenRect);
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum CustomUiResult
        {
            None =0,
            Rendered = 1<<2,
            PreventTooltip = 1<<3,
            PreventOpenSubGraph = 1<<4,
            PreventOpenParameterPopUp = 1<<5,
            PreventInputLabels = 1<<6,
        }
        
        public virtual SymbolChildUi Clone()
        {
            return new SymbolChildUi()
                   {
                       PosOnCanvas = PosOnCanvas,
                       Size = Size,
                       Style = Style,
                       SymbolChild = SymbolChild,
                   };
        }

        public Instance GetInstance(Instance compositionOp)
        {
            return compositionOp.Children.Single(child => child.SymbolChildId == Id);
        }
    }
}