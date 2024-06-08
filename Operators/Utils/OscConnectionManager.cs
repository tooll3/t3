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
                    group.Stop();
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
            try
            {
                newReceiver.Connect();
            }
            catch (Exception e)
            {
                Log.Warning("Failed to open OSC connection " + e.Message);
            }

            var newGroup = new PortGroup(newReceiver);
            _groupsByPort.Add(port, newGroup);
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
            private readonly OscReceiver receiver;
            private Thread thread;
            private bool isRunning;

            public int Port => this.receiver.Port;

            public HashSet<IOscConsumer> Consumers = new();

            public PortGroup(OscReceiver receiver)
            {
                if (receiver == null)
                    throw new ArgumentNullException("receiver");

                this.receiver = receiver;
                this.thread = new Thread(new ThreadStart(ThreadProc));
                this.isRunning = true;
                this.thread.Start();
            }

            private void ThreadProc()
            {
                while (this.isRunning)
                {
                    while (receiver.State != OscSocketState.Closed)
                    {
                        if (receiver.State != OscSocketState.Connected)
                            continue;

                        try
                        {
                            // Get the next message. This will block until one arrives or the socket is closed
                            var oscPacket = receiver.Receive();

                            //note rug.osc ignores non osc packets sent, so this is directly usable
                            try
                            {
                                if (oscPacket is OscBundle)
                                {
                                    var bundle = (OscBundle)oscPacket;

                                    foreach (var bundleContent in bundle)
                                    {
                                        if (bundleContent is OscMessage bundleMessage)
                                        {
                                            ForwardMessage(bundleMessage);
                                        }
                                    }
                                }
                                else if (oscPacket is OscMessage)
                                {
                                    ForwardMessage((OscMessage)oscPacket);
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

                    //vux: remark : do not wait 5 seconds if user changed the port
                    if (this.isRunning)
                    {
                        //vux: remark : normally the only case this would happen is if another app was using the port when starting t3
                        // the app got closed, otherwise listening on udp will not auto close, is that really necessary?
                        Log.Debug($"OSC connection on port {Port} closed");
                        while (receiver.State == OscSocketState.Closed)
                        {
                            Thread.Sleep(1000);
                            Log.Debug($"Trying to reconnect OSC port {Port}...");
                            receiver.Connect();
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
                this.isRunning = false;
                this.receiver.Dispose();
                this.thread.Join();
            }
        }
    }
}