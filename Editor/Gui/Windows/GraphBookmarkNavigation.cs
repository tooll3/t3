using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows
{
    /// <summary>
    /// Allows users to create and use bookmarks to quickly navigate between areas of graphs
    /// </summary>
    public static class GraphBookmarkNavigation
    {
        public static void HandleForCanvas(GraphCanvas canvas)
        {
            // var isNotFocused = !ImGui.IsWindowFocused();
            // if (isNotFocused)
            // {
            //     return;
            // }

            var alreadyInteracted = HasInteractedInCurrentFrame();
            if (alreadyInteracted)
            {
                return;
            }

            for (var i = 0; i < _saveBookmarkActions.Length; i++)
            {
                if (KeyboardBinding.Triggered(_saveBookmarkActions[i]))
                {
                    SaveBookmark(canvas, i);
                    _lastInteractionFrame = ImGui.GetFrameCount();
                    break;
                }

                else if (KeyboardBinding.Triggered(_loadBookmarkActions[i]))
                {
                    LoadBookmark(canvas, i);
                    _lastInteractionFrame = ImGui.GetFrameCount();
                    break;
                }
            }
        }

        private static GraphWindow GetCurrentGraphWindow()
        {
            GraphWindow current = null;
            foreach (var instance in GraphWindow.GraphWindowInstances)
            {
                if (instance is GraphWindow graphWindow && instance.Config.Visible)
                {
                    current = graphWindow;
                    break;
                }
            }

            return current;
        }

        public static void DrawBookmarksMenu()
        {
            var currentWindow = GetCurrentGraphWindow();

            if (ImGui.BeginMenu("Load graph bookmark"))
            {
                for (var index = 0; index < _loadBookmarkActions.Length; index++)
                {
                    var action = _loadBookmarkActions[index];
                    var shortcuts = KeyboardBinding.ListKeyboardShortcuts(action, showLabel:false);
                    var isAvailable = DoesBookmarkExist(index);
                    if (ImGui.MenuItem(action.ToString(), shortcuts, false, enabled: isAvailable))
                    {
                        LoadBookmark(currentWindow.GraphCanvas, index);
                    }
                }

                ImGui.EndMenu();
                
            }

            if (ImGui.BeginMenu("Save graph bookmark"))
            {
                for (var index = 0; index < _saveBookmarkActions.Length; index++)
                {
                    var action = _saveBookmarkActions[index];
                    var shortcuts = KeyboardBinding.ListKeyboardShortcuts(action, showLabel:false);
                    
                    if (ImGui.MenuItem(action.ToString(), shortcuts))
                    {
                        SaveBookmark(currentWindow.GraphCanvas, index);
                    }
                }

                ImGui.EndMenu();
            }
        }

        private static void LoadBookmark(GraphCanvas canvas, int index)
        {
            var bookmark = GetBookmarkAt(index);
            if (bookmark == null)
            {
                Log.Debug($"Bookmark {index} doesn't exist. You can create it by focusing a graph window and press Ctrl+Shift" + index);
                return;
            }

            var op = Structure.GetInstanceFromIdPath(bookmark.IdPath);
            if (op == null)
            {
                Log.Error("Invalid node path");
                return;
            }

            canvas.SetComposition(bookmark.IdPath, ICanvas.Transition.Undefined);
            canvas.SetVisibleRange(bookmark.ViewScope.Scale, bookmark.ViewScope.Scroll);
            //SelectionManager.SetSelection(bookmark.SelectedChildIds);
        }

        private static void SaveBookmark(GraphCanvas canvas, int index)
        {
            Log.Debug("Saving bookmark " + index);
            var bookmarks = UserSettings.Config.Bookmarks;

            // Extend list length if necessary
            if (UserSettings.Config.Bookmarks.Count <= index)
            {
                bookmarks.AddRange(new Bookmark[index + 1 - bookmarks.Count]);
            }

            bookmarks[index] = new Bookmark()
                                   {
                                       IdPath = OperatorUtils.BuildIdPathForInstance(canvas.CompositionOp),
                                       ViewScope = canvas.GetTargetScope(),
                                       SelectedChildIds = NodeSelection.GetSelectedNodes<SymbolChildUi>().Select(s => s.Id).ToList()
                                   };
            ;
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
            return GetBookmarkAt(index) != null;
        }

        private static Bookmark GetBookmarkAt(int index)
        {
            var bookmarks = UserSettings.Config.Bookmarks;
            if (bookmarks != null
                && bookmarks.Count > index)
            {
                return bookmarks[index];
            }

            return null;
        }

        public class Bookmark
        {
            // Fixme: Deserialization doesn't work and results into new (incorrect) random Ids
            //[JsonConverter(typeof(List<Guid>))]
            public List<Guid> IdPath = new();
            public ScalableCanvas.Scope ViewScope;
            public List<Guid> SelectedChildIds = new();
        }


        private static bool HasInteractedInCurrentFrame()
        {
            var frameIndex = ImGui.GetFrameCount();
            return _lastInteractionFrame == frameIndex;
        }
        
        private static int _lastInteractionFrame;
    }
}