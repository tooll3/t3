using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui
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
        
        public static Vector2 DefaultOpSize { get; } = new(110, 25);
        
        public Dictionary<Guid, ConnectionStyles> ConnectionStyleOverrides { get; } = new();
        
        public SymbolChild SymbolChild;

        public Guid Id => SymbolChild.Id;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = DefaultOpSize;
        public bool IsSelected => NodeSelection.IsNodeSelected(this);
        public int SnapshotGroupIndex { get; set; }
        public Styles Style { get; set; }

        public bool IsDisabled {
            get => SymbolChild.Outputs.FirstOrDefault().Value?.IsDisabled ?? false;
            set => SetDisabled(value);
        }

        public bool IsBypassed {
            get => _isBypassed;
            set => SetBypassed(value);
        }
        private bool _isBypassed = false;
        
        private void SetDisabled(bool shouldBeDisabled)
        {
            var outputDefinitions = SymbolChild.Symbol.OutputDefinitions;

            // Set disabled status on this child's outputs
            foreach (var outputDef in outputDefinitions)
            {
                if (outputDef == null)
                {
                    Log.Warning($"{SymbolChild.Symbol.GetType()} {SymbolChild.Symbol.Name} contains a null {typeof(Symbol.OutputDefinition)}", Id);
                    continue;
                }

                var hasOutput = SymbolChild.Outputs.TryGetValue(outputDef.Id, out var childOutput);
                if (!hasOutput)
                {
                    Log.Warning($"{typeof(SymbolChild)} {SymbolChild.ReadableName} does not have the following child output as defined: " +
                                $"{childOutput.OutputDefinition.Name}({nameof(Guid)}{childOutput.OutputDefinition.Id})");
                    continue;
                }

                childOutput.IsDisabled = shouldBeDisabled;
            }

            // Set disabled status on outputs of each instanced copy of this child within all parents that contain it
            foreach (var parentInstance in SymbolChild.Parent.InstancesOfSymbol)
            {
                var matchingChildInstances = parentInstance.Children.Where(child => child.SymbolChildId == Id).ToArray();

                // This parent doesn't have an instance of our SymbolChild. Ignoring and continuing.
                if (matchingChildInstances.Length == 0)
                    continue;

                // Set disabled status on all outputs of each instance
                foreach (var instance in matchingChildInstances)
                {
                    List<ISlot> outputs = instance.Outputs;

                    foreach (var t in outputs)
                    {
                        t.IsDisabled = shouldBeDisabled;
                    }
                }
            }
        }

        private bool IsBypassable()
        {
            var symbol = SymbolChild.Symbol;
            
            if(symbol.OutputDefinitions.Count == 0)
                return false;
            
            if(symbol.InputDefinitions.Count == 0)
                return false;

            var mainInput = symbol.InputDefinitions[0];
            var mainOutput = symbol.OutputDefinitions[0];

            if (mainInput.DefaultValue.ValueType != mainOutput.ValueType)
                return false;
            
            if(mainInput.DefaultValue.ValueType == typeof(Command))
                return true;
            
            if(mainInput.DefaultValue.ValueType == typeof(Texture2D))
                return true;

            if(mainInput.DefaultValue.ValueType == typeof(BufferWithViews))
                return true;

            if(mainInput.DefaultValue.ValueType == typeof(float))
                return true;

            return false;
        }

        private void SetBypassed(bool shouldBypass)
        {
            if(!IsBypassable())
                return;
            
            var parentInstancesOfSymbol = SymbolChild.Parent.InstancesOfSymbol;
            foreach (var parentInstance in parentInstancesOfSymbol)
            {
                var instance = parentInstance.Children.First(child => child.SymbolChildId == Id);

                var mainInputSlot = instance.Inputs[0];
                var mainOutputSlot = instance.Outputs[0];
                
                if (shouldBypass)
                {
                    mainOutputSlot.OverrideOrRestoreUpdateAction( mainInputSlot.GetUpdateAction());
                    if(mainOutputSlot is Slot<BufferWithViews> bufferOutput && mainInputSlot is Slot<BufferWithViews> bufferInput)
                        bufferOutput.Value = bufferInput.Value;

                    if(mainOutputSlot is Slot<Texture2D> texture2dOutput && mainInputSlot is Slot<Texture2D> texture2dInput)
                        texture2dOutput.Value = texture2dInput.Value;

                    if(mainOutputSlot is Slot<float> floatOutput && mainInputSlot is Slot<float> floatInput)
                        floatOutput.Value = floatInput.Value;
                }
                else
                {
                    mainOutputSlot.OverrideOrRestoreUpdateAction(null);
                }
                _isBypassed = shouldBypass;

            }
        }



        public static CustomUiResult DrawCustomUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            return CustomChildUiRegistry.Entries.TryGetValue(instance.Type, out var drawFunction) 
                       ? drawFunction(instance, drawList, selectableScreenRect) 
                       : CustomUiResult.None;
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
    }
}