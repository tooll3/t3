using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.IO;

namespace T3.Gui
{
    /// <summary>
    /// UserAction represent single atomic commands that can be mapped to a keyboard shortcuts
    /// </summary>
    /// 
    public enum UserActions
    {
        // General
        Undo,
        Redo,
        Save,
        FocusSelection,
        DeleteSelection,
        CopyToClipboard,
        PasteFromClipboard,
        
        // Playback
        PlaybackForward,
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
        
        // Timeline
        SetStartTime,
        SetEndTime,
        InsertKeyframe,
        InsertKeyframeWithIncrement,
        
        // Graph
        PinToOutputWindow,
        DisplayImageAsBackground,
        ClearBackgroundImage,
        Duplicate,
        LayoutSelection,
        ToggleDisabled,
        AddAnnotation,
        
        ScrollLeft,
        ScrollRight,
        ScrollUp,
        ScrollDown,
        
        // Layout and window management
        ToggleFullScreenGraph,
        ToggleFocusMode,
        ToggleVariationsWindow,
        
        LoadLayout0,
        LoadLayout1,
        LoadLayout2,
        LoadLayout3,
        LoadLayout4,
        LoadLayout5,
        LoadLayout6,
        LoadLayout7,
        LoadLayout8,
        LoadLayout9,
        SaveLayout0,
        SaveLayout1,
        SaveLayout2,
        SaveLayout3,
        SaveLayout4,
        SaveLayout5,
        SaveLayout6,
        SaveLayout7,
        SaveLayout8,
        SaveLayout9,

        LoadBookmark0,
        LoadBookmark1,
        LoadBookmark2,
        LoadBookmark3,
        LoadBookmark4,
        LoadBookmark5,
        LoadBookmark6,
        LoadBookmark7,
        LoadBookmark8,
        LoadBookmark9,
        SaveBookmark0,
        SaveBookmark1,
        SaveBookmark2,
        SaveBookmark3,
        SaveBookmark4,
        SaveBookmark5,
        SaveBookmark6,
        SaveBookmark7,
        SaveBookmark8,
        SaveBookmark9,
        

    }

    public static class UserActionRegistry
    {
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
        public bool NeedsWindowFocus;
        public bool NeedsWindowHover;
        public bool KeyPressOnly;
        public readonly KeyCombination Combination;
        public float AutoRepeatTime = -1f;
        private float PressTime = 0;


        public static bool Triggered(UserActions action)
        {
            if (!_anyKeysPressed)
                return false;

            if (ImGui.IsAnyItemActive())
                return false;

            var io = ImGui.GetIO();
            foreach (var binding in Bindings)
            {
                if (binding.Action != action)
                    continue;

                if (binding.NeedsWindowFocus && !ImGui.IsWindowFocused())
                    continue;

                if (binding.NeedsWindowHover && !ImGui.IsWindowHovered())
                    continue;

                var c = binding.Combination;
                float curTime = (float)ImGui.GetTime();

                if (binding.AutoRepeatTime <= 0)
                {
                    var isKeyPressed = (!binding.KeyPressOnly || ImGui.IsKeyPressed((ImGuiKey)c.Key, false));
                    if (ImGui.IsKeyPressed((ImGuiKey)c.Key, false)
                        && isKeyPressed
                        && ((!c.Alt && !io.KeyAlt) || (c.Alt && io.KeyAlt)) // There is probably a smarty way to express this.
                        && ((!c.Ctrl && !io.KeyCtrl) || (c.Ctrl && io.KeyCtrl))
                        && ((!c.Shift && !io.KeyShift) || (c.Shift && io.KeyShift))
                        )
                        return true;
                }
                else
                {
                    var isKeyDown = ImGui.IsKeyDown((ImGuiKey)c.Key);
                    if (isKeyDown
                        && ((!c.Alt && !io.KeyAlt) || (c.Alt && io.KeyAlt)) // There is probably a smarty way to express this.
                        && ((!c.Ctrl && !io.KeyCtrl) || (c.Ctrl && io.KeyCtrl))
                        && ((!c.Shift && !io.KeyShift) || (c.Shift && io.KeyShift))
                        )
                    {
                        if (binding.PressTime == 0) // first time key down
                        {
                            binding.PressTime = curTime;
                            return true;
                        }

                        float deltaTime = curTime - binding.PressTime;
                        if (deltaTime >= binding.AutoRepeatTime)
                        {
                            return true;
                        }
                        else
                        {
                            return false; // wait autorepeat timeout
                        }
                    }
                    else // key up
                    {
                        binding.PressTime = 0;
                    }
                }
            }
            return false;
        }
        



