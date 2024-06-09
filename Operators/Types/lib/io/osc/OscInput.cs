using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
//using System.Threading.Channels;
//using System.Windows.Forms;
using Operators.Utils;
using Rug.Osc;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_3a1d7ea0_5445_4df0_b08a_6596e53f815a
{
    public class OscInput : Instance<OscInput>, OscConnectionManager.IOscConsumer, IStatusProvider, ICustomDropdownHolder
    {
        [Output(Guid = "F697732E-46F3-4037-AFC5-56F396BD70AD", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Dict<float>> Values = new();

        [Output(Guid = "5544b675-0de2-4a28-97d0-1a67349152fc")]
        public readonly Slot<float> FirstResult = new();

        [Output(Guid = "1E2EC3D2-B242-4E6F-8D15-290584315AA9")]
        public readonly Slot<List<float>> Results = new();

        [Output(Guid = "8F426B4A-AD49-4AB9-80EE-3DF9F5A5AFF6")]
        public readonly Slot<float> LastMessageTime = new();

        public OscInput()
        {
            FirstResult.UpdateAction = Update;
            Results.UpdateAction = Update;
            LastMessageTime.UpdateAction = Update;
            Values.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var statusNeedsUpdate = Address.DirtyFlag.IsDirty || Port.DirtyFlag.IsDirty;
            var newAddress =Address.GetValue(context);
            if (newAddress != _address)
            {
                _valuesByKeys.Clear();
                _address = newAddress;
            } 

            var newPort = Port.GetValue(context);
            if (newPort != _registeredPort)
            {
                _valuesByKeys.Clear();

                if (newPort < 0 || newPort > 65535)
                {
                    SetStatus("Invalid port number", IStatusProvider.StatusLevel.Warning);
                    return;
                }

                if (_registeredPort != UndefinedPortId)
                {
                    OscConnectionManager.UnregisterConsumer(this);
                }

                OscConnectionManager.RegisterConsumer(this, newPort);
                _registeredPort = newPort;
            }

            _printLogMessages = PrintLogMessages.GetValue(context);

            lock (this)
            {
                if (_lastMatchingSignals.Count > 0)
                {
                    _allResults.Clear();
                    foreach (var m in _lastMatchingSignals)
                    {

                        if (m.Count == 0)
                            continue;

                        for (var index = 0; index < m.Count; index++)
                        {
                            var obj = m[index];
                            var floatValue = obj switch
                                        {
                                            float f => f,
                                            int i   => i,
                                            bool b  => b ? 1 : 0,
                                            _       => float.NaN
                                        };
                            
                            if (!float.IsNaN(floatValue))
                            {
                                const string channels="xyzw";
                                var suffix = index < 4 ? channels[index].ToString() : index.ToString(); 
                                var address = m.Address + "." + suffix;
                                _valuesByKeys[address] = floatValue;
                            }
                            
                            _allResults.Add(floatValue);
                        }

                        _currentControllerValue = _allResults.Count > 0
                                                      ? _allResults[0]
                                                      : float.NaN;

                        _lastMessageTime = Playback.RunTimeInSecs;
                        _isDefaultValue = false;
                    }

                    _lastMatchingSignals.Clear();
                }
            }

            FirstResult.Value = _isDefaultValue 
                                    ? DefaultOutputValue.GetValue(context) 
                                    : _currentControllerValue;
            Results.Value = _allResults;
            LastMessageTime.Value = (float)_lastMessageTime;
            Values.Value = _valuesByKeys;

            if (statusNeedsUpdate)
                UpdateStatusMessage();

            FirstResult.DirtyFlag.Clear();
            LastMessageTime.DirtyFlag.Clear();
            Results.DirtyFlag.Clear();
            Values.DirtyFlag.Clear();
        }

        private void UpdateStatusMessage()
        {
            // Update status message
            var ipAddress = GetLocalIpAddress();
            var portIsActive = OscConnectionManager.TryGetScannedAddressesForPort(_registeredPort, out var addresses) && addresses.Count > 0;
            var addressIsDefined = !string.IsNullOrEmpty(_address);
            var addressIsActive = addressIsDefined && portIsActive && addresses.ContainsKey(_address);

            if (addressIsActive)
            {
                SetStatus(string.Empty, IStatusProvider.StatusLevel.Success);
                return;
            }

            if (addressIsDefined && portIsActive)
            {
                SetStatus($"No messages for {_address} on {_registeredPort}.", IStatusProvider.StatusLevel.Warning);
                return;
            }

            if (portIsActive)
            {
                SetStatus("Please use dropdown to pick an active address.", IStatusProvider.StatusLevel.Warning);
                return;
            }

            SetStatus($"Listening on {ipAddress}:{_registeredPort}\nNo messages received, yet.", IStatusProvider.StatusLevel.Notice);
        }

        // Called in other thread!
        public void ProcessMessage(OscMessage msg)
        {
            lock (this)
            {
                if (_printLogMessages)
                {
                    Log.Debug($"Received OSC: {msg}", this);
                }

                if (string.IsNullOrEmpty(_address) || msg.Address.StartsWith(_address))
                {
                    _lastMatchingSignals.Add(msg);
                    _isDefaultValue = false;
                }

                SetStatus(string.Empty, IStatusProvider.StatusLevel.Success);
                FlagAsDirty();
            }
        }

        /// <summary>
        /// This will cause Update to be called on next frame 
        /// </summary>
        private void FlagAsDirty()
        {
            FirstResult.DirtyFlag.Invalidate();
            Results.DirtyFlag.Invalidate();
            LastMessageTime.DirtyFlag.Invalidate();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            OscConnectionManager.UnregisterConsumer(this);
        }

        private string GetLocalIpAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }

            return "unknown IP";
        }

        #region implement status provider
        private void SetStatus(string message, IStatusProvider.StatusLevel level)
        {
            _lastWarningMessage = message;
            _statusLevel = level;
        }

        public IStatusProvider.StatusLevel GetStatusLevel() => _statusLevel;
        public string GetStatusMessage() => _lastWarningMessage;

        private string _lastWarningMessage = "Not updated yet.";
        private IStatusProvider.StatusLevel _statusLevel;
        #endregion

        #region address dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return Address.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != Address.Id)
            {
                yield return "undefined";
                yield break;
            }

            if (!OscConnectionManager.TryGetScannedAddressesForPort(Port.Value, out var addresses))
            {
                Log.Warning("No addresses found for port " + Port.Value);
                yield break;
            }

            var listOfOrderedKeyValuePairs = addresses.OrderBy(x => x.Key).ToList();

            foreach (var (address, type) in listOfOrderedKeyValuePairs)
            {
                yield return $"{address}{Separator}<{type}>";
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            var parts = result.Split(Separator);
            if (parts.Length > 1)
            {
                Address.SetTypedInputValue(parts[0]);
            }
            else
            {
                Address.SetTypedInputValue(result);
            }
        }
        #endregion

        private double _lastMessageTime;
        private readonly List<float> _allResults = new();

        private readonly Dict<float> _valuesByKeys = new(0f);

        private const int UndefinedPortId = -1;
        private int _registeredPort = UndefinedPortId;
        private string _address;

        private bool _printLogMessages;
        private bool _isDefaultValue = true;
        private readonly List<OscMessage> _lastMatchingSignals = new(10);
        private float _currentControllerValue;

        [Input(Guid = "87EFD3C4-F2DF-4996-924F-12C631BAD8D8")]
        public readonly InputSlot<int> Port = new();

        [Input(Guid = "17D1FE47-430A-4465-92AA-92A4EFFB515F")]
        public readonly InputSlot<string> Address = new();

        [Input(Guid = "4636D6CF-8233-4281-8840-5BA079B5F1A6")]
        public readonly InputSlot<float> DefaultOutputValue = new();

        // [Input(Guid = "7C681EE6-D071-4284-8585-1C3E03A089EA")]
        // public readonly InputSlot<bool> IsScanning = new();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new();

        private const string Separator = " - ";
    }
}