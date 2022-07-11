using System;
using System.Collections.Generic;
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
                var shouldCloseGroup = @group.Consumers.Count == 1;
                if (shouldCloseGroup)
                {
                    Log.Debug($"Closing port {group.Port}");
                    group.Receiver.Close();
                    group.Thread.Join();
                    _groupsByPort.Remove(group.Port);
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
            newReceiver.Connect();

            var newGroup = new PortGroup
                               {
                                   Port = port,
                                   Receiver = newReceiver,
                               };

            var thread = new Thread(new ThreadStart(newGroup.ThreadProc));
            newGroup.Thread = thread;
            thread.Start();
            return newGroup;
        }

        public interface IOscConsumer
        {
            void ProcessMessage(OscMessage msg);
            //void ErrorReceivedHandler(object sender, OscMessage msg);
        }

        private static readonly Dictionary<int, PortGroup> _groupsByPort = new();

        public class PortGroup
        {
            public int Port;
            public Thread Thread;
            public OscReceiver Receiver;
            public HashSet<IOscConsumer> Consumers = new();

            public void ThreadProc()
            {
                try
                {
                    while (Receiver.State != OscSocketState.Closed)
                    {
                        if (Receiver.State != OscSocketState.Connected)
                            continue;

                        // Get the next message. This will block until one arrives or the socket is closed
                        var oscPacket = Receiver.Receive();

                        var oscMessage = OscMessage.Parse(oscPacket.ToString());

                        //if (oscMessage.Address == "/beatTimer")
                        //{
                        // TODO: Do something
                        Log.Debug($"Got OSC on port {Port} message: {oscMessage.Address} {oscMessage}");

                        foreach (var consumer in Consumers)
                        {
                            consumer.ProcessMessage(oscMessage);
                        }

                        //OscMessageReceived?.Invoke(this, oscMessage);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    // if the socket was connected when this happens
                    // then tell the user
                    if (Receiver.State == OscSocketState.Connected)
                    {
                        Log.Debug("Exception in listen loop");
                        Log.Debug(ex.Message);
                    }
                }
            }
        }
    }
}