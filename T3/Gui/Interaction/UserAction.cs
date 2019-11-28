using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using T3.Gui.Commands;
using T3.Gui.Windows;

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
        Duplicate,
        DeleteSelection,
        CopyToClipboard,
        PasteFromClipboard,
        LoadLayout0,
        LoadLayout1,
        LoadLayout2,
        SaveLayout0,
        SaveLayout1,
        SaveLayout2,
    }

    public static class UserActionRegistry
    {
        public static Dictionary<UserActions, Action> Entries { get; }
            = new Dictionary<UserActions, Action>
              {
                  { UserActions.Undo, UndoRedoStack.Undo },
                  { UserActions.Redo, UndoRedoStack.Redo },
                  { UserActions.Save, T3Ui.UiModel.Save },

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
        public bool NeedsWindowFocus = false;
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
                        && Math.Abs(io.KeysDownDurationPrev[(int)c.Key]) < 0.001f
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

        private KeyboardBinding(UserActions action, KeyCombination combination, bool needsWindowFocus = false)
        {
            Action = action;
            Combinations = new[] { combination };
            NeedsWindowFocus = needsWindowFocus;
        }

        private static readonly List<KeyboardBinding> Bindings
            = new List<KeyboardBinding>
              {
                  new KeyboardBinding(UserActions.PlaybackForward, new KeyCombination(Key.L)),
                  new KeyboardBinding(UserActions.PlaybackForwardHalfSpeed,
                                      new KeyCombination(Key.L, shift: true)),
                  new KeyboardBinding(UserActions.PlaybackBackwards, new KeyCombination(Key.J)),
                  new KeyboardBinding(UserActions.PlaybackStop, new KeyCombination(Key.K)),
                  new KeyboardBinding(UserActions.PlaybackToggle, new KeyCombination(Key.Space)),    // TODO: Fixme!
                  new KeyboardBinding(UserActions.PlaybackPreviousFrame,
                                      new KeyCombination(Key.CursorLeft, shift: true)),
                  new KeyboardBinding(UserActions.PlaybackNextFrame,
                                      new KeyCombination(Key.CursorRight, shift: true)),
                  new KeyboardBinding(UserActions.PlaybackJumpToNextKeyframe, new KeyCombination(Key.Period)),
                  new KeyboardBinding(UserActions.PlaybackJumpToPreviousKeyframe,
                                      new KeyCombination(Key.Comma)),
                  new KeyboardBinding(UserActions.PlaybackNextFrame,
                                      new KeyCombination(Key.CursorRight, shift: true)),

                  new KeyboardBinding(UserActions.Undo, new KeyCombination(Key.Z, ctrl: true)),
                  new KeyboardBinding(UserActions.Redo, new KeyCombination(Key.Z, ctrl: true, shift: true)),

                  new KeyboardBinding(UserActions.Save, new KeyCombination(Key.S, ctrl: true)),
                  new KeyboardBinding(UserActions.FocusSelection, new KeyCombination(Key.F))
                  { NeedsWindowHover = true },
                  new KeyboardBinding(UserActions.Duplicate, new KeyCombination(Key.D, ctrl: true))
                  { NeedsWindowFocus = true },
                  new KeyboardBinding(UserActions.DeleteSelection, new KeyCombination(Key.Delete))
                  { NeedsWindowFocus = true },
                  new KeyboardBinding(UserActions.CopyToClipboard, new KeyCombination(Key.C, ctrl: true))
                  { NeedsWindowFocus = true },
                  new KeyboardBinding(UserActions.PasteFromClipboard, new KeyCombination(Key.V, ctrl: true))
                  { NeedsWindowFocus = true },
                  new KeyboardBinding(UserActions.LoadLayout0, new KeyCombination(Key.F1)),
                  new KeyboardBinding(UserActions.LoadLayout1, new KeyCombination(Key.F2)),
                  new KeyboardBinding(UserActions.LoadLayout2, new KeyCombination(Key.F3)),
                  new KeyboardBinding(UserActions.SaveLayout0, new KeyCombination(Key.F1, ctrl: true)),
                  new KeyboardBinding(UserActions.SaveLayout1, new KeyCombination(Key.F2, ctrl: true)),
                  new KeyboardBinding(UserActions.SaveLayout2, new KeyCombination(Key.F3, ctrl: true)),
              };
    }
}