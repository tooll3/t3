namespace T3.Gui.Interaction.PresetSystem.Model
{
    public class Preset
    {
        public States State = States.InActive;

        public enum States
        {
            Undefined,
            InActive,
            Active,
            Modified,
        }
    }
}