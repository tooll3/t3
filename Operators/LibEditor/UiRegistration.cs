using lib._3d.mesh.generate;
using lib.anim;
using lib.anim._obsolete;
using lib.color;
using lib.data;
using lib.dx11;
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
        CustomChildUiRegistry.Register(typeof(Counter), CounterUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(SampleGradient), GradientSliderUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(SampleCurve), SampleCurveUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(_Jitter), _JitterUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(_Jitter2d), Jitter2dUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(lib.types.String), StringUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(Value), ValueUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(IntValue), IntValueUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(Remap), RemapUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(AnimValue), AnimValueUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(AnimVec2), AnimVec2Ui.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(AnimVec3), AnimVec3Ui.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(SequenceAnim), SequenceAnimUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(TriggerAnim), TriggerAnimUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(MidiInput), MidiInputUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(Boolean), BooleanUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(Trigger), TriggerUi.DrawChildUi);

        CustomChildUiRegistry.Register(typeof(GetIntVar), GetIntVarUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(GetFloatVar), GetFloatVarUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(SetFloatVar), SetFloatVarUi.DrawChildUi);

        CustomChildUiRegistry.Register(typeof(AudioReaction), AudioReactionUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Register(typeof(DataList), DataListUi.DrawChildUi);

        PlaybackUtils.BpmProvider = BpmProvider.Instance;
        PlaybackUtils.TapProvider = TapProvider.Instance;

        foreach (var templateDefinition in TemplateDefinitions.Templates)
        {
            TemplateDefinition.AddTemplateDefinition(templateDefinition);
        }
        
        Log.Debug("Registered UI EntriesRw. Total: {0}", CustomChildUiRegistry.Entries.Count);
    }
}