using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
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
        PlaybackForwardHalfSpeed,
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
        Save,
        FocusSelection,
    }


    public static class UserActionRegistry
    {
        public static Dictionary<UserActions, Action> Entries { get; } = new Dictionary<UserActions, Action>
                                                                         {
                                                                             { UserActions.Undo, UndoRedoStack.Undo },
                                                                             { UserActions.Redo, UndoRedoStack.Redo },
                                                                             { UserActions.Save, T3UI.UiModel.Save }
                                                                         };
        public static readonly HashSet<UserActions> DeferredActions = new HashSet<UserActions>();

        public static bool WasActionQueued(UserActions action)
        {
            if (!DeferredActions.Contains(action)) 
                return false;
            
            DeferredActions.Remove(action);
            return true;
        }
    }

    public class KeyboardBinding
    {
        public readonly UserActions Action;
        public readonly bool NeedsWindowFocus = false;
        public bool NeedsWindowHover = false;
        public readonly KeyCombination[] Combinations;

        public static bool Triggered(UserActions action)
        {
            if (ImGui.IsAnyItemActive())
                return false;

            var binding = Bindings.FirstOrDefault(b => b.Action == action);
            if (binding != null)
            {
                if (binding.NeedsWindowFocus && !ImGui.IsWindowFocused())
                    return false;

                if (binding.NeedsWindowHover && !ImGui.IsWindowHovered())
                    return false;

                var io = ImGui.GetIO();
                foreach (var c in binding.Combinations)
                {
                    if (io.KeysDown[(int)c.Key]
                        && io.KeysDownDurationPrev[(int)c.Key] == 0
                        && ((!c.Alt && !io.KeyAlt) || (c.Alt && io.KeyAlt)) // There is probably a smarty way to say this.
                        && ((!c.Ctrl && !io.KeyCtrl) || (c.Ctrl && io.KeyCtrl))
                        && ((!c.Shift && !io.KeyShift) || (c.Shift && io.KeyShift))    
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
            public readonly bool Ctrl;
            public readonly bool Alt;
            public readonly bool Shift;
            public readonly Key Key;
        }

        public KeyboardBinding(UserActions action, KeyCombination combination, bool needsWindowFocus = false)
        {
            Action = action;
            Combinations = new KeyCombination[] { combination };
            NeedsWindowFocus = needsWindowFocus;
        }

        public static readonly List<KeyboardBinding> Bindings = new List<KeyboardBinding>()
        {
            new KeyboardBinding(UserActions.PlaybackForward, new KeyCombination(Key.L) ),
            new KeyboardBinding(UserActions.PlaybackForwardHalfSpeed, new KeyCombination(Key.L, shift:true) ),
            new KeyboardBinding(UserActions.PlaybackBackwards, new KeyCombination(Key.J) ),
            new KeyboardBinding(UserActions.PlaybackStop, new KeyCombination(Key.K) ),
            new KeyboardBinding(UserActions.PlaybackToggle, new KeyCombination(Key.Space) ),
            new KeyboardBinding(UserActions.PlaybackPreviousFrame, new KeyCombination(Key.CursorLeft, shift:true)),
            new KeyboardBinding(UserActions.PlaybackNextFrame, new KeyCombination(Key.CursorRight, shift:true)),
            new KeyboardBinding(UserActions.PlaybackJumpToNextKeyframe, new KeyCombination(Key.Period)),
            new KeyboardBinding(UserActions.PlaybackJumpToPreviousKeyframe, new KeyCombination(Key.Comma)),
            new KeyboardBinding(UserActions.PlaybackNextFrame, new KeyCombination(Key.CursorRight, shift:true)),

            new KeyboardBinding(UserActions.Undo, new KeyCombination(Key.Z,ctrl:true) ),
            new KeyboardBinding(UserActions.Redo, new KeyCombination(Key.Z,ctrl:true, shift:true) ),
            
            new KeyboardBinding(UserActions.Save, new KeyCombination(Key.S,ctrl:true) ),
            new KeyboardBinding(UserActions.FocusSelection, new KeyCombination(Key.F)) { NeedsWindowHover = true},
        };
    }
}
