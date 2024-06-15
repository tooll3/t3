using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Operators.Utils;
using Rug.Osc;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3a1d7ea0_5445_4df0_b08a_6596e53f815a
{
    public class OscInput : Instance<OscInput>, OscConnectionManager.IOscConsumer, IStatusProvider, ICustomDropdownHolder
    {
        [Output(Guid = "F697732E-46F3-4037-AFC5-56F396BD70AD")]
        public readonly Slot<Dict<float>> Contents = new();

        [Output(Guid = "1E2EC3D2-B242-4E6F-8D15-290584315AA9")]
        public readonly Slot<List<float>> Values = new();

        [Output(Guid = "3291E15A-1900-4252-8591-D016281527F0", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> WasTrigger = new();


        public OscInput()
        {
            Values.UpdateAction = Update;
            Contents.UpdateAction = Update;
            
            WasTrigger.UpdateAction = AnimatedUpdate;
        }

        
        private void Update(EvaluationContext context)
        {
            var useKeyValuePairs = UseKeyValuePairs.GetValue(context);
            if (useKeyValuePairs != _useKeyValuePairs)
            {
                _valuesByKeys.Clear();
                _useKeyValuePairs = useKeyValuePairs;
            }

            // Update address and give connectivity status 
            _filterKey = FilterKey.GetValue(context);
            
            var filterPattern = Pattern.GetValue(context);
            if(filterPattern != _filterPattern)
            {
                _filterPattern = filterPattern;
                if (!string.IsNullOrEmpty(filterPattern))
                {
                    try
                    {
                        _filterRegex = new Regex(filterPattern);
                    }
                    catch (Exception e)
                    {
                        SetStatus("Invalid regex pattern: " + e.Message, IStatusProvider.StatusLevel.Warning);
                        _filterRegex = null;
                        return;
                    }
                }
                
                _valuesByKeys.Clear();
            }
            

            if (Address.DirtyFlag.IsDirty || Port.DirtyFlag.IsDirty)
                UpdateStatusMessage();
            
            var newAddress = Address.GetValue(context);
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

            // Update output
            _printLogMessages = PrintLogMessages.GetValue(context);

            Values.Value = _collectedFloatResults;
            Contents.Value = _valuesByKeys;
            _wasTrigger = true;

            // Prevent updates from multiple connected outputs
            Values.DirtyFlag.Clear();
            Contents.DirtyFlag.Clear();
        }

        /// <summary>
        /// WasHit needs to send true and false to trigger events in connected operators.
        /// To avoid updating the rest of the operator on every frame, we have have this
        /// alternative update method that only sends the trigger value. 
        /// </summary>
        private void AnimatedUpdate(EvaluationContext context)
        {
            WasTrigger.Value = _wasTrigger;
            _wasTrigger = false;
        }
        private bool _wasTrigger;

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

        # region handling async messages from other thread
        
        public void ProcessMessage(OscMessage msg)
        {
            lock (this)
            {
                if (_printLogMessages)
                {
                    Log.Debug($"Received OSC: {msg}", this);
                }

                if (!string.IsNullOrEmpty(_address) && !msg.Address.StartsWith(_address))
                    return;

                if (!ParseMessages(msg))
                    return;
                
                SetStatus(string.Empty, IStatusProvider.StatusLevel.Success);
                FlagAsDirty();
            }
        }
        
        /// <summary>
        /// This will cause Update to be called on next frame 
        /// </summary>
        private void FlagAsDirty()
        {
            Contents.DirtyFlag.Invalidate();
            Values.DirtyFlag.Invalidate();
        }
        
        private bool ParseMessages(OscMessage m)
        {
            if (m.Count == 0)
                return false;

            _collectedFloatResults.Clear();
            if (_useKeyValuePairs)
            {
                if (m.Count % 2 != 0)
                {
                    SetStatus("Osc message has odd number of elements, can't be used as key value pairs",
                              IStatusProvider.StatusLevel.Warning);
                    return false;
                }

                if (!string.IsNullOrEmpty(_filterKey))
                {
                    var foundMatch = false;
                    for (var index = 0; index < m.Count; index += 2)
                    {
                        if (m[index] is not string key)
                            continue;

                        if (key != _filterKey)
                            continue;

                        var valueAsString = m[index + 1].ToString();

                        if (valueAsString == null)
                            continue;

                        if (_filterRegex != null)
                        {
                            if (!_filterRegex.IsMatch(valueAsString))
                                continue;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(valueAsString))
                                continue;
                        }

                        // if (valueAsString != _filterPattern)
                        //     continue;

                        foundMatch = true;
                        break;
                    }

                    if (!foundMatch)
                    {
                        if (_printLogMessages)
                        {
                            Log.Debug($"Skipping OSC message not matching {_filterKey} == {_filterPattern}", this);
                            return false;
                        }
                    }
                }

                for (var index = 0; index < m.Count; index += 2)
                {
                    if (m[index] is not string key)
                    {
                        SetStatus("Expected key but got " + m[index].GetType().Name,
                                  IStatusProvider.StatusLevel.Warning);
                        break;
                    }

                    if (OscConnectionManager.TryGetFloatFromMessagePart(m[index + 1], out var floatValue))
                    {
                        _valuesByKeys[m.Address + "/" + key] = floatValue;
                    }

                    _collectedFloatResults.Add(floatValue);
                }
            }
            else
            {
                for (var index = 0; index < m.Count; index++)
                {
                    if (OscConnectionManager.TryGetFloatFromMessagePart(m[index], out var floatValue))
                    {
                        var path = m.Count == 1
                                       ? OscConnectionManager.BuildMessageComponentPath(m)
                                       : OscConnectionManager.BuildMessageComponentPath(m, index);
                        _valuesByKeys[path] = floatValue;
                    }

                    _collectedFloatResults.Add(floatValue);
                }
            }

            return true;
        }
        # endregion



        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            OscConnectionManager.UnregisterConsumer(this);
        }

        private static string GetLocalIpAddress()
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

        private bool _useKeyValuePairs;
        private string _filterKey;
        private string _filterPattern;
        private Regex _filterRegex;
        
        private readonly List<float> _collectedFloatResults = new(10);
        private readonly Dict<float> _valuesByKeys = new(0f);

        private const int UndefinedPortId = -1;
        private int _registeredPort = UndefinedPortId;
        private string _address;

        private bool _printLogMessages;

        [Input(Guid = "87EFD3C4-F2DF-4996-924F-12C631BAD8D8")]
        public readonly InputSlot<int> Port = new();

        [Input(Guid = "17D1FE47-430A-4465-92AA-92A4EFFB515F")]
        public readonly InputSlot<string> Address = new();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new();

        [Input(Guid = "8014A7A6-CACB-4206-A5B4-87C14235A20C")]
        public readonly InputSlot<bool> UseKeyValuePairs = new();

        [Input(Guid = "8E5D30A3-5878-4F64-9EB4-AD5782A957BF")]
        public readonly InputSlot<string> FilterKey = new();

        [Input(Guid = "DBF1C777-D399-49BC-ACB0-335CD1F7FA81")]
        public readonly InputSlot<string> Pattern = new();

        private const string Separator = " - ";
    }
}