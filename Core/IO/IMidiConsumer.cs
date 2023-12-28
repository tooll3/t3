using NAudio.Midi;

namespace T3.Core.IO;

public interface IMidiConsumer
{
    void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg);
    void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg);
}