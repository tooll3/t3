using System;
using System.Collections.Generic;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public class Slot<T> : ISlot
    {
        public Guid Id { get; set; }
        public Type ValueType { get; }
        public Instance Parent { get; set; }
        public DirtyFlag DirtyFlag { get; set; } = new DirtyFlag();
        
        public T Value; // { get; set; }
        public bool IsMultiInput { get; protected set; } = false;

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

        public void Update(EvaluationContext context)
        {
            if (DirtyFlag.IsDirty)
            {
                _updateAction?.Invoke(context);
                DirtyFlag.Clear();
                DirtyFlag.SetUpdated();
            }
        }

        public void ConnectedUpdate(EvaluationContext context)
        {
            Value = InputConnection[0].GetValue(context);
        }

        public T GetValue(EvaluationContext context)
        {
            Update(context);

            return Value;
        }

        public void AddConnection(ISlot sourceSlot, int index = 0)
        {
            if (!IsConnected)
            {
                UpdateAction = ConnectedUpdate;
                DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                DirtyFlag.Reference = DirtyFlag.Target - 1;
            }

            InputConnection.Insert(index, (Slot<T>)sourceSlot);
        }

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
                // if no connection is set anymore restore the default update action
                SetUpdateActionBackToDefault();
                DirtyFlag.Invalidate();
            }
        }

        public void SetUpdateActionBackToDefault()
        {
            UpdateAction = _defaultUpdateAction;
        }

        public bool IsConnected => InputConnection.Count > 0;

        public ISlot GetConnection(int index)
        {
            return InputConnection[index];
        }

        private List<Slot<T>> _inputConnection = new List<Slot<T>>();

        public List<Slot<T>> InputConnection
        {
            get => _inputConnection;
            set
            {
                _inputConnection = value;
                DirtyFlag.Target++;
            }
        }

        private Action<EvaluationContext> _updateAction;
        public virtual Action<EvaluationContext> UpdateAction { get => _updateAction; set => _updateAction = value; }

        protected Action<EvaluationContext> _defaultUpdateAction;
    }
}