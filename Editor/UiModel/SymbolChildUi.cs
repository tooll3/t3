using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.UiModel
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public sealed class SymbolChildUi : ISelectableCanvasObject
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
        
        public Symbol.Child SymbolChild;

        public Guid Id => SymbolChild.Id;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = DefaultOpSize;
        
        public int SnapshotGroupIndex;
        public Styles Style;
        public string Comment;

        public bool IsDisabled {
            get => SymbolChild.Outputs.FirstOrDefault().Value?.IsDisabled ?? false;
            set => SetDisabled(value);
        }

        internal SymbolChildUi()
        {
            
        }

        public SymbolUi Parent { get; internal set; }

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
                    Log.Warning($"{typeof(Symbol.Child)} {SymbolChild.ReadableName} does not have the following child output as defined: " +
                                $"{childOutput.OutputDefinition.Name}({nameof(Guid)}{childOutput.OutputDefinition.Id})");
                    continue;
                }

                childOutput.IsDisabled = shouldBeDisabled;
            }

            // Set disabled status on outputs of each instanced copy of this child within all parents that contain it
            foreach (var parentInstance in SymbolChild.Parent.InstancesOfSelf)
            {
                // This parent doesn't have an instance of our SymbolChild. Ignoring and continuing.
                if (!parentInstance.Children.TryGetValue(Id, out var matchingChildInstance))
                    continue;

                // Set disabled status on all outputs of each instance
                foreach (var slot in matchingChildInstance.Outputs)
                {
                    slot.IsDisabled = shouldBeDisabled;
                }
            }
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

        internal SymbolChildUi Clone(SymbolUi parent)
        {
            return new SymbolChildUi()
                   {
                       PosOnCanvas = PosOnCanvas,
                       Size = Size,
                       Style = Style,
                       SymbolChild = SymbolChild,
                       Parent = parent,
                   };
        }
        
        public override string ToString()
        {
            return $"{SymbolChild.Parent.Name}>[{SymbolChild.ReadableName}]";
        }
    }
}