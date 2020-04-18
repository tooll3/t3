using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
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
    }
}
