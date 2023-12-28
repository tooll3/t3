using System.ComponentModel;
using Operators.Lib.Editor.CustomUi;
using Operators.Lib.Utils;
using Operators.Utils.Recording;
using T3.Core.DataTypes.DataSet;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Interaction.Timing;
using T3.Operators.Types.Id_2d1c9633_b66e_4958_913c_116ae36963a5;

namespace Operators.Lib.Editor;

public static class UiRegistration
{
    static UiRegistration()
    {
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_11882635_4757_4cac_a024_70bb4e8b504c.Counter), CounterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536.SampleGradient), GradientSliderUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_b724ea74_d5d7_4928_9cd1_7a7850e4e179.SampleCurve), SampleCurveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_3b0eb327_6ad8_424f_bca7_ccbfa2c9a986._Jitter), _JitterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819._Jitter2d), Jitter2dUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_5880cbc3_a541_4484_a06a_0e6f77cdbe8e.AString), AStringUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe.Value), ValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b.IntValue), IntValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169.Remap), RemapUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_ea7b8491_2f8e_4add_b0b1_fd068ccfed0d.AnimValue), AnimValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_af79ee8c_d08d_4dca_b478_b4542ed69ad8.AnimVec2), AnimVec2Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_7814fd81_b8d0_4edf_b828_5165f5657344.AnimVec3), AnimVec3Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_94a392e6_3e03_4ccf_a114_e6fafa263b4f.SequenceAnim), SequenceAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_95d586a2_ee14_4ff5_a5bb_40c497efde95.TriggerAnim), TriggerAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5.MidiInput), MidiInputUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4.Boolean), BooleanUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_0bec016a_5e1b_467a_8273_368d4d6b9935.Trigger), TriggerUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_be52b670_9749_4c0d_89f0_d8b101395227.LoadObj), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c.ComputeShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_646f5988_0a76_4996_a538_ba48054fd0ad.VertexShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_f7c625da_fede_4993_976c_e259e0ee4985.PixelShader), DescriptiveUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_470db771_c7f2_4c52_8897_d3a9b9fc6a4e.GetIntVar), GetIntVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_e6072ecf_30d2_4c52_afa1_3b195d61617b.GetFloatVar), GetFloatVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_2a0c932a_eb81_4a7d_aeac_836a23b0b789.SetFloatVar), SetFloatVarUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_03477b9a_860e_4887_81c3_5fe51621122c.AudioReaction), AudioReactionUi.DrawChildUi);

        PlaybackUtils.BpmProvider = BpmProvider.Instance;
        PlaybackUtils.TapProvider = TapProvider.Instance;
    }
}