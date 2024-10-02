using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;
using T3.Core.Stats;

namespace T3.Core.Operator.Slots
{
    public class Slot<T> : ISlot
    {
        public Guid Id { get; set; }
        public Type ValueType { get; }
        public Instance Parent { get; set; }
        public DirtyFlag DirtyFlag { get; set; } = new();
        
        public T Value; // { get; set; }
        public bool IsMultiInput { get; protected set; } = false;

        protected bool _isDisabled;

        protected virtual void SetDisabled(bool shouldBeDisabled)
        {
            if (shouldBeDisabled == _isDisabled)
                return;

            if (shouldBeDisabled)
            {
                if (_keepOriginalUpdateAction != null)
                {
                    Log.Warning("Is already bypassed or disabled");
                    return;
                }
                
                _keepOriginalUpdateAction = _updateAction;
                _keepDirtyFlagTrigger = DirtyFlag.Trigger;
                UpdateAction = EmptyAction;
                DirtyFlag.Invalidate();
            }
            else
            {
                RestoreUpdateAction();
            }

            _isDisabled = shouldBeDisabled;
        }

        public virtual bool TrySetBypassToInput(Slot<T> targetSlot)
        {
            if (_keepOriginalUpdateAction != null)
            {
                //Log.Warning("Already disabled or bypassed");
                return false;
            }
            
            _keepOriginalUpdateAction = UpdateAction;
            _keepDirtyFlagTrigger = DirtyFlag.Trigger;
            UpdateAction = ByPassUpdate;
            DirtyFlag.Invalidate();
            _targetInputForBypass = targetSlot;
            return true;
        }

        public void OverrideWithAnimationAction(Action<EvaluationContext> newAction)
        {
            // Animation actions are updated regardless if operator was already animated
            if (_keepOriginalUpdateAction == null)
            {
                _keepOriginalUpdateAction = UpdateAction;
                _keepDirtyFlagTrigger = DirtyFlag.Trigger;
            }

            UpdateAction = newAction;
            DirtyFlag.Invalidate();
        }
        
        public Action<EvaluationContext> GetUpdateAction()
        {
            return _updateAction;
        }
        
        public virtual void RestoreUpdateAction()
        {
            // This will happen when operators are recompiled and output slots are disconnected
            if (_keepOriginalUpdateAction == null)
            {
                UpdateAction = null;
                return;
            }
            
            UpdateAction = _keepOriginalUpdateAction;
            _keepOriginalUpdateAction = null;
            DirtyFlag.Trigger = _keepDirtyFlagTrigger;
            DirtyFlag.Invalidate();
        }

        public bool IsDisabled 
        {
            get => _isDisabled;
            set => SetDisabled(value);
        }

        protected void EmptyAction(EvaluationContext context) { }

        public Slot()
        {
            // UpdateAction = Update;
            ValueType = typeof(T);
        }

        public Slot(Action<EvaluationContext> updateAction) : this()
        {
            UpdateAction = updateAction;
        }

        public Slot(Action<EvaluationContext> updateAction, T defaultValue) : this()
        {
            UpdateAction = updateAction;
            Value = defaultValue;
        }

