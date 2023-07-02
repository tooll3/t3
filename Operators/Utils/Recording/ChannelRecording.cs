using System.Collections.Generic;
using NAudio.Midi;
using T3.Core.Animation;
using T3.Core.DataTypes.DataSet;

namespace Operators.Utils.Recording;

/// <summary>
/// This is a stub for an implementation of midi signal recording
/// - These recordings are intended for later playback so that MidiConsumers would receive the signals and replay them like live signals. For this to work...
///   - MidiInput and MidiStream recorder would need to share the same MidiEvent definition (maybe different from NAudio.MidiEvent)
///   - Handle MidiEvent should not rely on the MidiIn class to avoid double lookup of device description.
/// </summary>
public class MidiStreamRecorder : MidiInConnectionManager.IMidiConsumer
{
    public readonly DataSet DataSet = new();

    public MidiStreamRecorder()
    {
        MidiInConnectionManager.RegisterConsumer(this);
    }

    public void Reset()
    {
        DataSet.Clear();
        _channelsByHash.Clear();
    }

    public void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
    {
        if (sender is not MidiIn midiIn || msg.MidiEvent == null)
            return;

        var device = MidiInConnectionManager.GetDescriptionForMidiIn(midiIn);
        var deviceName = (device.ProductName
                          + (device.ProductId is not (0 or 65535)
                                 ? device.ProductId.ToString()
                                 : string.Empty)).Replace("/", "_");

        var someTime = (float)msg.MidiEvent.AbsoluteTime;
        switch (msg.MidiEvent)
        {
            case NoteEvent midiNoteEvent:
                var noteChannel = FindOrCreateNoteChannel(deviceName, midiNoteEvent);
                var lastNote = noteChannel.GetLastEvent();

                switch (msg.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOff:
                        lastNote?.Finish(someTime);
                        break;
                    case MidiCommandCode.NoteOn:
                    {
                        if (lastNote != null && lastNote.IsUnfinished)
                        {
                            lastNote.Finish(someTime);
                        }

                        noteChannel.Events.Add(new DataEvent()
                                                   {
                                                       TimeRange = new TimeRange(someTime, float.PositiveInfinity),
                                                       TimeCode = midiNoteEvent.AbsoluteTime,
                                                       Value = (float)midiNoteEvent.Velocity,
                                                   });
                        break;
                    }
                }

                break;

            case ControlChangeEvent controlChangeEvent:
                FindOrCreateControlChangeChannel(deviceName, controlChangeEvent)
                   .Events
                   .Add(new DataEvent()
                            {
                                TimeRange = new TimeRange(someTime, someTime),
                                TimeCode = controlChangeEvent.AbsoluteTime,
                                Value = (float)controlChangeEvent.ControllerValue,
                            });
                break;
        }
    }

    private DataChannel FindOrCreateControlChangeChannel(string deviceName, ControlChangeEvent controlChangeEvent)
    {
        var hash = (byte)controlChangeEvent.Controller << 16 + controlChangeEvent.Channel;
        hash = hash * 31 + deviceName.GetHashCode();

        if (_channelsByHash.TryGetValue(hash, out var channel))
        {
            return channel;
        }

        var newChannel = new DataChannel(typeof(float))
                             {
                                 Path = new List<string>
                                            {
                                                MidiNamespacePrefix,
                                                deviceName,
                                                controlChangeEvent.Channel.ToString(),
                                                "CC" + (int)controlChangeEvent.Controller
                                            }
                             };
        _channelsByHash[hash] = newChannel;
        DataSet.Channels.Add(newChannel);
        return newChannel;
    }

    private DataChannel FindOrCreateNoteChannel(string deviceName, NoteEvent noteEvent)
    {
        var hash = (byte)noteEvent.NoteNumber << 16 + noteEvent.Channel;
        hash = hash * 31 + deviceName.GetHashCode();

        if (_channelsByHash.TryGetValue(hash, out var channel))
        {
            return channel;
        }

        var newChannel = new DataChannel(typeof(float))
                             {
                                 Path = new List<string>
                                            {
                                                MidiNamespacePrefix,
                                                deviceName,
                                                noteEvent.Channel.ToString(),
                                                "N" + noteEvent.NoteNumber
                                            },
                             };
        _channelsByHash[hash] = newChannel;
        DataSet.Channels.Add(newChannel);
        return newChannel;
    }

    public void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
    {
        //throw new System.NotImplementedException();
    }

    private const string MidiNamespacePrefix = "Midi";
    private readonly Dictionary<int, DataChannel> _channelsByHash = new();
}