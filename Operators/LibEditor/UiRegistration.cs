using lib._3d.mesh.generate;
using lib.anim;
using lib.anim._obsolete;
using lib.color;
using lib.dx11.compute;
using lib.dx11.draw;
using lib.exec.context;
using lib.io.audio;
using lib.io.midi;
using lib.math.@bool;
using lib.math.curve;
using lib.math.@float;
using lib.math.@int;
using lib.@string;
using lib.Utils;
using libEditor.CustomUi;
using T3.Core.Logging;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Templates;
using user.cynic.research;
using user.cynic.research.data;
using Boolean = lib.math.@bool.Boolean;

namespace libEditor;

public class UiRegistration : IOperatorUIInitializer
{
    // ReSharper disable once EmptyConstructor
    public UiRegistration()
    {
    }

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
        CustomChildUiRegistry.Entries.Add(typeof(GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(DataList), DataListUi.DrawChildUi);

        PlaybackUtils.BpmProvider = BpmProvider.Instance;
        PlaybackUtils.TapProvider = TapProvider.Instance;

        foreach (var templateDefinition in TemplateDefinitions.Templates)
        {
            TemplateDefinition.AddTemplateDefinition(templateDefinition);
        }
        
        Log.Debug("Registered UI entries. Total: {0}", CustomChildUiRegistry.Entries.Count);
    }
}