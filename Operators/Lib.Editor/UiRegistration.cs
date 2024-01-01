using Operators.Lib._3d.mesh.generate;
using Operators.Lib.anim;
using Operators.Lib.anim._obsolete;
using Operators.Lib.color;
using Operators.Lib.dx11.compute;
using Operators.Lib.dx11.draw;
using Operators.Lib.Editor.CustomUi;
using Operators.Lib.exec.context;
using Operators.Lib.io.audio;
using Operators.Lib.io.midi;
using Operators.Lib.math.@bool;
using Operators.Lib.math.curve;
using Operators.Lib.math.@float;
using Operators.Lib.math.@int;
using Operators.Lib.@string;
using Operators.Lib.Utils;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Templates;
using Boolean = Operators.Lib.math.@bool.Boolean;

namespace Operators.Lib.Editor;

public class UiRegistration : IOperatorUIInitializer
{

    public void Initialize()
    {
        CustomChildUiRegistry.Entries.Add(typeof(Counter), CounterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(SampleGradient), GradientSliderUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(SampleCurve), SampleCurveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(_Jitter), _JitterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(_Jitter2d), Jitter2dUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(AString), AStringUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(Value), ValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(IntValue), IntValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(Remap), RemapUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(AnimValue), AnimValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(AnimVec2), AnimVec2Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(AnimVec3), AnimVec3Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(SequenceAnim), SequenceAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(TriggerAnim), TriggerAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(MidiInput), MidiInputUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(Boolean), BooleanUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(Trigger), TriggerUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(LoadObj), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(ComputeShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(VertexShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(PixelShader), DescriptiveUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(GetIntVar), GetIntVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(GetFloatVar), GetFloatVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(SetFloatVar), SetFloatVarUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(AudioReaction), AudioReactionUi.DrawChildUi);

        PlaybackUtils.BpmProvider = BpmProvider.Instance;
        PlaybackUtils.TapProvider = TapProvider.Instance;

        foreach (var templateDefinition in TemplateDefinitions.Templates)
        {
            TemplateDefinition.AddTemplateDefinition(templateDefinition);
        }
    }
}