using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.exec.context
{
	[Guid("470db771-c7f2-4c52-8897-d3a9b9fc6a4e")]
    public class GetIntVar : Instance<GetIntVar>
    {
        [Output(Guid = "B306B216-630C-4611-90FD-52FF322EBD00", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Result = new();

        public GetIntVar()
        {
            Result.UpdateAction += Update;
            VariableName.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
        }

        private void Update(EvaluationContext context)
        {
            var logUpdates = LogUpdates.GetEnumValue<LogLevels>(context);
            var fallbackValue = FallbackValue.GetValue(context);
            var variableName = VariableName.GetValue(context);

            if (context.IntVariables.TryGetValue(variableName, out int currentValue))
            {
                Result.Value = currentValue;

                if ((int)logUpdates >= (int)LogLevels.AllUpdates)
                    Log.Debug($"int {variableName} is {currentValue}", this);
            }
            else
            {
                Result.Value = fallbackValue;

                var complain = ((int)logUpdates >= (int)LogLevels.Warnings && !_complainedOnce)
                               || logUpdates == LogLevels.AllUpdates;

                if (!complain)
                    return;

                Log.Warning($"Can't read undefined int {variableName}.", this);
                _complainedOnce = true;
            }
        }

        private bool _complainedOnce;

        [Input(Guid = "d7662b65-f249-4887-a319-dc2cf7d192f2")]
        public readonly InputSlot<string> VariableName = new();

        [Input(Guid = "C78E0D72-1296-430C-98FB-078CBE2E9596")]
        public readonly InputSlot<int> FallbackValue = new();

        [Input(Guid = "70EF2D92-CD3E-4C14-BA9C-E7E29B2478C8", MappedType = typeof(LogLevels))]
        public readonly InputSlot<int> LogUpdates = new();

        private enum LogLevels
        {
            None,
            Warnings,
            Changes,
            AllUpdates,
        }
    }
}