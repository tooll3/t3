using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Gui
{
    /// <summary>
    /// UserAction represent single atomic commands that can be mapped to a keyboard shortcuts
    /// </summary>
    /// 
    public enum UserAction
    {
        PlaybackForward = 0,
        PlaybackBackwards,
        PlaybackNextFrame,
        PlaybackPreviousFrame,
        PlaybackStop,
        PlaybackToggle,
        PlaybackJumpBack,
        PlaybackJumpToNextKeyframe,
        PlaybackJumpToPreviousKeyframe,
        PlaybackJumpToEndTime,
        PlaybackJumpToStartTime,
        SetStartTime,
        SetEndTime,
    }


    public class KeyboardBinding
    {
        public UserAction Action;
        public bool NeedsWindowFocus = false;
        public KeyCombination[] Combinations;

        public static bool Triggered(UserAction action)
        {
            var binding = Bindings.FirstOrDefault(b => b.Action == action);
            if (binding != null)
            {
                if (binding.NeedsWindowFocus && !ImGui.IsWindowFocused())
                    return false;

                var io = ImGui.GetIO();
                foreach (var c in binding.Combinations)
                {
                    if (io.KeysDown[(int)c.Key]
                        && io.KeysDownDurationPrev[(int)c.Key] == 0
                        && (!c.Alt || (c.Alt && io.KeyAlt))
                        && (!c.Ctrl || (c.Ctrl && io.KeyCtrl))
                        && (!c.Shift || (c.Shift && io.KeyShift))
                     )
                        return true;
                }
            }
            return false;
        }

        public class KeyCombination
        {
            public KeyCombination(Key key, bool ctrl = false, bool alt = false, bool shift = false)
            {
                Key = key;
                Ctrl = ctrl;
                Alt = alt;
                Shift = shift;
            }
            public bool Ctrl;
            public bool Alt;
            public bool Shift;
            public Key Key;
        }

        public KeyboardBinding(UserAction action, KeyCombination combination, bool needsWindowFocus = false)
        {
            Action = action;
            Combinations = new KeyCombination[] { combination };
            NeedsWindowFocus = needsWindowFocus;
        }

        public static List<KeyboardBinding> Bindings = new List<KeyboardBinding>()
        {
            new KeyboardBinding(UserAction.PlaybackForward, new KeyCombination(Key.L) ),
            new KeyboardBinding(UserAction.PlaybackBackwards, new KeyCombination(Key.J) ),
            new KeyboardBinding(UserAction.PlaybackStop, new KeyCombination(Key.K) ),
            new KeyboardBinding(UserAction.PlaybackToggle, new KeyCombination(Key.Space) ),
        };
    }
}
