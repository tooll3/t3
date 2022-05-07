namespace T3.Gui.Interaction.Variations.Midi
{
    /// <summary>
    /// Used to save captured note and control events from MidiInputs
    /// </summary>
    public class ControlChangeSignal
    {
        public int ControllerId;
        public float ControllerValue;
        public int Channel;
    }
}