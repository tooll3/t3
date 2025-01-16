using ImGuiNET;
using T3.Editor.Gui.Graph.GraphUiModel;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.ProjectSession;

namespace T3.Editor.Gui.Windows;

/// <summary>
/// Allows users to create and use bookmarks to quickly navigate between areas of graphs
/// </summary>
internal static class GraphBookmarkNavigation
{
    public static void HandleForCanvas(GraphComponents components)
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
                SaveBookmark(components, i);
                _lastInteractionFrame = ImGui.GetFrameCount();
                break;
            }

            else if (KeyboardBinding.Triggered(_loadBookmarkActions[i]))
            {
                LoadBookmark(components, i);
                _lastInteractionFrame = ImGui.GetFrameCount();
                break;
            }
        }
    }

    public static void DrawBookmarksMenu()
    {
        var components = ProjectEditing.Components;
        if (components == null)
        {
            Log.Warning($"Cannot draw bookmark menu. No focused graph window.");
            return;
        }

        if (ImGui.BeginMenu("Load graph bookmark"))
        {
            for (var index = 0; index < _loadBookmarkActions.Length; index++)
            {
                var action = _loadBookmarkActions[index];
                var shortcuts = KeyboardBinding.ListKeyboardShortcuts(action, showLabel:false);
                var isAvailable = DoesBookmarkExist(index);
                if (ImGui.MenuItem(action.ToString(), shortcuts, false, enabled: isAvailable))
                {
                    LoadBookmark(components, index);
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
                    SaveBookmark(components, index);
                }
            }

            ImGui.EndMenu();
        }
    }

    private static void LoadBookmark(GraphComponents window, int index)
    {
        var bookmark = GetBookmarkAt(index);
        if (bookmark == null)
        {
            Log.Debug($"Bookmark {index} doesn't exist. You can create it by focusing a graph window and press Ctrl+Shift" + index);
            return;
        }

        var op = window.Structure.GetInstanceFromIdPath(bookmark.IdPath);
        if (op == null)
        {
            Log.Error("Invalid node path");
            return;
        }

        window.TrySetCompositionOp(bookmark.IdPath);
        window.GraphCanvas.SetVisibleRange(bookmark.ViewScope.Scale, bookmark.ViewScope.Scroll);
        //SelectionManager.SetSelection(bookmark.SelectedChildIds);
    }

    private static void SaveBookmark(GraphComponents window, int index)
    {
        Log.Debug("Saving bookmark " + index);
        var bookmarks = UserSettings.Config.Bookmarks;

        // Extend list length if necessary
        if (UserSettings.Config.Bookmarks.Count <= index)
        {
            bookmarks.AddRange(new Bookmark[index + 1 - bookmarks.Count]);
        }
            
        var canvas = window.GraphCanvas;

        bookmarks[index] = new Bookmark()
                               {
                                   IdPath = window.CompositionOp.InstancePath.ToList(),
                                   ViewScope = canvas.GetTargetScope(),
                                   SelectedChildIds = window.NodeSelection.GetSelectedNodes<SymbolUi.Child>().Select(s => s.Id).ToList()
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


    private static bool HasInteractedInCurrentFrame()
    {
        var frameIndex = ImGui.GetFrameCount();
        return _lastInteractionFrame == frameIndex;
    }
        
    private static int _lastInteractionFrame;
}

public class Bookmark
{
    // Fixme: Deserialization doesn't work and results into new (incorrect) random Ids
    //[JsonConverter(typeof(List<Guid>))]
    public List<Guid> IdPath = new();
    public CanvasScope ViewScope;

    public List<Guid> SelectedChildIds = new();
    // todo - include Project in this
}