using System.Threading.Tasks;
using ImGuiNET;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Gui.Graph.Legacy;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.UiHelpers.Wiki;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.ProjectSession;

namespace T3.Editor.Gui;

internal static class AppMenu
{
    internal static void DrawAppMenuBar()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6) * T3Ui.UiScaleFactor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, T3Style.WindowPaddingForMenus * T3Ui.UiScaleFactor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                UserSettings.Config.ShowMainMenu = true;
            }

            ImGui.SetCursorPos(new Vector2(0, -1)); // Shift to make menu items selected when hitting top of screen

            if (ImGui.BeginMenu("Project"))
            {
                UserSettings.Config.ShowMainMenu = true;

                var currentProject = ProjectEditing.Components?.OpenedProject.Package;
                var showNewTemplateOption = !T3Ui.IsCurrentlySaving && currentProject != null;

                if (ImGui.MenuItem("New...", KeyboardBinding.ListKeyboardShortcuts(UserActions.New, false), false, showNewTemplateOption))
                {
                    T3Ui.CreateFromTemplateDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("New Project"))
                {
                    T3Ui.NewProjectDialog.ShowNextFrame();
                }

                if (currentProject is { IsReadOnly: false } && currentProject is EditableSymbolProject project)
                {
                    ImGui.Separator();

                    if (ImGui.BeginMenu("Open.."))
                    {
                        if (ImGui.MenuItem("Project Folder"))
                        {
                            CoreUi.Instance.OpenWithDefaultApplication(project.Folder);
                        }

                        if (ImGui.MenuItem("Resource Folder"))
                        {
                            CoreUi.Instance.OpenWithDefaultApplication(project.ResourcesFolder);
                        }

                        if (ImGui.MenuItem("Project in IDE"))
                        {
                            CoreUi.Instance.OpenWithDefaultApplication(project.CsProjectFile.FullPath);
                        }
                        
                        ImGui.EndMenu();
                    }
                }

                ImGui.Separator();

                // Disabled, at least for now, as this is an incomplete (or not even started) operation on the Main branch atm
                if (ImGui.MenuItem("Import Operators", null, false, !T3Ui.IsCurrentlySaving))
                {
                    BlockingWindow.Instance.ShowMessageBox("This feature is not yet implemented on the main branch - stay tuned for updates!", "Not yet implemented");
                    //_importDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Fix File references", ""))
                {
                    BlockingWindow.Instance.ShowMessageBox("This feature is not yet implemented on the main branch - stay tuned for updates!", "Not yet implemented");
                    //FileReferenceOperations.FixOperatorFilepathsCommand_Executed();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Save", KeyboardBinding.ListKeyboardShortcuts(UserActions.Save, false), false, !T3Ui.IsCurrentlySaving))
                {
                    T3Ui.SaveInBackground(saveAll: false);
                }
                
                if (ImGui.MenuItem("Save All", !T3Ui.IsCurrentlySaving))
                {
                    Task.Run(() =>
                             {
                                 T3Ui.Save(true);
                             });
                }
                
                ImGui.Separator();
                
                if(ImGui.BeginMenu("Load Project", !T3Ui.IsCurrentlySaving && EditableSymbolProject.AllProjects.Any(x => x.HasHome)))
                {
                    foreach (var package in EditableSymbolProject.AllProjects)
                    {
                        if (!package.HasHome)
                            continue;

                        var name = package.DisplayName;
                        
                        if (ImGui.MenuItem(name))
                        {
                            bool replaceFocusedWindow = false;

                            if (GraphWindow.GraphWindowInstances.Count > 0)
                            {
                                var choice = BlockingWindow.Instance.ShowMessageBox("Would you like to create a new window?", "Opening " + name, "Yes", "No");
                                replaceFocusedWindow = choice == "No";
                            }
                            
                            if (!GraphWindow.TryOpenPackage(package, replaceFocusedWindow))
                            {
                                Log.Error("Failed to open package " + name);
                            };
                        }
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.Separator();
                
                if(ImGui.BeginMenu("Clear shader cache"))
                {
                    if (ImGui.MenuItem("Editor only"))
                    {
                        ShaderCompiler.DeleteShaderCache(all: false);
                    }

                    if (ImGui.MenuItem("All editor and player versions"))
                    {
                        ShaderCompiler.DeleteShaderCache(all: true);
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.Separator();

                if (ImGui.MenuItem("Quit", !T3Ui.IsCurrentlySaving))
                {
                    EditorUi.Instance.ExitApplication();
                }

                if (ImGui.IsItemHovered() && T3Ui.IsCurrentlySaving)
                {
                    ImGui.SetTooltip("Can't exit while saving is in progress");
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                UserSettings.Config.ShowMainMenu = true;
                if (ImGui.MenuItem("Undo", "CTRL+Z", false, UndoRedoStack.CanUndo))
                {
                    UndoRedoStack.Undo();
                }

                if (ImGui.MenuItem("Redo", "CTRL+SHIFT+Z", false, UndoRedoStack.CanRedo))
                {
                    UndoRedoStack.Redo();
                }

                ImGui.Separator();

                if (ImGui.BeginMenu("Bookmarks"))
                {
                    GraphBookmarkNavigation.DrawBookmarksMenu();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Tools"))
                {
                    if (ImGui.MenuItem("Export Operator Descriptions"))
                    {
                        ExportWikiDocumentation.ExportWiki();
                    }

                    if (ImGui.MenuItem("Export Documentation to JSON"))
                    {
                        ExportDocumentationStrings.ExportDocumentationAsJson();
                    }

                    if (ImGui.MenuItem("Import documentation from JSON"))
                    {
                        ExportDocumentationStrings.ImportDocumentationAsJson();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Add"))
            {
                UserSettings.Config.ShowMainMenu = true;
                SymbolTreeMenu.Draw();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                UserSettings.Config.ShowMainMenu = true;

                ImGui.Separator();
                ImGui.MenuItem("Show Main Menu", "", ref UserSettings.Config.ShowMainMenu);
                ImGui.MenuItem("Show Title", "", ref UserSettings.Config.ShowTitleAndDescription);
                ImGui.MenuItem("Show Timeline", "", ref UserSettings.Config.ShowTimeline);
                ImGui.MenuItem("Show Minimap", "", ref UserSettings.Config.ShowMiniMap);
                ImGui.MenuItem("Show Toolbar", "", ref UserSettings.Config.ShowToolbar);
                ImGui.MenuItem("Show Interaction Overlay", "", ref UserSettings.Config.ShowInteractionOverlay);
                if (ImGui.MenuItem("Toggle All UI Elements", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleAllUiElements, false), false,
                                   !T3Ui.IsCurrentlySaving))
                {
                    T3Ui.ToggleAllUiElements();
                }

                ImGui.MenuItem("Fullscreen", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleFullscreen, false), ref UserSettings.Config.FullScreen);
                if (ImGui.MenuItem("Focus Mode", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleFocusMode, false), UserSettings.Config.FocusMode))
                {
                    T3Ui.ToggleFocusMode();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Windows"))
            {
                WindowManager.DrawWindowMenuContent();
                ImGui.EndMenu();
            }
            
            #if DEBUG
            
            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Show Popup"))
                {
                    const string bodyText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis sagittis quis ligula sit amet ornare. " +
                                            "Donec auctor, nisl vel ultricies tincidunt, nisl nisl aliquam nisl, nec pulvinar nisl nisl vitae nisl. " +
                                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis sagittis quis ligula sit amet ornare. ";
                        
                    var result = BlockingWindow.Instance.ShowMessageBox(bodyText, "Debug Popup", "Ok", "Maybe", "Idk", "Possibly", "Affirmative", "Negatory", "NO!");
                    Log.Debug($"Result: \"{result}\"");
                }
                ImGui.EndMenu();
                
            }
            
            #endif
            
            if (UserSettings.Config.FullScreen)
            {
                ImGui.Dummy(new Vector2(10, 10));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1);

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.2f).Rgba);
                ImGui.TextUnformatted(Program.VersionText);
                ImGui.PopStyleColor();
            }

            T3Metrics.DrawRenderPerformanceGraph();

            Program.StatusErrorLine.Draw();

            ImGui.EndMainMenuBar();
        }

        ImGui.PopStyleVar(3);
    }
}