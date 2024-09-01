using System.Reflection;
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

public class UiRegistration : IEditorUiExtension
{
    // ReSharper disable once EmptyConstructor
    public UiRegistration()
    {
    }

    public void Initialize()
    {
        CustomChildUiRegistry.Register(typeof(Counter), CounterUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(SampleGradient), GradientSliderUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(SampleCurve), SampleCurveUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(_Jitter), _JitterUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(_Jitter2d), Jitter2dUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(lib.types.String), StringUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(Value), ValueUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(IntValue), IntValueUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(Remap), RemapUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(AnimValue), AnimValueUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(AnimVec2), AnimVec2Ui.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(AnimVec3), AnimVec3Ui.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(SequenceAnim), SequenceAnimUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(TriggerAnim), TriggerAnimUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(MidiInput), MidiInputUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(Boolean), BooleanUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(Trigger), TriggerUi.DrawChildUi, _types);

        CustomChildUiRegistry.Register(typeof(GetIntVar), GetIntVarUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(GetFloatVar), GetFloatVarUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(SetFloatVar), SetFloatVarUi.DrawChildUi, _types);

        CustomChildUiRegistry.Register(typeof(AudioReaction), AudioReactionUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(GpuMeasure), GpuMeasureUi.DrawChildUi, _types);
        CustomChildUiRegistry.Register(typeof(DataList), DataListUi.DrawChildUi, _types);

        PlaybackUtils.BpmProvider = BpmProvider.Instance;
        PlaybackUtils.TapProvider = TapProvider.Instance;

        foreach (var templateDefinition in TemplateDefinitions.Templates)
        {
            TemplateDefinition.AddTemplateDefinition(templateDefinition);
        }
        
        Log.Debug("Registered UI Entries");
    }

    public void Uninitialize()
    {
        foreach(var templateDefinition in TemplateDefinitions.Templates)
        {
            TemplateDefinition.RemoveTemplateDefinition(templateDefinition);
        }
        
        foreach (var type in _types)
        {
            CustomChildUiRegistry.Remove(type);
        }
        
        Log.Debug("Unregistered UI Entries");
    }
    
    private readonly List<Type> _types = new();
}