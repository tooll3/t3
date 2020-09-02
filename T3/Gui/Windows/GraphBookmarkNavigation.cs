using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Gui.Interaction;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    /// <summary>
    /// Allows users to create and use bookmarks to quickly navigate between areas of graphs
    /// </summary>
    public static class GraphBookmarkNavigation
    {
        public static void HandleBookmarkInteraction()
        {
            if (ImGui.IsWindowFocused() && !HasInteractedInCurrentFrame())
                return;

            _lastInteractionFrame = ImGui.GetFrameCount();
            
            
            for (var i = 0; i < _saveBookmarkActions.Length; i++)
            {
                if (KeyboardBinding.Triggered(_saveBookmarkActions[i]))
                    SaveBookmark(i);

                if (KeyboardBinding.Triggered(_loadBookmarkActions[i]))
                    LoadBookmark(i);
            }
        }


        public static void DrawBookmarksMenu()
        {
            if (ImGui.BeginMenu("Load Graph Bookmark"))
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ImGui.MenuItem("Bookmark " + (i + 1), "F" + (i + 1), false, enabled: DoesBookmarkExist(i)))
                    {
                        LoadBookmark(i);
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Save layouts"))
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ImGui.MenuItem("Bookmark " + (i + 1), "Ctrl+F" + (i + 1)))
                    {
                        SaveBookmark(i);
                    }
                }

                ImGui.EndMenu();
            }
        }


        private static void LoadBookmark(int index)
        {
            // Not implemented yet
        }
        
        private static void SaveBookmark(int index)
        {
            // Not implemented yet
        }
        
        private static readonly UserActions[] _loadBookmarkActions =
            {
                UserActions.LoadBookmark0,
                UserActions.LoadBookmark1,
                UserActions.LoadBookmark2,
                UserActions.LoadBookmark3,
                UserActions.LoadBookmark4,
                UserActions.LoadBookmark5,
                UserActions.LoadBookmark6,
                UserActions.LoadBookmark7,
                UserActions.LoadBookmark8,
                UserActions.LoadBookmark9,
            };

        private static readonly UserActions[] _saveBookmarkActions =
            {
                UserActions.SaveBookmark0,
                UserActions.SaveBookmark1,
                UserActions.SaveBookmark2,
                UserActions.SaveBookmark3,
                UserActions.SaveBookmark4,
                UserActions.SaveBookmark5,
                UserActions.SaveBookmark6,
                UserActions.SaveBookmark7,
                UserActions.SaveBookmark8,
                UserActions.SaveBookmark9,
            };

        private static bool DoesBookmarkExist(int index)
        {
            return UserSettings.Config.Bookmarks != null
                   && UserSettings.Config.Bookmarks.Count < index
                   && UserSettings.Config.Bookmarks[index] != null;

        }
        
        
        public class Bookmark
        {
            
            public List<Guid> IdPath= new List<Guid>();
            public ScalableCanvas.Scope ViewScope;
            public List<Guid> SelectedChildIds = new List<Guid>();
        }

        private static int _lastInteractionFrame;

        private static bool HasInteractedInCurrentFrame()
        {
            var frameIndex = ImGui.GetFrameCount();
            return _lastInteractionFrame == frameIndex;
        }
    }
}