using ImGuiNET;
using Operators.Utils;
using T3.Core.IO;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Variations.Midi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Config.Title = "Settings";
        }

        private enum Categories
        {
            Interface,
            Theme,
            Project,
            Midi,
            SpaceMouse,
            Keyboard,
        }

        private Categories _activeCategory;

        protected override void DrawContent()
        {
            var changed = false;

            ImGui.BeginChild("categories", new Vector2(120 * T3Ui.UiScaleFactor, -1), true, ImGuiWindowFlags.NoScrollbar);
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
                FormInputs.AddSegmentedButton(ref _activeCategory, "", 110 * T3Ui.UiScaleFactor);
                ImGui.PopStyleVar();
            }
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 5));
            ImGui.BeginChild("content", new Vector2(-1, -1), true);
            {
                FormInputs.SetIndentToParameters();
                switch (_activeCategory)
                {
                    case Categories.Interface:
                        FormInputs.SetIndentToLeft();

                        FormInputs.AddSectionHeader("User Interface");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Editing values with mousewheel needs CTRL key",
                                                          ref UserSettings.Config.MouseWheelEditsNeedCtrlKey,
                                                          "In parameter window you can edit numeric values by using the mouse wheel. This setting will prevent accidental modifications while scrolling because by using ctrl key for activation.",
                                                          UserSettings.Defaults.MouseWheelEditsNeedCtrlKey);

                        changed |= FormInputs.AddCheckBox("Mousewheel adjust flight speed",
                                                          ref UserSettings.Config.AdjustCameraSpeedWithMouseWheel,
                                                          "If enabled, scrolling the mouse wheel while holding left of right mouse button will control navigation speed with WASD keys. This is similar to Unity and Unreal",
                                                          UserSettings.Defaults.AdjustCameraSpeedWithMouseWheel);

                        changed |= FormInputs.AddCheckBox("Use arc connections",
                                                          ref UserSettings.Config.UseArcConnections,
                                                          "Affects the shape of the connections between your operators",
                                                          UserSettings.Defaults.UseArcConnections);

                        changed |= FormInputs.AddCheckBox("Drag snapped nodes",
                                                          ref UserSettings.Config.SmartGroupDragging,
                                                          "An experimental features that will drag neighbouring snapped operators",
                                                          UserSettings.Defaults.SmartGroupDragging);
                                                          
						changed |= FormInputs.AddCheckBox("Balance soundtrack visualizer",
                                                          ref UserSettings.Config.ExpandSpectrumVisualizerVertically,
                                                          "If true, changes the visualized pitch's logarithmic scale from base 'e' to base 10.\nLower frequencies will become more visible, making the frequency spectrum\n appear more \"balanced\"",
                                                          UserSettings.Defaults.ExpandSpectrumVisualizerVertically);
                        FormInputs.AddVerticalSpace();
                        FormInputs.SetIndentToParameters();
                        changed |= FormInputs.AddFloat("UI Scale",
                                                       ref UserSettings.Config.UiScaleFactor,
                                                       0.1f, 5f, 0.01f, true,
                                                       "The global scale of all rendered UI in the application",
                                                       UserSettings.Defaults.UiScaleFactor);

                        changed |= FormInputs.AddFloat("Scroll smoothing",
                                                       ref UserSettings.Config.ScrollSmoothing,
                                                       0.0f, 0.2f, 0.01f, true,
                                                       null,
                                                       UserSettings.Defaults.ScrollSmoothing);

                        changed |= FormInputs.AddFloat("Snap strength",
                                                       ref UserSettings.Config.SnapStrength,
                                                       0.0f, 0.2f, 0.01f, true,
                                                       "Controls the distance until items such as keyframes snap in the timeline",
                                                       UserSettings.Defaults.SnapStrength);

                        changed |= FormInputs.AddFloat("Click threshold",
                                                       ref UserSettings.Config.ClickThreshold,
                                                       0.0f, 10f, 0.1f, true,
                                                       "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input",
                                                       UserSettings.Defaults.ClickThreshold);

                        changed |= FormInputs.AddFloat("Timeline marks density",
                                                       ref UserSettings.Config.TimeRasterDensity,
                                                       0.0f, 10f, 0.01f, true,
                                                       "Density/opacity of the marks (time or beat) at the bottom of the timeline",
                                                       UserSettings.Defaults.TimeRasterDensity);

                        changed |= FormInputs.AddFloat("Gizmo size",
                                                       ref UserSettings.Config.GizmoSize,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.FrameStepAmount,
                                                              "Frame step amount",
                                                              "Controls the next rounding and step amount when jumping between frames.\nDefault shortcut is Shift+Cursor Left/Right");

                        changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.ValueEditMethod,
                                                              "Value input method",
                                                              "The control that pops up when dragging on a number value"
                                                             );

                        FormInputs.SetIndentToLeft();
                        FormInputs.AddVerticalSpace();
                        FormInputs.AddSectionHeader("Previews");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Show Graph thumbnails",
                                                          ref UserSettings.Config.ShowThumbnails, null,
                                                          UserSettings.Defaults.ShowThumbnails);

                        changed |= FormInputs.AddCheckBox("Show nodes thumbnails when hovering",
                                                          ref UserSettings.Config.EditorHoverPreview, null,
                                                          UserSettings.Defaults.EditorHoverPreview);

                        FormInputs.AddVerticalSpace();
                        FormInputs.AddSectionHeader("Advanced");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Reset time after playback",
                                                          ref UserSettings.Config.ResetTimeAfterPlayback,
                                                          "After the playback is halted, the time will reset to the moment when the playback began. This feature proves beneficial for iteratively reviewing animations without requiring manual rewinding.",
                                                          UserSettings.Defaults.ResetTimeAfterPlayback);
                        changed |= FormInputs.AddCheckBox("Suspend invalidation of inactive time clips",
                                                          ref ProjectSettings.Config.TimeClipSuspending,
                                                          "An experimental optimization that avoids dirty flag evaluation of graph behind inactive TimeClips. This is only relevant for very complex projects and multiple parts separated by timelines.",
                                                          ProjectSettings.Defaults.TimeClipSuspending);

                        changed |= FormInputs.AddCheckBox("Warn before Lib modifications",
                                                          ref UserSettings.Config.WarnBeforeLibEdit,
                                                          "This warning pops up when you attempt to enter an Operator that ships with the application.\n" +
                                                          "If unsure, this is best left checked.",
                                                          UserSettings.Defaults.WarnBeforeLibEdit);

                        changed |= FormInputs.AddCheckBox("Suspend rendering when hidden",
                                                          ref UserSettings.Config.SuspendRenderingWhenHidden,
                                                          "Suspend rendering and update when Tooll's editor window is minimized. This will reduce energy consumption significantly.",
                                                          UserSettings.Defaults.SuspendRenderingWhenHidden);

                        break;
                    case Categories.Theme:
                        FormInputs.AddSectionHeader("Color Theme");
                        FormInputs.AddVerticalSpace();

                        ColorThemeEditor.DrawEditor();
                        break;
                    case Categories.Project:
                    {
                        FormInputs.AddSectionHeader("Project specific settings");

                        var projectSettingsChanged = false;
                        CustomComponents.HelpText("These settings only when playback as executable");
                        FormInputs.AddVerticalSpace();

                        FormInputs.SetIndentToLeft();

                        projectSettingsChanged |= FormInputs.AddCheckBox("Enable Playback Control",
                                                                         ref ProjectSettings.Config.EnablePlaybackControlWithKeyboard,
                                                                         "Users can use cursor left/right to skip through time\nand space key to pause playback\nof exported executable.",
                                                                         ProjectSettings.Defaults.EnablePlaybackControlWithKeyboard);
                        
                        projectSettingsChanged |= CustomComponents.DrawDropdown(selectedValue: ref ProjectSettings.Config.DefaultWindowMode,
                                                                                label: "Default Export Window Mode",
                                                                                tooltip: "The default window mode when exporting an executable.",
                                                                                getDisplayTextFunc: value => value.ToString(),
                                                                                values: Enum.GetValues<WindowMode>(),
                                                                                labelOnSameLine: true);
                        if (projectSettingsChanged)
                            ProjectSettings.Save();

                        FormInputs.SetIndentToParameters();

                        break;
                    }
                    case Categories.Midi:
                    {
                        FormInputs.AddSectionHeader("Midi");

                        if (ImGui.Button("Rescan devices"))
                        {
                            MidiInConnectionManager.Rescan();
                            MidiOutConnectionManager.Init();
                        }

                        {
                            FormInputs.AddVerticalSpace();
                            ImGui.TextUnformatted("Limit captured MIDI devices...");
                            CustomComponents
                               .HelpText("This can be useful it avoid capturing devices required by other applications.\nEnter one search string per line...");

                            var limitMidiDevices = string.IsNullOrEmpty(ProjectSettings.Config.LimitMidiDeviceCapture)
                                                       ? string.Empty
                                                       : ProjectSettings.Config.LimitMidiDeviceCapture;

                            if (ImGui.InputTextMultiline("##Limit MidiDevices", ref limitMidiDevices, 2000, new Vector2(-1, 100)))
                            {
                                changed = true;
                                ProjectSettings.Config.LimitMidiDeviceCapture = string.IsNullOrEmpty(limitMidiDevices) ? null : limitMidiDevices;
                                MidiInConnectionManager.Rescan();
                            }

                            FormInputs.AddVerticalSpace();
                        }
                        FormInputs.SetIndentToLeft();
                        changed |= FormInputs.AddCheckBox("Enable Midi snapshot LEDs",
                                                          ref ProjectSettings.Config.EnableMidiSnapshotIndication,
                                                          "With selected midi controllers like APC Mini and APC40, Tooll will highlight LEDs for available and active snapshots. This requires an active MIDI out channel which will interfere with the [MidiOut] operator.\nChanging this requires a restart.",
                                                          ProjectSettings.Defaults.EnableMidiSnapshotIndication);

                        FormInputs.AddVerticalSpace();
                        FormInputs.SetIndentToParameters();
                        break;
                    }
                    case Categories.SpaceMouse:
                        FormInputs.AddSectionHeader("Space Mouse");

                        CustomComponents.HelpText("These settings only apply with a connected space mouse controller");
                        FormInputs.AddVerticalSpace();

                        changed |= FormInputs.AddFloat("Smoothing",
                                                       ref UserSettings.Config.SpaceMouseDamping,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddFloat("Move Speed",
                                                       ref UserSettings.Config.SpaceMouseMoveSpeedFactor,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddFloat("Rotation Speed",
                                                       ref UserSettings.Config.SpaceMouseRotationSpeedFactor,
                                                       0.0f, 10f, 0.01f, true);
                        break;

                    case Categories.Keyboard:
                        FormInputs.AddSectionHeader("Keyboard Shortcuts");
                        CustomComponents.HelpText("The keyboard layout can't be edited yet.");

                        if (ImGui.BeginTable("Shortcuts", 2,
                                             ImGuiTableFlags.BordersInnerH))
                        {
                            foreach (var value in Enum.GetValues<UserActions>())
                            {
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                var actionName = CustomComponents.HumanReadablePascalCase(Enum.GetName(value));
                                var shortcuts = KeyboardBinding.ListKeyboardShortcuts(value, false);
                                var hasShortcut = !string.IsNullOrEmpty(shortcuts);
                                ImGui.PushStyleColor(ImGuiCol.Text, hasShortcut ? UiColors.Text : UiColors.TextMuted.Rgba);
                                ImGui.TextUnformatted(actionName);

                                ImGui.TableSetColumnIndex(1);

                                if (hasShortcut)
                                {
                                    ImGui.PushFont(Fonts.FontBold);
                                    ImGui.TextUnformatted(shortcuts);
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                }
                            }

                            ImGui.EndTable();
                        }

                        break;
                }

                if (changed)
                    UserSettings.Save();
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}