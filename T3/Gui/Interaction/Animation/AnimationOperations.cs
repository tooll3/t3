using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Gui.Commands;

namespace T3.Gui.Windows.TimeLine
{
    public class AnimationOperations
    {
        public static List<VDefinition> InsertKeyframeToCurves(IEnumerable<Curve> curves, double time, float increment = 0)
        {
            var commands = new List<ICommand>();
            
            var newKeyframes = new List<VDefinition>(4);
            foreach (var curve in curves)
            {
                var value = curve.GetSampledValue(time);
                var previousU = curve.GetPreviousU(time);

                var key = (previousU != null)
                              ? curve.GetV(previousU.Value).Clone()
                              : new VDefinition();

                key.Value = value + increment;
                key.U = time;

                var command = new AddKeyframesCommand(curve, key);
                command.Do();
                commands.Add(command);
                newKeyframes.Add(key);
            }
            
            var marcoCommand = new MacroCommand("Insert Keyframe", commands);
            UndoRedoStack.Add(marcoCommand);
            
            return newKeyframes;
        }

        public static void RemoveKeyframeFromCurves(IEnumerable<Curve> curves, double time)
        {
            var commands = new List<ICommand>();
            foreach (var curve in curves)
            {
                var key = curve.GetV(time);
                if (key != null)
                {
                    var command = new DeleteKeyframesCommand(curve, key);
                    commands.Add(command);
                }
            }
            UndoRedoStack.AddAndExecute(new MacroCommand("Delete keyframes", commands));
        }

        public static void DeleteSelectedKeyframesFromAnimationParameters(HashSet<VDefinition> selectedKeyframes, IEnumerable<TimeLineCanvas.AnimationParameter> animationParameters)
        {
            var commands = new List<ICommand>();
            
            foreach (var param in animationParameters)
            {
                foreach (var curve in param.Curves)
                {
                    foreach (var keyframe in curve.GetVDefinitions().ToList())
                    {
                        if(!selectedKeyframes.Contains(keyframe))
                            continue;
                        
                        var command = new DeleteKeyframesCommand(curve, keyframe);
                        commands.Add(command);
                        selectedKeyframes.Remove(keyframe);
                    }
                }
            }
            
            UndoRedoStack.AddAndExecute(new MacroCommand("Delete keyframes", commands));
        }
    }
}