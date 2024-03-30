using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System.Net;
using Rug.Osc;
using System;
using T3.Core.Utils;
using T3.Core.Operator.Interfaces;

namespace T3.Operators.Types.Id_4e99da86_482f_4037_8664_b2371526d632
{
    public class OscOutput : Instance<OscOutput>, IStatusProvider
    {
        [Input(Guid = "98f38caa-4f79-425c-ab46-22d7dbe62978")]
        public readonly InputSlot<string> IpAddress = new InputSlot<string>();

        [Input(Guid = "6c0e07ba-7ea6-4dab-af0b-61cf9cb74ad7")]
        public readonly InputSlot<int> Port = new InputSlot<int>();

        [Input(Guid = "9016e418-7761-4916-aafb-c95599f77f38")]
        public readonly InputSlot<string> Location = new InputSlot<string>();

        [Input(Guid = "d5e9e9be-093b-4d57-9070-9b2cf33aa45b")]
        public readonly InputSlot<string> String = new InputSlot<string>();

        [Input(Guid = "827c7ae2-c129-4d04-a715-328c0a86bf8a")]
        public readonly InputSlot<float> Number = new InputSlot<float>();

        [Input(Guid = "4931217b-e30d-42a6-9cda-bc49a8fd8d67", MappedType = typeof(OscTypes))]
        public readonly InputSlot<int> OscValueType = new();

        [Output(Guid = "a6679d6c-fc34-4588-ab20-5079ad8f8a03")]
        public readonly Slot<T3.Core.DataTypes.Command> Result = new Slot<T3.Core.DataTypes.Command>();

        public OscOutput()
        {
            Result.UpdateAction = Update;
        }

        private enum OscTypes
        {
            Number,
            String
        }

        private void Update(EvaluationContext context)
        {
            var location = Location.GetValue(context);
            var num = Number.GetValue(context);
            var str = String.GetValue(context);
            var oscType = OscValueType.GetEnumValue<OscTypes>(context);
            if (TryGetValidAddress(IpAddress.GetValue(context), out var error, out var address))
            {
                int port = Port.GetValue(context);
                if (port <= 0 || port > 65535)
                {
                    _lastErrorMessage = "Port must be found in 1-65535 range";
                    return;
                }

                if (!_connected) _connected = TryConnectOSC(address, port);
                else
                {
                    // Connection changed, let's close and reset it.
                    if (address != _ipAddress || port != _port)
                    {
                        _sender.Close();
                        _connected = TryConnectOSC(address, port);
                    }
                }
            }
            else
            {
                _lastErrorMessage = error;
                return;
            }


            // Address means location in Rug.Osc, and is confusing, not related to our IpAddress
            if (OscAddress.IsValidAddressPattern(location) == false)
            {
                _lastErrorMessage = $"The OSC location '{location}' is invalid";
                return;
            }

            var _invalidMessage = $"Failed to build the OSC message({oscType})";
            switch (oscType)
            {
                case OscTypes.Number:
                    if (num != _numericValue)
                    {
                        OscMessage message = new OscMessage(location, num);
                        if (message.Error == OscPacketError.None)
                        {
                            _sender.Send(message);
                            _numericValue = num;
                            _lastErrorMessage = null;
                        }
                        else _lastErrorMessage = $"{_invalidMessage}: {message.ErrorMessage}";
                    }
                    break;

                case OscTypes.String:
                    if (str != _stringValue)
                    {
                        OscMessage message = new OscMessage(location, str);
                        if (message.Error == OscPacketError.None)
                        {
                            _sender.Send(message);
                            _stringValue = str;
                            _lastErrorMessage = null;
                        }
                        else _lastErrorMessage = $"{_invalidMessage}: {message.ErrorMessage}";
                    }
                    break;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && _connected) _sender.Dispose();
        }

        private bool TryGetValidAddress(string address, out string error, out IPAddress ipAddress)
        {
            try
            {
                _ipAddress = IPAddress.Parse(address);
            }
            catch (System.FormatException)
            {
                error = $"Failed to parse ip: {address}";
                ipAddress = null;
                return false;
            }
            error = null;
            ipAddress = _ipAddress;
            return true;
        }

        private bool TryConnectOSC(IPAddress ipAddress, int port)
        {
            try
            {
                // the '0' picks a random available outbound (send) port
                // it defaults to "port" otherwise, which break server if it's
                // running on the same IP.
                _sender = new OscSender(ipAddress, 0, port);
                _sender.Connect();
                if (_sender.State == OscSocketState.Connected)
                {
                    _ipAddress = ipAddress;
                    _port = port;
                    return true;
                }
            }
            catch (Exception)
            {
                _lastErrorMessage = $"Failed to connect to {ipAddress}:{port}";
            }
            return false;
        }

        private bool _connected;
        private IPAddress _ipAddress;
        private int _port;
        private float _numericValue;
        private string _stringValue;
        private OscSender _sender;
        private string _lastErrorMessage = null;
        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage() { return _lastErrorMessage; }
    }
}
