namespace T3.Editor.Gui.Interaction.Midi.CommandProcessing
{
    /// <summary>
    /// Used to save captured note and control events from MidiInputs
    /// </summary>
    public class ButtonSignal
    {
        public int ButtonId;
        public float PressTime;
        public float ControllerValue;
        //public bool IsPressed;
        public States State;
        // public int PressCount;
        public int Channel;

        public enum States
        {
            Undefined,
            JustPressed,
            Hold,
            Released,
        }
    }
}