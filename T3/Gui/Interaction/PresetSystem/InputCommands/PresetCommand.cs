using T3.Core.Logging;

namespace T3.Gui.Interaction.PresetControl.InputCommands
{
    public abstract class InputCommand
    {
        public abstract void ExecuteOnce(PresetSystem presetSystem);

        public bool UpdateExecution(PresetSystem presetSystem)
        {
            return true; // complete?
        }

        public bool IsInstant = true;
    }

    public abstract class ButtonsPressCommand : InputCommand
    {
        protected readonly int[] Indices;

        protected ButtonsPressCommand(int[] indices)
        {
            Indices = indices;
        }
    }

    
    public class SavePresetCommand : ButtonsPressCommand
    {
        public SavePresetCommand(int[] indices) : base(indices)
        {
            
        }

        public override void ExecuteOnce(PresetSystem presetSystem)
        {
            Log.Debug($"SavePresetCommand.Execute({Indices})");
        }
    }

    
    public class ApplyPresetCommand : ButtonsPressCommand
    {
        public ApplyPresetCommand(int[] indices) : base(indices)
        {
        }

        public override void ExecuteOnce(PresetSystem presetSystem)
        {
            Log.Debug($"ApplyPresetCommand.Execute({Indices})");
        }
    }
}