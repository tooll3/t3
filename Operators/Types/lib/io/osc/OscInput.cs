using System;
using System.Collections.Generic;
//using System.Threading.Channels;
//using System.Windows.Forms;
using Operators.Utils;
using Rug.Osc;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_3a1d7ea0_5445_4df0_b08a_6596e53f815a 
{
    public class OscInput : Instance<OscInput>, OscConnectionManager.IOscConsumer, IDisposable
    {
        [Output(Guid = "5544b675-0de2-4a28-97d0-1a67349152fc", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> FirstResult = new();

        [Output(Guid = "1E2EC3D2-B242-4E6F-8D15-290584315AA9", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> Results = new ();

        [Output(Guid = "8F426B4A-AD49-4AB9-80EE-3DF9F5A5AFF6", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> LastMessageTime = new ();

        public OscInput()
        {
            FirstResult.UpdateAction = Update;
            Results.UpdateAction = Update;
            LastMessageTime.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            _address = Address.GetValue(context);
            
            var newPort = Port.GetValue(context);
            if (newPort != _registeredPort)
            {
                if (_registeredPort != NotRegistered)
                {
                    OscConnectionManager.UnregisterConsumer(this);
                }
                OscConnectionManager.RegisterConsumer(this, newPort);
                _registeredPort = newPort;
            }
            
            _printLogMessages = PrintLogMessages.GetValue(context);

            var teachJustTriggered = MathUtils.WasTriggered(TeachTrigger.GetValue(context), ref _oldTeachTrigger);
            if (teachJustTriggered)
            {
                _teachingActive = true;
                _lastMatchingSignals.Clear();
                _currentControllerValue = 0;
            }

            lock (this)
            {
                foreach (var message in _lastMatchingSignals)
                {
                    if (_teachingActive)
                    {
                        Log.Debug($" connected to OSC address '{message.Address}'", this);
                        Address.TypedInputValue.Value = message.Address;
                        Address.Input.IsDefault = false;
                        Address.DirtyFlag.Invalidate();

                        TeachTrigger.TypedInputValue.Value = false;
                        TeachTrigger.Input.IsDefault = false;
                        TeachTrigger.DirtyFlag.Invalidate();

                        _address = message.Address;
                        _teachingActive = false;
                    }

                    _allResults.Clear();
                    
                    if (message.Count > 0)
                    {
                        foreach (var p in message)
                        {
                            var t = p.GetType();
                            var v = float.NaN;
                            switch (p)
                            {
                                case float f:
                                    v=f;
                                    break;
                                case int i:
                                    v=i;
                                    break;
                                case bool b:
                                    v= b ? 1:0;
                                    break;
                            }
                            _allResults.Add(v);
                        }

                        _currentControllerValue = _allResults.Count > 0
                                                      ? _allResults[0]
                                                      : float.NaN;
                    }
                    
                    _lastMessageTime = Playback.RunTimeInSecs;
                    _isDefaultValue = false;
                }

                _lastMatchingSignals.Clear();
            }
            
            var defaultValue = DefaultOutputValue.GetValue(context);
            FirstResult.Value = _isDefaultValue ? defaultValue : _currentControllerValue;
            Results.Value = _allResults;
            LastMessageTime.Value = (float)_lastMessageTime;
        }

        private double _lastMessageTime;
        private List<float> _allResults = new();

        // Called in other thread!
        public void ProcessMessage(OscMessage msg)
        {
            lock (this)
            {
                if (_printLogMessages)
                {
                    Log.Debug($"Received OSC: {msg.Address}  {msg}", this);
                }

                var matchesAddress = msg.Address == _address;

                if (matchesAddress || _teachingActive)
                {
                    _lastMatchingSignals.Add(msg);
                    _isDefaultValue = false;
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            OscConnectionManager.UnregisterConsumer(this);
        }
        
        
        private const int NotRegistered = -1; 
        private int _registeredPort = NotRegistered;
        private string _address; 
        
        private bool _printLogMessages;
        private bool _isDefaultValue = true;
        private bool _oldTeachTrigger;
        private bool _teachingActive;
        private readonly List<OscMessage> _lastMatchingSignals = new(10);
        private float _currentControllerValue;
        
        [Input(Guid = "87EFD3C4-F2DF-4996-924F-12C631BAD8D8")]
        public readonly InputSlot<int> Port = new();

        [Input(Guid = "17D1FE47-430A-4465-92AA-92A4EFFB515F")]
        public readonly InputSlot<string> Address = new();
        
        [Input(Guid = "4636D6CF-8233-4281-8840-5BA079B5F1A6")]
        public readonly InputSlot<float> DefaultOutputValue = new();
        
        [Input(Guid = "7C681EE6-D071-4284-8585-1C3E03A089EA")]
        public readonly InputSlot<bool> TeachTrigger = new();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new();

        [Input(Guid = "dbd5e806-faa7-4cc0-b997-f00041fe1fd1")]
        public readonly InputSlot<bool> TextMode = new InputSlot<bool>();
    }
}
