using ImGuiNET;
using System;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    public class UIControlledSetting
    {
        /// <summary>
        /// The generated unique label
        /// </summary>
        public string uniqueLabel { get; private set; }

        /// <summary>
        /// The label provided in the constructor
        /// Unused by this class and here for convenience, but often best left ignored
        /// </summary>
        public string cleanLabel { get; private set; }

        private string tooltip;
        private string additionalNotes;
        private Func<bool> guiFunc;
        private Action OnValueChanged;

        // Cheaper than GUIDs
        // In case we want to have the same variable be changeable with different UI controls
        // or if multiple settings have the same label
        static ushort countForUniqueID = ushort.MaxValue;

        /// <summary>
        /// For the sake of simple use of the optional parameters and populating/maintaining many settings, the recommended way to call this constructor is:
        /// <code>
        /// new UIControlledSetting
        /// (
        ///    label: "My Setting",
        ///    tooltip: "The global scale of all rendered UI in the application",
        ///    guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.UiScaleFactor, 0.01f, 0.5f, 3f),
        ///    OnValueChanged: () => //your action
        /// );
        /// </code>
        /// </summary>
        /// <param name="label">The label to display next to the gui control</param>
        /// <param name="guiFunc">The <see cref="ImGuiNET"/> - based function that draws the setting control and
        /// returns true if the control was changed. The input to this function see is a unique ID based on the label provided</param>
        /// <param name="tooltip">Tooltip displayed when hovering over the control</param>
        /// <param name="additionalNotes">Additional notes displayed alongside the tooltip</param>
        /// <param name="OnValueChanged">An action performed when the value is changed</param>
        public UIControlledSetting(string label, Func<string, bool> guiFunc, string tooltip = null, string additionalNotes = null, Action OnValueChanged = null)
        {
            cleanLabel = label;
            uniqueLabel = $"{label}##{countForUniqueID--}";

            this.guiFunc = () => guiFunc(uniqueLabel);
            this.tooltip = tooltip;
            this.additionalNotes = additionalNotes;
            this.OnValueChanged = OnValueChanged;
        }

        /// <summary>
        /// Draws the GUI for this setting using the Func provided in its constructor
        /// </summary>
        /// <returns>True if changed, false if unchanged.
        /// If an Action was provided in constructor, it will be executed when value is changed. </returns>
        public bool DrawGUIControl()
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip(tooltip);
                }
            }

            var changed = guiFunc.Invoke();

            if(changed)
            {
                OnValueChanged?.Invoke();
            }

            return changed;
        }
    }
}