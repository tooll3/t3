namespace Editor.Gui.Interaction.Variations.Midi
{
    public class ModeButton
    {
        /// <summary>
        /// While holding switches the device into an input mode 
        /// </summary>
        public ModeButton(ButtonRange buttonRange, AbstractMidiDevice.InputModes mode, Interactions interaction= Interactions.ActiveWhileHolding)
        {
            ButtonRange = buttonRange;
            Mode = mode;
            Interaction = interaction;
        }

        public enum Interactions
        {
            ActiveWhileHolding,
            ToggleOnPress,
        }

        public Interactions Interaction;
        public ButtonRange ButtonRange;
        public AbstractMidiDevice.InputModes Mode;
    }
}