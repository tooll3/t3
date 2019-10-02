using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Commands;

namespace T3.Gui
{
    /// <summary>
    /// UserAction represent single atomic commands that can be mapped to a keyboard shortcuts
    /// </summary>
    /// 
    public enum UserActions
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
        Undo,
        Redo,
    }


    public static class Actions
    {
        public static Dictionary<UserActions, Action> Entries = new Dictionary<UserActions, Action>()
        {
            {UserActions.PlaybackBackwards, () => { } },
            {UserActions.Undo, UndoRedoStack.Undo },
            {UserActions.Redo,  UndoRedoStack.Redo },
        };
    }

    public class KeyboardBinding
    {
        public UserActions Action;
        public bool NeedsWindowFocus = false;
        public KeyCombination[] Combinations;

        public static bool Triggered(UserActions action)
        {
            if (ImGui.IsAnyItemActive())
                return false;

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
                        && (!c.Alt || io.KeyAlt)
                        && (!c.Ctrl || io.KeyCtrl)
                        && (!c.Shift || io.KeyShift)
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

        public KeyboardBinding(UserActions action, KeyCombination combination, bool needsWindowFocus = false)
        {
            Action = action;
            Combinations = new KeyCombination[] { combination };
            NeedsWindowFocus = needsWindowFocus;
        }

        public static List<KeyboardBinding> Bindings = new List<KeyboardBinding>()
        {
            new KeyboardBinding(UserActions.PlaybackForward, new KeyCombination(Key.L) ),
            new KeyboardBinding(UserActions.PlaybackBackwards, new KeyCombination(Key.J) ),
            new KeyboardBinding(UserActions.PlaybackStop, new KeyCombination(Key.K) ),
            new KeyboardBinding(UserActions.PlaybackToggle, new KeyCombination(Key.Space) ),
            new KeyboardBinding(UserActions.Undo, new KeyCombination(Key.Z,ctrl:true) ),
            new KeyboardBinding(UserActions.Redo, new KeyCombination(Key.Z,ctrl:true, shift:true) ),
        };
    }
}
