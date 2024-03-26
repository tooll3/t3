using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using System.Net;
using Rug.Osc;
using System;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_4e99da86_482f_4037_8664_b2371526d632
{
    public class OscOutput : Instance<OscOutput>
    {
        private bool _connected;
        private IPAddress _address;
        private int _port;
        private float _num_value;
        private string _str_value;
        private OscSender _sender;
        public OscOutput()
        {
            Result.UpdateAction = Update;
        }

        private enum OscType
        {
            Number,
            String
        }
        private void Update(EvaluationContext context)
        {
            var location = Location.GetValue(context);
            var num = Number.GetValue(context);
            var str = String.GetValue(context);
            var oscType = OscValueType.GetEnumValue<OscType>(context);

            CheckAddr(context);
            if (OscAddress.IsValidAddressPattern(location) == false)
            {
                Log.Error("OSC location is invalid");
                return;
            }
            switch (oscType)
            {
                case OscType.Number:
                    if (num != _num_value)
                    {
                        // Log.Info("Sending:" + num);
                        OscMessage message = new OscMessage(location, num);
                        if (message.Error == OscPacketError.None)
                        {
                            _sender.Send(message);
                            _num_value = num;
                        }
                        else Log.Error("failed to build the OSC message (float): " + message.ErrorMessage);
                    }
                    break;

                case OscType.String:
                    if (str != _str_value)
                    {
                        // Log.Info("Sending:" + str);
                        OscMessage message = new OscMessage(location, str);
                        if (message.Error == OscPacketError.None)
                        {
                            _sender.Send(message);
                            _str_value = str;
                        }
                        else Log.Error("failed to build the OSC message (string): " + message.ErrorMessage);
                    }
                    break;
            }
        }

        private void CheckAddr(EvaluationContext context)
        {
            IPAddress address;
            try
            {
                address = IPAddress.Parse(Address.GetValue(context));
            }
            catch (System.FormatException e)
            {
                Log.Error("Failed to parse ip: " + e);
                return;
            }
            int port = Port.GetValue(context);
            if (_connected) CheckChanges(address, port);
            else ConnectOSC(address, port);
        }

        // Connection changed, let's close and reset it.
        private void CheckChanges(IPAddress address, int port)
        {
            if (address != _address || port != _port)
            {
                _sender.Close();
                ConnectOSC(address, port);
            }
        }

        private void ConnectOSC(IPAddress address, int port)
        {
            try {
                _sender = new OscSender(address, port);
                _sender.Connect();
                if (_sender.State == OscSocketState.Connected) {
                    _address = address;
                    _port = port;
                    _connected = true;
                    return;
                }
            }
            catch (Exception e) {
                Log.Error("Connection failure: " + e);
            }
            _connected = false;
        }

        [Input(Guid = "98f38caa-4f79-425c-ab46-22d7dbe62978")]
        public readonly InputSlot<string> Address = new InputSlot<string>();

        [Input(Guid = "6c0e07ba-7ea6-4dab-af0b-61cf9cb74ad7")]
        public readonly InputSlot<int> Port = new InputSlot<int>();


        [Input(Guid = "9016e418-7761-4916-aafb-c95599f77f38")]
        public readonly InputSlot<string> Location = new InputSlot<string>();

        [Input(Guid = "d5e9e9be-093b-4d57-9070-9b2cf33aa45b")]
        public readonly InputSlot<string> String = new InputSlot<string>();

        [Input(Guid = "827c7ae2-c129-4d04-a715-328c0a86bf8a")]
        public readonly InputSlot<float> Number = new InputSlot<float>();

        [Input(Guid = "4931217b-e30d-42a6-9cda-bc49a8fd8d67", MappedType = typeof(OscType))]
        public readonly InputSlot<int> OscValueType = new ();

        [Output(Guid = "a6679d6c-fc34-4588-ab20-5079ad8f8a03")]
        public readonly Slot<T3.Core.DataTypes.Command> Result = new Slot<T3.Core.DataTypes.Command>();
    }
}
