using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.ChildUi;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using UiHelpers;
using T3.Core.Operator.Slots;

namespace T3.Gui
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectableCanvasObject
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
        public bool IsSelected => NodeSelection.IsNodeSelected(this);
        public Styles Style { get; set; }

        public bool IsDisabled {
            get => SymbolChild.Outputs.FirstOrDefault().Value?.IsDisabled ?? false;
            set 
            {
                List<Symbol.OutputDefinition> outputDefinitions = SymbolChild.Symbol.OutputDefinitions;

                // Set disabled status on this child's outputs
                foreach (var outputDef in outputDefinitions)
                {
                    if (outputDef == null)
                    {
                        Log.Warning($"{SymbolChild.Symbol.GetType()} {SymbolChild.Symbol.Name} contains a null {typeof(Symbol.OutputDefinition)}", Id);
                        continue;
                    }

                    SymbolChild.Output childOutput;
                    bool hasOutput = SymbolChild.Outputs.TryGetValue(outputDef.Id, out childOutput);

                    if(!hasOutput)
                    {
                        Log.Warning($"{typeof(SymbolChild)} {SymbolChild.ReadableName} does not have the following child output as defined: " +
                            $"{childOutput.OutputDefinition.Name}({nameof(Guid)}{childOutput.OutputDefinition.Id})");
                        continue;
                    }

                    childOutput.IsDisabled = value;
                }

                // Set disabled status on outputs of each instanced copy of this child within all parents that contain it
                foreach (var parentInstance in SymbolChild.Parent.InstancesOfSymbol)
                {
                    var matchingChildInstances = parentInstance.Children.Where(child => child.SymbolChildId == Id).ToArray();

                    //this parent doesn't have an instance of our SymbolChild. Ignoring and continuing.
                    if(matchingChildInstances.Length == 0)
                        continue;

                    //set disabled status on all outputs of each instance
                    foreach (var instance in matchingChildInstances)
                    {
                        List<ISlot> outputs = instance.Outputs;

                        foreach (var t in outputs)
                        {
                            t.IsDisabled = value;
                        }
                    }
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