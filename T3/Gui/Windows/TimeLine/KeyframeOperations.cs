using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Gui.Graph;
using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    internal static class KeyframeOperations
    {
        public static void DeleteSelectedKeyframesFromAnimationParameters(HashSet<VDefinition> selectedKeyframes, IEnumerable<GraphWindow.AnimationParameter> animationParameters)
        {
            foreach (var param in animationParameters)
            {
                foreach (var curve in param.Curves)
                {
                    foreach (var keyframe in curve.GetVDefinitions().ToList())
                    {
                        if(!selectedKeyframes.Contains(keyframe))
                            continue;
                        
                        curve.RemoveKeyframeAt(keyframe.U);
                        selectedKeyframes.Remove(keyframe);
                    }
                }
            }
        }

        
        public static void CheckForBetterSnapping(double targetTime, double anchorTime, float snapThresholdOnCanvas, ref SnapResult bestSnapResult)
        {
            var distance = Math.Abs(anchorTime - targetTime);
            if (distance < 0.001)
                return;

            var force = Math.Max(0, snapThresholdOnCanvas - distance);
            if (bestSnapResult != null && bestSnapResult.Force > force)
                return;

            // Avoid allocation
            if (bestSnapResult == null)
            {
                bestSnapResult = new SnapResult(distance, force);
            }
            else
            {
                bestSnapResult.Force = force;
                bestSnapResult.SnapToValue = anchorTime;
            }
        }
    }
}
