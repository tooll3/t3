using System;
using System.Collections.Generic;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_d34f9b46_57c1_445a_a0e9_74ff99fd6924
{
    public class BranchRenderPasses : Instance<BranchRenderPasses>
    {
        [Output(Guid = "d566da95-6869-4b39-a92b-4ea8396c5f8a")]
        public readonly Slot<Command> Output = new();

        [Output(Guid = "4771b93d-b5bc-472f-9b03-45d51f3d2007")]
        public readonly Slot<int> Count = new();
        
        public BranchRenderPasses()
        {
            Output.UpdateAction = Update;
            Count.UpdateAction = UpdateCount;
        }

        private void UpdateCount(EvaluationContext context)
        {
            var commands = Commands.GetCollectedTypedInputs();
            if (commands == null)
            {
                Count.Value = 0;
                return;
            }

            Count.Value = commands.Count;
        }

        private void Update(EvaluationContext context)
        {
            var commands = Commands.GetCollectedTypedInputs();
            var index = Index.GetValue(context);

            if (commands.Count == 0 || index == -1)
            {
                Count.Value = 0;
                return;
            }
            
            // Do all
            _activeIndices.Clear();
            if (index == -2)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    _activeIndices.Add(i);
                    commands[i].GetValue(context); 
                }

                for (int i = 0; i < commands.Count; i++)
                {
                    _activeIndices.Add(i);
                    commands[i].Value?.RestoreAction?.Invoke(context);
                }
            }
            else
            {
                index %= commands.Count;
                if (index < 0)
                {
                    index += commands.Count;
                }
                
                _activeIndices.Add(index);
                commands[index].GetValue(context); 
                commands[index].Value?.RestoreAction?.Invoke(context);
            }

            Commands.LimitMultiInputInvalidationToIndices = OptimizeInvalidation.GetValue(context) 
                                                                ? _activeIndices 
                                                                : null;
            Count.Value = commands.Count;
        }

        private readonly List<int> _activeIndices = new();

        [Input(Guid = "b4efb2d5-7de1-4edd-9df1-e79474506092")]
        public readonly MultiInputSlot<Command> Commands = new();
        
        [Input(Guid = "d002a04f-72eb-43f6-bdad-930dad774e02")]
        public readonly InputSlot<int> Index = new();
        
        [Input(Guid = "c38203c8-58e1-4735-b047-67aa1ddbe83a")]
        public readonly InputSlot<bool> OptimizeInvalidation = new();
        
        [Input(Guid = "ABB57FDB-687A-4425-B1BA-D1F14057526F")]
        public readonly InputSlot<int> ShadowMode = new();


        private enum ShadowBehaviours
        {
            Ignore,
            CastAndReceive,
            OnlyCast,
            OnlyReceive,
        }
    }
}