using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Rug.Osc;
using T3.Core.Logging;

namespace Operators.Utils
{
    public static class OscConnectionManager
    {
        public static void RegisterConsumer(IOscConsumer consumer, int port)
        {
            var group = CreateOrGetReceiverForPort(port);
            group.Consumers.Add(consumer);
        }

        public static void UnregisterConsumer(IOscConsumer consumer)
        {
            var foundConsumer = false;
            foreach (var group in _groupsByPort.Values)
            {
                if (!group.Consumers.Contains(consumer))
                    continue;

                foundConsumer = true;
                lock (_groupsByPort)
                {
                    var shouldCloseGroup = @group.Consumers.Count == 1 && group._isRunning;
                    if (shouldCloseGroup)
                    {
                        Log.Debug($"Closing OSC port {group.Port}");
                        try
                        {
                            group.Stop();
                        }
                        catch (Exception e)
                        {
                            Log.Debug("Exception: " + e.Message);
                        }

                        _groupsByPort.Remove(group.Port);
                    }
                    else
                    {
                        group.Consumers.Remove(consumer);
                    }
                }

                break;
            }

            if (!foundConsumer)
            {
                Log.Error("Attempted to unregister a non-registered OSC consumer?");
            }
        }

        private static PortGroup CreateOrGetReceiverForPort(int port)
        {
            if (_groupsByPort.TryGetValue(port, out var receiver))
                return receiver;

            var newReceiver = new OscReceiver(port);
            try
            {
                newReceiver.Connect();
                
                var newGroup = new PortGroup(newReceiver);
                _groupsByPort.Add(port, newGroup);
                return newGroup;
            }
            catch (Exception e)
            {
                Log.Warning("Failed to open OSC connection " + e.Message);
            }

            return null;
        }

        public interface IOscConsumer
        {
            void ProcessMessage(OscMessage msg);
        }

        public static bool TryGetScannedAddressesForPort(int port, out Dictionary<string, string> addresses)
        {
            if (_groupsByPort.TryGetValue(port, out var group))
            {
                addresses = group.ScannedAddresses;
                return true;
            }

            addresses = null;
            return false;
        }

        private static readonly Dictionary<int, PortGroup> _groupsByPort = new();

        private class PortGroup
        {
            public int Port => _receiver.Port;
            public readonly Dictionary<string, string> ScannedAddresses = new();

            public readonly HashSet<IOscConsumer> Consumers = new();

            public PortGroup(OscReceiver receiver)
            {
                _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
                _thread = new Thread(ThreadProc);
                _isRunning = true;
                _thread.Start();
            }

            private void ThreadProc()
            {
                while (_isRunning)
                {
                    while (_receiver.State != OscSocketState.Closed)
                    {
                        if (_receiver.State != OscSocketState.Connected)
                            continue;

                        try
                        {
                            // Get the next message. This will block until one arrives or the socket is closed
                            var oscPacket = _receiver.Receive();

                            // note rug.osc ignores non osc packets sent, so this is directly usable
                            try
                            {
                                switch (oscPacket)
                                {
                                    case OscBundle bundle:
                                    {
                                        foreach (var bundleContent in bundle)
                                        {
                                            if (bundleContent is not OscMessage bundleMessage)
                                                continue;

                                            KeepMessageAddress(bundleMessage);
                                            ForwardMessage(bundleMessage);
                                        }

                                        break;
                                    }
                                    case OscMessage message:
                                        KeepMessageAddress(message);
                                        ForwardMessage(message);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Warning($"Failed to parse OSC Message: '{oscPacket} {e.Message}'");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"OSC connection on port {Port} changed {e.Message}");
                        }
                    }

                    if (_isRunning)
                    {
                        // vux: remark : normally the only case this would happen is if another app was using the port when starting t3
                        // the app got closed, otherwise listening on udp will not auto close, is that really necessary?
                        Log.Debug($"OSC connection on port {Port} closed");
                        while (_receiver.State == OscSocketState.Closed)
                        {
                            Thread.Sleep(100);
                            Log.Debug($"Trying to reconnect OSC port {Port}...");
                            _receiver.Connect();
                        }
                    }
                }
            }

            private void ForwardMessage(OscMessage message)
            {
                foreach (var consumer in Consumers)
                {
                    consumer.ProcessMessage(message);
                }
            }

            public void Stop()
            {
                _isRunning = false;
                _receiver.Dispose();
                _thread.Join();
            }

            private void KeepMessageAddress(OscMessage packet)
            {
                if (ScannedAddresses.ContainsKey(packet.Address))
                    return;

                var sb = new StringBuilder();
                foreach (var arg in packet)
                {
                    var v = arg switch
                                {
                                    float  => "f",
                                    int    => "i",
                                    bool   => "b",
                                    string => "s",
                                    double => "d",
                                    _      => "?"
                                };
                    sb.Append(v);
                }

                ScannedAddresses[packet.Address] = sb.ToString();
            }

            private readonly OscReceiver _receiver;
            private readonly Thread _thread;
            public bool _isRunning;
        }
        
        public static bool TryGetFloatFromMessagePart(object arg, out float value)
        {
            value = arg switch
                        {
                            float f => f,
                            int   i => i,
                            bool  b => b ? 1f:0f,
                            string s => float.TryParse(s, out var f) ? f : float.NaN,
                            double d => (float)d,
                            _      => float.NaN
                        };
            return !float.IsNaN(value);
        }

        public static string BuildMessageComponentPath(OscMessage msg, int index)
        {
            const string channels="xyzw";
            var suffix = index < 4 ? channels[index].ToString() : index.ToString(); 
            return  msg.Address + "." + suffix;
        }
        
        public static string BuildMessageComponentPath(OscMessage msg)
        {
            return  msg.Address;
        }
    }
}