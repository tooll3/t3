using System;
using System.Collections.Generic;
using T3.Core.Animation;
using NAudio.Midi;
using T3.Core.Logging;

namespace Operators.Utils.ChannelRecording;

public interface IChannelSet
{
    public List<IChannel> Channels { get; set; }
    public void Reset();
}

public interface IChannel
{
    public List<string> Path { get; set; }
    public string Name { get; set; }
    public List<SignalEvent> Events { get; set; }

    public SignalEvent GetLastEvent();

}

public class SignalChannel : IChannel
{
    public List<string> Path { get; set; }
    public string Name { get; set; }
    public List<SignalEvent> Events { get; set; } = new();
    public SignalEvent GetLastEvent()
    {
        {
            if (Events == null || Events.Count == 0)
                return null;

            return Events[^1];
        }
    }

    public Type Type;
}


public class SignalEvent
{
    public TimeRange TimeRange;
    public double TimeCode;
    public float Value { get; set; }

    public bool IsUnfinished => float.IsInfinity(TimeRange.End);

    public void Finish(float someTime)
    {
        if (!IsUnfinished)
        {
            Log.Warning("setting finish time of fished note?");
        }

        TimeRange.End = someTime;
    }
}



public class SignalIntervalsChannel : SignalChannel
{
}

/// <summary>
/// This is a stub for an implementation of midi signal recording
/// - These recordings are intended for later playback so that MidiConsumers would receive the signals and replay them like live signals. For this to work...
///   - MidiInput and MidiStream recorder would need to share the same MidiEvent definition (maybe different from NAudio.MidiEvent)
///   - Handle MidiEvent should not rely on the MidiIn class to avoid double lookup of device description.
/// </summary>
public class MidiStreamRecorder : MidiInConnectionManager.IMidiConsumer, IChannelSet
{
    public MidiStreamRecorder()
    {
        MidiInConnectionManager.RegisterConsumer(this);
    }

    public List<IChannel> Channels { get; set; } = new();
    public void Reset()
    {
        Channels.Clear();
        _channelsByHash.Clear();
    }

    public void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
    {
        if (sender is not MidiIn midiIn || msg.MidiEvent == null)
            return;

        var device = MidiInConnectionManager.GetDescriptionForMidiIn(midiIn);
        var deviceName = device.ProductName + ((device.ProductId == 0 || device.ProductId == 65535) ? "" : device.ProductId.ToString());

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

                        noteChannel.Events.Add(new SignalEvent()
                                                   {
                                                       TimeRange = new TimeRange(someTime, float.PositiveInfinity),
                                                       TimeCode = midiNoteEvent.AbsoluteTime,
                                                       Value = midiNoteEvent.Velocity,
                                                   });
                        break;
                    }
                }

                break;

            case ControlChangeEvent controlChangeEvent:
                FindOrCreateControlChangeChannel(deviceName, controlChangeEvent).Events.Add(new SignalEvent()
                                                                                                {
                                                                                                    TimeRange = new TimeRange(someTime, someTime),
                                                                                                    TimeCode = controlChangeEvent.AbsoluteTime,
                                                                                                    Value = controlChangeEvent.ControllerValue,
                                                                                                });
                break;
        }
    }

    private SignalChannel FindOrCreateControlChangeChannel(string deviceName, ControlChangeEvent controlChangeEvent)
    {
        var hash = (byte)controlChangeEvent.Controller << 16 + controlChangeEvent.Channel;
        hash = hash * 31 + deviceName.GetHashCode();

        if (_channelsByHash.TryGetValue(hash, out var channel))
        {
            return channel;
        }

        var newChannel = new SignalChannel
                             {
                                 Path = new List<string>
                                            {
                                                deviceName,
                                                controlChangeEvent.Channel.ToString(),
                                                "CC" + (int)controlChangeEvent.Controller
                                            }
                             };
        _channelsByHash[hash] = newChannel;
        Channels.Add(newChannel);
        return newChannel;
    }

    private SignalChannel FindOrCreateNoteChannel(string deviceName, NoteEvent noteEvent)
    {
        var hash = (byte)noteEvent.NoteNumber << 16 + noteEvent.Channel;
        hash = hash * 31 + deviceName.GetHashCode();

        if (_channelsByHash.TryGetValue(hash, out var channel))
        {
            return channel;
        }

        var newChannel = new SignalIntervalsChannel
                             {
                                 Path = new List<string>
                                            {
                                                deviceName,
                                                noteEvent.Channel.ToString(),
                                                "N"+noteEvent.NoteNumber
                                            }
                             };
        _channelsByHash[hash] = newChannel;
        Channels.Add(newChannel);
        return newChannel;
    }

    public void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
    {
        //throw new System.NotImplementedException();
    }

    private readonly Dictionary<int, SignalChannel> _channelsByHash = new();
}