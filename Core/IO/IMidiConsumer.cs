using NAudio.Midi;

namespace T3.Core.IO;

public interface IMidiConsumer
{
    void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg);
    void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg);

    /// <summary>
    /// This will be called if the number of controllers or devices changed and the
    /// listener should update its status.
    /// </summary>
    void OnSettingsChanged();
}