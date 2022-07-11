using System.Collections.Generic;
//using System.Threading.Channels;
//using System.Windows.Forms;
using Operators.Utils;
using Rug.Osc;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3a1d7ea0_5445_4df0_b08a_6596e53f815a 
{
    public class OscInput : Instance<OscInput>, OscConnectionManager.IOscConsumer
    {
        [Output(Guid = "5544b675-0de2-4a28-97d0-1a67349152fc", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        public OscInput()
        {
            Result.UpdateAction = Update;
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
                        Log.Debug($" connected to OSC address '{message.Address}'");
                        Address.TypedInputValue.Value = message.Address;
                        Address.Input.IsDefault = false;
                        Address.DirtyFlag.Invalidate();

                        TeachTrigger.TypedInputValue.Value = false;
                        TeachTrigger.Input.IsDefault = false;
                        TeachTrigger.DirtyFlag.Invalidate();

                        _address = message.Address;
                        _teachingActive = false;
                    }

                    if (message.Count > 0)
                    {
                        var p = message[0];
                        var t = p.GetType();
                        if (p is float f)
                        {
                            _currentControllerValue = f;
                        }
                    }
                    
                    LastMessageTime = Playback.RunTimeInSecs;
                    _isDefaultValue = false;
                }

                _lastMatchingSignals.Clear();
            }

            if (_isDefaultValue)
            {
                Result.Value = DefaultOutputValue.GetValue(context);
                return;
            }

            Result.Value = _currentControllerValue;
        }

        public double LastMessageTime { get; set; }

        // Called in other thread!
        public void ProcessMessage(OscMessage msg)
        {
            lock (this)
            {
                if (_printLogMessages)
                {
                    Log.Debug($" would process message for address {_address}");

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
        
        
        

    }
}
