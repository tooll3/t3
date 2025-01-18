#nullable enable
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal static class KeyboardActions
{
    internal static void HandleKeyboardActions(GraphUiContext context)
    {
        var compositionOp = context.CompositionOp;
        //var compositionUi = compositionOp.GetSymbolUi();

        if (KeyboardBinding.Triggered(UserActions.FocusSelection))
        {
            // TODO: Implement
            Log.Debug("Not implemented yet");
            context.Canvas.FocusViewToSelection(context);
        }

        if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.Duplicate))
        {
            NodeActions.CopySelectedNodesToClipboard(context.Selector, compositionOp);
            NodeActions.PasteClipboard(context.Selector, context.Canvas, compositionOp);
            context.Layout.FlagAsChanged();
        }

        if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
        {
            Modifications.DeleteSelectedOps(context);
        }

        if (KeyboardBinding.Triggered(UserActions.ToggleDisabled))
        {
            NodeActions.ToggleDisabledForSelectedElements(context.Selector);
        }

        if (KeyboardBinding.Triggered(UserActions.ToggleBypassed))
        {
            NodeActions.ToggleBypassedForSelectedElements(context.Selector);
        }

        if (KeyboardBinding.Triggered(UserActions.PinToOutputWindow))
        {
            if (UserSettings.Config.FocusMode)
            {
                var selectedImage = context.Selector.GetFirstSelectedInstance();
                if (selectedImage != null && ProjectManager.Components != null)
                {
                    ProjectManager.Components.SetBackgroundOutput(selectedImage);
                }
            }
            else
            {
                // FIXME: This is a work around that needs a legacy graph window to be active
                if(ProjectManager.Components != null) 
                    NodeActions.PinSelectedToOutputWindow(ProjectManager.Components, context.Selector, compositionOp);
            }
        }
        
        if (KeyboardBinding.Triggered(UserActions.DisplayImageAsBackground))
        {
            var selectedImage = context.Selector.GetFirstSelectedInstance();
            if (selectedImage != null)
            {
                // TODO: implement
                //_window.GraphImageBackground.OutputInstance = selectedImage;
                Log.Debug("Not implemented yet");

            }
        }

        if (KeyboardBinding.Triggered(UserActions.DisplayImageAsBackground))
        {
            var selectedImage = context.Selector.GetFirstSelectedInstance();
            if (selectedImage != null && ProjectManager.Components != null)
            {
                ProjectManager.Components.SetBackgroundOutput(selectedImage);
                //GraphWindow.Focused..SetBackgroundInstanceForCurrentGraph(selectedImage);
            }
        }

        if (KeyboardBinding.Triggered(UserActions.CopyToClipboard))
        {
            NodeActions.CopySelectedNodesToClipboard(context.Selector, compositionOp);
        }

        if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.PasteFromClipboard))
        {
            NodeActions.PasteClipboard(context.Selector, context.Canvas, compositionOp);
            context.Layout.FlagAsChanged();
        }

        // if (KeyboardBinding.Triggered(UserActions.LayoutSelection))
        // {
        //     _nodeGraphLayouting.ArrangeOps(compositionOp);
        // }

        // if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.AddAnnotation))
        // {
        //     var newAnnotation = NodeActions.AddAnnotation(context.Selector, context.Canvas, compositionOp);
        //     // TODO: enable rename annotation state...
        //     //_graph.RenameAnnotation(newAnnotation);
        // }

        //IReadOnlyList<Guid>? navigationPath = null;

        // Navigation (this should eventually be part of the graph window)
        // if (KeyboardBinding.Triggered(UserActions.NavigateBackwards))
        // {
        //     navigationPath = context.NavigationHistory.NavigateBackwards();
        // }
        //
        // if (KeyboardBinding.Triggered(UserActions.NavigateForward))
        // {
        //     navigationPath = context.NavigationHistory.NavigateForward();
        // }

        //if (navigationPath != null)
        //    _window.TrySetCompositionOp(navigationPath);

        // Todo: Implement
        // if (KeyboardBinding.Triggered(UserActions.SelectToAbove))
        // {
        //     NodeNavigation.SelectAbove();
        // }
        //
        // if (KeyboardBinding.Triggered(UserActions.SelectToRight))
        // {
        //     NodeNavigation.SelectRight();
        // }
        //
        // if (KeyboardBinding.Triggered(UserActions.SelectToLeft))
        // {
        //     NodeNavigation.SelectLeft();
        // }
        //
        // if (KeyboardBinding.Triggered(UserActions.SelectToBelow))
        // {
        //     NodeNavigation.SelectBelow();
        // }

        if (KeyboardBinding.Triggered(UserActions.AddComment))
        {
            context.EditCommentDialog.ShowNextFrame();
        }
    }
}