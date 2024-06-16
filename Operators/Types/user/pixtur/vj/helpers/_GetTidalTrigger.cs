using System;
using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8fb63c4d_80a8_4023_b55b_7f97bffbee48
{
    public class _GetTidalTrigger : Instance<_GetTidalTrigger>
                                  , IStatusProvider, ICustomDropdownHolder
    {
        [Output(Guid = "a4121952-5c82-4237-8e9f-913b83c6273b", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Note = new(0f);

        [Output(Guid = "084E6671-1932-451B-9DA0-4A474844AD27", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> WasTrigger = new();

        public _GetTidalTrigger()
        {
            Note.UpdateAction = Update;
            WasTrigger.UpdateAction = Update;
        }

        private double _lastUpdateTime;

        private void Update(EvaluationContext context)
        {
            if(Math.Abs(context.LocalFxTime - _lastUpdateTime) < 0.001f)
                return;
            
            _lastUpdateTime = context.LocalFxTime;
            
            _dict = DictionaryInput.GetValue(context);
            
            var useNotesForBeats = UseNotesForBeats.GetValue(context);
            var id = Id.GetValue(context);
            var channel = Channel.GetValue(context);
            
            var path = useNotesForBeats ? $"/dirt/play/{id}/"
                : $"/dirt/play/{id}/{channel}/";

            if (_dict == null)
            {
                SetStatus("No dictionary input", IStatusProvider.StatusLevel.Warning);
                return;
            }

            
            var notePath = path + NoteChannel.GetValue(context);
            var cyclePath = path + CycleChannel.GetValue(context);

            if (
                _dict.TryGetValue(notePath, out var note)
                && _dict.TryGetValue(cyclePath, out var cycle))
            {
                if (useNotesForBeats)
                {
                    if ($"{note}" == channel)
                    {
                        Note.Value = note;
                        WasTrigger.Value = cycle > _lastCycle;
                        _lastCycle = cycle;
                        Log.Debug($"found beat {notePath} '{note}'  '{channel}' " );
                    }
                }
                else
                {
                    Note.Value = note;
                    WasTrigger.Value = cycle > _lastCycle;
                    _lastCycle = cycle;
                }
                SetStatus(null, IStatusProvider.StatusLevel.Success);
            }
            else
            {
                SetStatus($"Key not found: {path}", IStatusProvider.StatusLevel.Warning);
            }

            Note.DirtyFlag.Clear();
            WasTrigger.DirtyFlag.Clear();
        }

        private float _lastCycle = 0;

        private Dict<float> _dict;

        #region implement status provider
        private void SetStatus(string message, IStatusProvider.StatusLevel level)
        {
            _lastWarningMessage = message;
            _statusLevel = level;
        }

        #region select dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return Select.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != Select.Id || _dict == null)
            {
                yield return "";
                yield break;
            }

            foreach (var key in _dict.Keys)
            {
                yield return key;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            Select.SetTypedInputValue(result);
        }
        #endregion

        public IStatusProvider.StatusLevel GetStatusLevel() => _statusLevel;
        public string GetStatusMessage() => _lastWarningMessage;

        private string _lastWarningMessage = "Not updated yet.";
        private IStatusProvider.StatusLevel _statusLevel;
        #endregion

        [Input(Guid = "eddeab43-5d7e-4f42-abb7-7909c9a7212e")]
        public readonly InputSlot<Dict<float>> DictionaryInput = new();

        [Input(Guid = "E594E270-B748-4B96-AB19-9A0D30CDBCCA")]
        public readonly InputSlot<string> NoteChannel = new();

        [Input(Guid = "FF5A9352-8BFA-4D73-9992-306AF55213AE")]
        public readonly InputSlot<string> CycleChannel = new();

        [Input(Guid = "fac3cc60-2a58-4106-a258-3798df04455a")]
        public readonly InputSlot<string> Select = new();
        

        [Input(Guid = "FB11C81B-931A-4E3C-9E1C-9A287C2F64A1")]
        public readonly InputSlot<string> Id = new();

        [Input(Guid = "D16A364D-2466-451C-B639-70FBA4C3357A")]
        public readonly InputSlot<string> Channel = new();


        [Input(Guid = "8E355D24-4934-4008-990D-76448A647281")]
        public readonly InputSlot<bool> UseNotesForBeats = new();

    }
}