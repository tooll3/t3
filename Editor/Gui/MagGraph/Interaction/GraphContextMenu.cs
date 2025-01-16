using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.SystemUi;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Legacy;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Commands;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal static class GraphContextMenu
{
    internal static void DrawContextMenuContent(GraphUiContext context)
    {
        var clickPosition = ImGui.GetMousePosOnOpeningCurrentPopup();
        var compositionSymbolUi = context.CompositionOp.GetSymbolUi();

        var nodeSelection = context.Selector;

        var selectedChildUis = nodeSelection.GetSelectedChildUis().ToList();
        var nextUndoTitle = UndoRedoStack.CanUndo ? $" ({UndoRedoStack.GetNextUndoTitle()})" : string.Empty;
        if (ImGui.MenuItem("Undo" + nextUndoTitle,
                           shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.Undo, false),
                           selected: false,
                           enabled: UndoRedoStack.CanUndo))
        {
            UndoRedoStack.Undo();
        }

        ImGui.Separator();

        // ------ for selection -----------------------
        var oneOpSelected = selectedChildUis.Count == 1;
        var someOpsSelected = selectedChildUis.Count > 0;
        var snapShotsEnabledFromSomeOps
            = selectedChildUis
               .Any(selectedChildUi => selectedChildUi.EnabledForSnapshots);

        var label = oneOpSelected
                        ? $"{selectedChildUis[0].SymbolChild.ReadableName}..."
                        : $"{selectedChildUis.Count} selected items...";

        ImGui.PushFont(Fonts.FontSmall);
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Gray.Rgba);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();
        ImGui.PopFont();

        var allSelectedDisabled = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsDisabled);
        if (ImGui.MenuItem("Disable",
                           KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                           selected: allSelectedDisabled,
                           enabled: selectedChildUis.Count > 0))
        {
            NodeActions.ToggleDisabledForSelectedElements(nodeSelection);
        }

        var allSelectedBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
        if (ImGui.MenuItem("Bypassed",
                           KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleBypassed, false),
                           selected: allSelectedBypassed,
                           enabled: selectedChildUis.Count > 0))
        {
            NodeActions.ToggleBypassedForSelectedElements(nodeSelection);
        }

        if (ImGui.MenuItem("Rename", oneOpSelected))
        {
            RenameInstanceOverlay.OpenForChildUi(selectedChildUis[0]);
        }

        if (ImGui.MenuItem("Add Comment",
                           KeyboardBinding.ListKeyboardShortcuts(UserActions.AddComment, false),
                           selected: false,
                           enabled: oneOpSelected))
        {
            context.EditCommentDialog.ShowNextFrame();
        }

        // if (ImGui.MenuItem("Arrange sub graph",
        //                    KeyboardBinding.ListKeyboardShortcuts(UserActions.LayoutSelection, false),
        //                    selected: false,
        //                    enabled: someOpsSelected))
        // {
        //     _nodeGraphLayouting.ArrangeOps(compositionOp);
        // }

        var canModify = !compositionSymbolUi.Symbol.SymbolPackage.IsReadOnly;
        if (canModify)
        {
            // Disable if already enabled for all
            var disableBecauseAllEnabled
                = selectedChildUis
                   .TrueForAll(c2 => c2.EnabledForSnapshots);

            foreach (var c in selectedChildUis)
            {
                c.EnabledForSnapshots = !disableBecauseAllEnabled;
            }

            // Add to add snapshots
            var allSnapshots = VariationHandling.ActivePoolForSnapshots?.AllVariations;
            if (allSnapshots != null && allSnapshots.Count > 0)
            {
                if (disableBecauseAllEnabled)
                {
                    VariationHandling.RemoveInstancesFromVariations(selectedChildUis.Select(ui => ui.Id), allSnapshots);
                }
                // Remove from snapshots
                else
                {
                    var selectedInstances = selectedChildUis
                                           .Select(ui => context.CompositionOp.Children[ui.Id])
                                           .ToList();
                    foreach (var snapshot in allSnapshots)
                    {
                        VariationHandling.ActivePoolForSnapshots.UpdateVariationPropertiesForInstances(snapshot, selectedInstances);
                    }
                }
            }

            compositionSymbolUi.FlagAsModified();
        }

        if (ImGui.BeginMenu("Display as..."))
        {
            if (ImGui.MenuItem("Small", "",
                               selected: selectedChildUis.Any(child => child.Style == SymbolUi.Child.Styles.Default),
                               enabled: someOpsSelected))
            {
                foreach (var childUi in selectedChildUis)
                {
                    childUi.Style = SymbolUi.Child.Styles.Default;
                }
            }

            if (ImGui.MenuItem("Resizable", "",
                               selected: selectedChildUis.Any(child => child.Style == SymbolUi.Child.Styles.Resizable),
                               enabled: someOpsSelected))
            {
                foreach (var childUi in selectedChildUis)
                {
                    childUi.Style = SymbolUi.Child.Styles.Resizable;
                }
            }

            if (ImGui.MenuItem("Expanded", "",
                               selected: selectedChildUis.Any(child => child.Style == SymbolUi.Child.Styles.Resizable),
                               enabled: someOpsSelected))
            {
                foreach (var childUi in selectedChildUis)
                {
                    childUi.Style = SymbolUi.Child.Styles.Expanded;
                }
            }

            ImGui.Separator();

            // TODO: Implement
            var isImage = oneOpSelected
                          && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions.Count > 0
                          && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions[0].ValueType == typeof(Texture2D);
            if (ImGui.MenuItem("Set image as graph background",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.DisplayImageAsBackground, false),
                               selected: false,
                               enabled: isImage))
            {
                var instance = context.CompositionOp.Children[selectedChildUis[0].Id];
                context.GraphImageBackground.OutputInstance = instance;
            }

            // TODO: Implement
            if (ImGui.MenuItem("Pin to output", oneOpSelected))
            {
                if (GraphWindow.Focused != null) 
                    NodeActions.PinSelectedToOutputWindow(GraphWindow.Focused.Components, nodeSelection, context.CompositionOp);
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.MenuItem("Copy",
                           KeyboardBinding.ListKeyboardShortcuts(UserActions.CopyToClipboard, false),
                           selected: false,
                           enabled: someOpsSelected))
        {
            NodeActions.CopySelectedNodesToClipboard(nodeSelection, context.CompositionOp);
        }

        if (ImGui.MenuItem("Paste", KeyboardBinding.ListKeyboardShortcuts(UserActions.PasteFromClipboard, false)))
        {
            NodeActions.PasteClipboard(nodeSelection, context.Canvas, context.CompositionOp);
            context.Layout.FlagAsChanged();
        }

        var selectedInputUis = nodeSelection.GetSelectedNodes<IInputUi>().ToList();
        var selectedOutputUis = nodeSelection.GetSelectedNodes<IOutputUi>().ToList();

        var isSaving = T3Ui.IsCurrentlySaving;

        if (ImGui.MenuItem("Delete",
                           shortcut: "Del", // dynamic assigned shortcut is too long
                           selected: false,
                           enabled: (someOpsSelected || selectedInputUis.Count > 0 || selectedOutputUis.Count > 0) && !isSaving))
        {
            NodeActions.DeleteSelectedElements(nodeSelection, compositionSymbolUi, selectedChildUis, selectedInputUis, selectedOutputUis);
            context.Layout.FlagAsChanged();
        }

        if (ImGui.MenuItem("Duplicate",
                           KeyboardBinding.ListKeyboardShortcuts(UserActions.Duplicate, false),
                           selected: false,
                           enabled: selectedChildUis.Count > 0 && !isSaving))
        {
            NodeActions.CopySelectedNodesToClipboard(nodeSelection, context.CompositionOp);
            NodeActions.PasteClipboard(nodeSelection, context.Canvas, context.CompositionOp);
            context.Layout.FlagAsChanged();
        }

        ImGui.Separator();

        // if (ImGui.MenuItem("Change Symbol", someOpsSelected && !isSaving))
        // {
        //     var startingSearchString = selectedChildUis[0].SymbolChild.Symbol.Name;
        //     var position = selectedChildUis.Count == 1 ? selectedChildUis[0].PosOnCanvas : InverseTransformPositionFloat(ImGui.GetMousePos());
        //     _window.SymbolBrowser.OpenAt(position, null, null, false, startingSearchString,
        //                                  symbol => { ChangeSymbol.ChangeOperatorSymbol(nodeSelection, context.CompositionOp, selectedChildUis, symbol); });
        // }

        // TODO: Implement
        // if (ImGui.BeginMenu("Symbol definition...", !isSaving))
        // {
        //     if (ImGui.MenuItem("Rename Symbol", oneOpSelected))
        //     {
        //         _renameSymbolDialog.ShowNextFrame();
        //         _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name;
        //         //NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, "NewName");
        //     }
        //
        //     if (ImGui.MenuItem("Duplicate as new type...", oneOpSelected))
        //     {
        //         _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name ?? string.Empty;
        //         _nameSpaceForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Namespace ?? string.Empty;
        //         _symbolDescriptionForDialog = "";
        //         _duplicateSymbolDialog.ShowNextFrame();
        //     }
        //
        //     if (ImGui.MenuItem("Combine into new type...", someOpsSelected))
        //     {
        //         _nameSpaceForDialogEdits = compositionOp.Symbol.Namespace ?? string.Empty;
        //         _symbolDescriptionForDialog = "";
        //         _combineToSymbolDialog.ShowNextFrame();
        //     }
        //
        //     ImGui.EndMenu();
        // }

        var symbolPackage = compositionSymbolUi.Symbol.SymbolPackage;
        if (!symbolPackage.IsReadOnly)
        {
            if (ImGui.BeginMenu("Open folder..."))
            {
                if (ImGui.MenuItem("Project"))
                {
                    CoreUi.Instance.OpenWithDefaultApplication(symbolPackage.Folder);
                }

                if (ImGui.MenuItem("Resources"))
                {
                    CoreUi.Instance.OpenWithDefaultApplication(symbolPackage.ResourcesFolder);
                }

                ImGui.EndMenu();
            }
        }

        // TODO: Implement with mag graph logic
        // if (ImGui.BeginMenu("Add..."))
        // {
        //     if (ImGui.MenuItem("Add Node...", "TAB", false, true))
        //     {
        //         _window.SymbolBrowser.OpenAt(InverseTransformPositionFloat(clickPosition), null, null, false);
        //     }
        //
        //     if (canModify)
        //     {
        //         if (ImGui.MenuItem("Add input parameter..."))
        //         {
        //             _addInputDialog.ShowNextFrame();
        //         }
        //
        //         if (ImGui.MenuItem("Add output..."))
        //         {
        //             _addOutputDialog.ShowNextFrame();
        //         }
        //     }
        //
        //     if (ImGui.MenuItem("Add Annotation",
        //                        shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.AddAnnotation, false),
        //                        selected: false,
        //                        enabled: true))
        //     {
        //         var newAnnotation = NodeActions.AddAnnotation(nodeSelection, this, context.CompositionOp);
        //         _graph.RenameAnnotation(newAnnotation);
        //     }
        //
        //     ImGui.EndMenu();
        // }

        ImGui.Separator();

        if (ImGui.MenuItem("Export as Executable", oneOpSelected))
        {
            switch (PlayerExporter.TryExportInstance(context.CompositionOp, selectedChildUis.Single(), out var reason, out var exportDir))
            {
                case false:
                    Log.Error(reason);
                    BlockingWindow.Instance.ShowMessageBox(reason, $"Failed to export {label}");
                    break;
                default:
                    Log.Info(reason);
                    BlockingWindow.Instance.ShowMessageBox(reason, $"Exported {label} successfully!");
                    // open export directory in native file explorer
                    CoreUi.Instance.OpenWithDefaultApplication(exportDir);
                    break;
            }
        }

        // TODO: Clarify if required
        // if (oneOpSelected)
        // {
        //     var symbol = selectedChildUis.Single().SymbolChild.Symbol;
        //     CustomComponents.DrawSymbolCodeContextMenuItem(symbol);
        //     var childUi = selectedChildUis.Single();
        //
        //     // get instance that is currently selected
        //     var instance = context.CompositionOp.Children[childUi.Id];
        //
        //     if (NodeActions.TryGetShaderPath(instance, out var filePath, out var owner))
        //     {
        //         var shaderIsReadOnly = owner.IsReadOnly;
        //
        //         if (ImGui.MenuItem("Open in Shader Editor", true))
        //         {
        //             if (shaderIsReadOnly)
        //             {
        //                 CopyToTempShaderPath(filePath, out filePath);
        //                 BlockingWindow.Instance.ShowMessageBox("Warning - viewing a read-only shader. Modifications will not be saved.\n" +
        //                                                        "Following #include directives outside of the temp folder may lead you to read-only files, " +
        //                                                        "and editing those can break operators.\n\nWith great power...", "Warning");
        //             }
        //
        //             EditorUi.Instance.OpenWithDefaultApplication(filePath);
        //         }
        //     }
        // }
    }
}