        public static string ListKeyboardShortcuts(UserActions action, bool showLabel = true)
        {
            var bindings = Bindings.FindAll(b => b.Action == action);
            if (bindings.Count == 0)
                return "";

            var shortCuts = bindings.Select(binding => binding.Combination.ToString()).ToList();
            return (showLabel
                        ? (bindings.Count == 1
                               ? "Short cut: "
                               : "Short cuts ")
                        : "")
                   + string.Join(" and ", shortCuts);
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

            public override string ToString()
            {
                return (Ctrl ? "Ctrl+" : "")
                       + (Alt ? "Alt+" : "")
                       + (Shift ? "Shift+" : "")
                       + Key;
            }
        }

        private KeyboardBinding(UserActions action, KeyCombination combination, bool needsWindowFocus = false, bool keyPressOnly = false)
        {
            Action = action;
            Combination = combination;
            NeedsWindowFocus = needsWindowFocus;
            KeyPressOnly = keyPressOnly;
        }

        private static readonly List<KeyboardBinding> Bindings
            = new()
                  {
                            // Global
                            new KeyboardBinding(UserActions.Save, new KeyCombination(Key.S, ctrl: true)),
                            new KeyboardBinding(UserActions.FocusSelection, new KeyCombination(Key.F)) { NeedsWindowHover = true },
                            new KeyboardBinding(UserActions.Duplicate, new KeyCombination(Key.D, ctrl: true)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.DeleteSelection, new KeyCombination(Key.Delete)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.DeleteSelection, new KeyCombination(Key.Backspace)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.CopyToClipboard, new KeyCombination(Key.C, ctrl: true)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.PasteFromClipboard, new KeyCombination(Key.V, ctrl: true)) { NeedsWindowFocus = true },
                            
                            // Playback
                            new KeyboardBinding(UserActions.PlaybackForward, new KeyCombination(Key.L)),
                            new KeyboardBinding(UserActions.PlaybackForwardHalfSpeed, new KeyCombination(Key.L, shift: true)),
                            new KeyboardBinding(UserActions.PlaybackBackwards, new KeyCombination(Key.J)),
                            new KeyboardBinding(UserActions.PlaybackStop, new KeyCombination(Key.K)),
                            new KeyboardBinding(UserActions.PlaybackToggle, new KeyCombination(Key.Space)), // TODO: Fixme!
                            new KeyboardBinding(UserActions.PlaybackPreviousFrame, new KeyCombination(Key.CursorLeft, shift: true)) { AutoRepeatTime = 1.0f },
                            new KeyboardBinding(UserActions.PlaybackNextFrame, new KeyCombination(Key.CursorRight, shift: true)) { AutoRepeatTime = 1.0f },
                            new KeyboardBinding(UserActions.PlaybackJumpToNextKeyframe, new KeyCombination(Key.Period)),
                            new KeyboardBinding(UserActions.PlaybackJumpToPreviousKeyframe, new KeyCombination(Key.Comma)),
                            new KeyboardBinding(UserActions.PlaybackNextFrame, new KeyCombination(Key.CursorRight, shift: true)),
                            new KeyboardBinding(UserActions.PlaybackJumpBack, new KeyCombination(Key.B)),

                            new KeyboardBinding(UserActions.Undo, new KeyCombination(Key.Z, ctrl: true)),
                            new KeyboardBinding(UserActions.Redo, new KeyCombination(Key.Z, ctrl: true, shift: true)),

                            // Timeline
                            new KeyboardBinding(UserActions.InsertKeyframe, new KeyCombination(Key.C)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.InsertKeyframeWithIncrement, new KeyCombination(Key.C, shift: true)) { NeedsWindowFocus = true },
                            
                            // Graph window
                            new KeyboardBinding(UserActions.ToggleDisabled, new KeyCombination(Key.D, shift:true)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.PinToOutputWindow, new KeyCombination(Key.P)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.DisplayImageAsBackground, new KeyCombination(Key.P, ctrl:true)) { NeedsWindowFocus = false },
                            new KeyboardBinding(UserActions.ClearBackgroundImage, new KeyCombination(Key.P, ctrl:true, shift: true)) { NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.LayoutSelection, new KeyCombination(Key.G)),

                            new KeyboardBinding(UserActions.AddAnnotation, new KeyCombination(Key.A, shift:true)){ NeedsWindowFocus = true },
                            new KeyboardBinding(UserActions.ToggleVariationsWindow, new KeyCombination(Key.V, alt:true)){ NeedsWindowFocus = false },

                            // Layout and window management
                            new KeyboardBinding(UserActions.ToggleFullScreenGraph, new KeyCombination(Key.F11, ctrl: true)),
                            new KeyboardBinding(UserActions.ToggleFocusMode, new KeyCombination(Key.Esc, shift: true)),
                            new KeyboardBinding(UserActions.ToggleFocusMode, new KeyCombination(Key.Esc, shift: true)),
                            
                            new KeyboardBinding(UserActions.LoadBookmark1, new KeyCombination(Key.D1, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark2, new KeyCombination(Key.D2, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark3, new KeyCombination(Key.D3, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark4, new KeyCombination(Key.D4, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark5, new KeyCombination(Key.D5, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark6, new KeyCombination(Key.D6, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark7, new KeyCombination(Key.D7, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark8, new KeyCombination(Key.D8, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark9, new KeyCombination(Key.D9, ctrl: true)),
                            new KeyboardBinding(UserActions.LoadBookmark0, new KeyCombination(Key.D0, ctrl: true)),

                            new KeyboardBinding(UserActions.SaveBookmark1, new KeyCombination(Key.D1, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark2, new KeyCombination(Key.D2, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark3, new KeyCombination(Key.D3, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark4, new KeyCombination(Key.D4, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark5, new KeyCombination(Key.D5, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark6, new KeyCombination(Key.D6, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark7, new KeyCombination(Key.D7, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark8, new KeyCombination(Key.D8, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark9, new KeyCombination(Key.D9, ctrl: true, shift: true)),
                            new KeyboardBinding(UserActions.SaveBookmark0, new KeyCombination(Key.D0, ctrl: true, shift: true)),

                            new KeyboardBinding(UserActions.LoadLayout0, new KeyCombination(Key.F1)),
                            new KeyboardBinding(UserActions.LoadLayout1, new KeyCombination(Key.F2)),
                            new KeyboardBinding(UserActions.LoadLayout2, new KeyCombination(Key.F3)),
                            new KeyboardBinding(UserActions.LoadLayout3, new KeyCombination(Key.F4)),
                            new KeyboardBinding(UserActions.LoadLayout4, new KeyCombination(Key.F5)),
                            new KeyboardBinding(UserActions.LoadLayout5, new KeyCombination(Key.F6)),
                            new KeyboardBinding(UserActions.LoadLayout6, new KeyCombination(Key.F7)),
                            new KeyboardBinding(UserActions.LoadLayout7, new KeyCombination(Key.F8)),
                            new KeyboardBinding(UserActions.LoadLayout8, new KeyCombination(Key.F9)),
                            new KeyboardBinding(UserActions.LoadLayout9, new KeyCombination(Key.F10)),

                            new KeyboardBinding(UserActions.SaveLayout0, new KeyCombination(Key.F1, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout1, new KeyCombination(Key.F2, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout2, new KeyCombination(Key.F3, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout3, new KeyCombination(Key.F4, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout4, new KeyCombination(Key.F5, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout5, new KeyCombination(Key.F6, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout6, new KeyCombination(Key.F7, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout7, new KeyCombination(Key.F8, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout8, new KeyCombination(Key.F9, ctrl: true)),
                            new KeyboardBinding(UserActions.SaveLayout9, new KeyCombination(Key.F10, ctrl: true)),
                  };

        public static void InitFrame()
        {
            _anyKeysPressed = ImGui.GetIO().KeysDown.Count > 0;
        }

        private static bool _anyKeysPressed;
    }
}