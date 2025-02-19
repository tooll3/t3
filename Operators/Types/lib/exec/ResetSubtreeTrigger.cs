﻿using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_38b85057_fbcb_4ab1_9b40_cfb090750150
{
    public class ResetSubtreeTrigger : Instance<ResetSubtreeTrigger>
    {
        [Output(Guid = "0CF2EF2A-D47A-461A-A7EF-7279C5A17883", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        public ResetSubtreeTrigger()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (Trigger.GetValue(context))
            {
                DirtyFlag.InvalidationRefFrame++;
                Invalidate(Command);
                Trigger.TypedInputValue.Value = false;
                Trigger.Value = false;
            }
            Command.GetValue(context);
        }

        private int Invalidate(ISlot slot)
        {
            if (slot.IsConnected)
            {
                // slot is an output of an composition op
                slot.DirtyFlag.Target = Invalidate(slot.GetConnection(0));
            }
            else
            {
                Instance parent = slot.Parent;

                foreach (var input in parent.Inputs)
                {
                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            var multiInput = (IMultiInputSlot)input;
                            int dirtySum = 0;
                            foreach (var entry in multiInput.GetCollectedInputs())
                            {
                                dirtySum += Invalidate(entry);
                            }

                            input.DirtyFlag.Target = dirtySum;
                        }
                        else
                        {
                            input.DirtyFlag.Target = Invalidate(input.GetConnection(0));
                        }
                    }
                    else
                    {
                        input.DirtyFlag.Invalidate();
                    }
                }

                slot.DirtyFlag.Invalidate();
            }

            return slot.DirtyFlag.Target;
        }

        [Input(Guid = "7CC4E43B-18A2-4564-A511-05EB0D8EC7D2")]
        public readonly InputSlot<Command> Command = new();
        [Input(Guid = "2975F7BE-F21F-4FF4-B477-8FCC19D5F808")]
        public readonly InputSlot<bool> Trigger = new();
    }
}