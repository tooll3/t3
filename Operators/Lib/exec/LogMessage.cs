using System.Runtime.InteropServices;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.exec
{
	[Guid("53127485-e2c7-4be8-aff2-da5790799593")]
    public class LogMessage : Instance<LogMessage>
    {
        [Output(Guid = "8d6a6d3d-c814-41bc-b770-5a54dfdeb6a2", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        public LogMessage()
        {
            Output.UpdateAction += Update;
        }
        
        private void Update(EvaluationContext context)
        {
            var message = Message.GetValue(context);
            var logLevel = LogLevel.GetEnumValue<LogLevels>(context);
            if (logLevel > (int)LogLevels.None)
            {
                Log.Debug($"{new string(' ', _nestingLevel * 4)} { (string.IsNullOrEmpty( message) ? fallbackMessage : message)  }  @{context.LocalFxTime:0.00}b  {_dampedPreviousUpdateDuration*1000.0:0.00}ms", this);
            }

            _nestingLevel++;
            
            var startTime = Playback.RunTimeInSecs;
            SubGraph.GetValue(context);
            var updateDuration = Playback.RunTimeInSecs - startTime;
            _dampedPreviousUpdateDuration = MathUtils.Lerp(_dampedPreviousUpdateDuration, updateDuration, 0.01);
            
            _nestingLevel--;

            if ((int)logLevel >= (int)LogLevels.UpdateTime)
            {
                Log.Debug($"{new string(' ', _nestingLevel)} Update took {updateDuration*1000.0:0.00}ms", this);
            }
        } 

        private static readonly string fallbackMessage = "Log";

        private double _dampedPreviousUpdateDuration = 0;
        

        [Input(Guid = "183cd865-7939-4110-8192-f112fff3cc60")]
        public readonly InputSlot<Command> SubGraph = new();

        [Input(Guid = "acd53819-c248-4c95-a1a5-92a583e9b49b")]
        public readonly InputSlot<string> Message = new();
        
        [Input(Guid = "42a673ee-c323-401c-b5d9-8d8f9dbebc2b", MappedType = typeof(LogLevels))]
        public readonly InputSlot<int> LogLevel = new();

        private static int _nestingLevel = 0;
        
        private enum LogLevels
        {
            None,
            Messages,
            UpdateTime,
        }
    }
}