        public Slot(T defaultValue) : this()
        {
            Value = defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(EvaluationContext context)
        {
            if (DirtyFlag.IsDirty || ValueType == typeof(Command))
            {
                OpUpdateCounter.CountUp();
                _updateAction?.Invoke(context);
                DirtyFlag.Clear();
                DirtyFlag.SetUpdated();
            }
        }

        public void ConnectedUpdate(EvaluationContext context)
        {
            Value = InputConnection[0].GetValue(context);
        }
        
        public void ByPassUpdate(EvaluationContext context)
        {
            Value = _targetInputForBypass.GetValue(context);
        }

        public T GetValue(EvaluationContext context)
        {
            Update(context);

            return Value;
        }

        public void AddConnection(ISlot sourceSlot, int index = 0)
        {
            if (!IsConnected && sourceSlot != null)
            {
                if (UpdateAction != null)
                {
                    _actionBeforeAddingConnecting = UpdateAction;
                    if (Parent.Children.Count > 0 && Parent is ICompoundWithUpdate compoundWithUpdate && this is not IInputSlot)
                    {
                        //Log.Debug($"Skipping connection for compound op with update method for {Parent.Symbol} {this}", compoundWithUpdate);
                        //compoundWithUpdate.RegisterOutputUpdateAction(this, ConnectedUpdate);
                        InputConnection.Insert(index, (Slot<T>)sourceSlot);
                        DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                        DirtyFlag.Reference = DirtyFlag.Target - 1;
                        return;
                    }
                }
                UpdateAction = ConnectedUpdate;
                DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                DirtyFlag.Reference = DirtyFlag.Target - 1;
            }
            
            if (sourceSlot == null)
                return;
            
            if (sourceSlot.ValueType != ValueType)
            {
                Log.Warning("Type mismatch during connection");
                return;
            }
            InputConnection.Insert(index, (Slot<T>)sourceSlot);
            
        }

        private Action<EvaluationContext> _actionBeforeAddingConnecting;

        public void RemoveConnection(int index = 0)
        {
            if (IsConnected)
            {
                if (index < InputConnection.Count)
                {
                    InputConnection.RemoveAt(index);
                }
                else
                {
                    Log.Error($"Trying to delete connection at index {index}, but input slot only has {InputConnection.Count} connections");
                }
            }

            if (!IsConnected)
            {
                if (_actionBeforeAddingConnecting != null)
                {
                    UpdateAction = _actionBeforeAddingConnecting;
                }
                else
                {
                    // if no connection is set anymore restore the default update action
                    RestoreUpdateAction();
                }
                DirtyFlag.Invalidate();
            }
        }



        public bool IsConnected => InputConnection.Count > 0;

        public ISlot GetConnection(int index)
        {
            return InputConnection[index];
        }

        private List<Slot<T>> _inputConnection = new();

        public List<Slot<T>> InputConnection => _inputConnection;

        public virtual int Invalidate()
        {
            if (DirtyFlag.IsAlreadyInvalidated || DirtyFlag.HasBeenVisited)
                return DirtyFlag.Target;
            
            if (this is IInputSlot)
            {
                if (IsConnected)
                {
                    DirtyFlag.Target = GetConnection(0).Invalidate();
                }
                else if (DirtyFlag.Trigger != DirtyFlagTrigger.None)
                {
                    DirtyFlag.Invalidate();
                }
            }
            else if (IsConnected)
            {
                // slot is an output of an composition op
                DirtyFlag.Target = GetConnection(0).Invalidate();
            }
            
            else
            {
                Instance parent = Parent;
                
                
                bool outputDirty = DirtyFlag.IsDirty;
                foreach (var input in parent.Inputs)
                {
                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            // NOTE: In situations with extremely large graphs (1000 of instances)
                            // invalidation can become bottle neck. In these cases it might be justified
                            // to limit the invalidation to "active" parts of the subgraph. The [Switch]
                            // operator defines this list.
                            if (input.LimitMultiInputInvalidationToIndices != null)
                            {
                                var multiInput = (IMultiInputSlot)input;
                                var dirtySum = 0;
                                var index = 0;
                                
                                foreach (var entry in multiInput.GetCollectedInputs())
                                {
                                    if (!input.LimitMultiInputInvalidationToIndices.Contains(index++))
                                        continue;
                                    
                                    dirtySum += entry.Invalidate();
                                }

                                input.DirtyFlag.Target = dirtySum;
                                
                            }
                            else
                            {
                                var multiInput = (IMultiInputSlot)input;
                                int dirtySum = 0;
                                foreach (var entry in multiInput.GetCollectedInputs())
                                {
                                    dirtySum += entry.Invalidate();
                                }

                                input.DirtyFlag.Target = dirtySum;
                            }
                        }
                        else
                        {
                            input.DirtyFlag.Target = input.GetConnection(0).Invalidate();
                        }
                    }
                    else if ((input.DirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                    {
                        input.DirtyFlag.Invalidate();
                    }

                    input.DirtyFlag.SetVisited();
                    outputDirty |= input.DirtyFlag.IsDirty;
                }

                if (outputDirty || (DirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                {
                    DirtyFlag.Invalidate();
                }
            }

            DirtyFlag.SetVisited();
            return DirtyFlag.Target;
        }

        private Action<EvaluationContext> _updateAction;
        public virtual Action<EvaluationContext> UpdateAction { get => _updateAction; set => _updateAction = value; }

        protected Action<EvaluationContext> _keepOriginalUpdateAction;
        private DirtyFlagTrigger _keepDirtyFlagTrigger;
        protected Slot<T> _targetInputForBypass;
    }

    
    
}