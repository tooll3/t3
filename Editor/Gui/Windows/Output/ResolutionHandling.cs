using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.Output
{
    public static class ResolutionHandling
    {
        public static void DrawSelector(ref Resolution selectedResolution, EditResolutionDialog resolutionDialog)
        {
            resolutionDialog?.Draw(_resolutionForEdit);

            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("##ResolutionSelection", selectedResolution.Title, ImGuiComboFlags.HeightLargest))
            {
                foreach (var resolution in Resolutions.ToArray())
                {
                    ImGui.PushID(resolution.Title);
                    if (ImGui.Selectable(resolution.Title, resolution == selectedResolution))
                    {
                        selectedResolution = resolution;
                    }

                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            if (ImGui.MenuItem("Remove"))
                                                            {
                                                                Resolutions.Remove(resolution);
                                                            }
                                                        },
                                                        "#bla");
                    ImGui.PopID();
                }

                if (ImGui.Selectable("+ Add"))
                {
                    _resolutionForEdit = new Resolution("untitled", 256, 256);
                    Resolutions.Add(_resolutionForEdit);
                    resolutionDialog?.ShowNextFrame();
                    selectedResolution = _resolutionForEdit;
                }

                ImGui.EndCombo();
            }
        }


        public static readonly List<Resolution> Resolutions = new()
                                                                  {
                                                                      new("Fill", 0, 0, useAsAspectRatio: true),
                                                                      new("1:1", 1, 1, useAsAspectRatio: true),
                                                                      new("16:9", 16, 9, useAsAspectRatio: true),
                                                                      new("4:3", 4, 3, useAsAspectRatio: true),
                                                                      new("480p", 850, 480),
                                                                      new("720p", 1280, 720),
                                                                      new("1080p", 1920, 1080),
                                                                      new("4k", 1920 * 2, 1080 * 2),
                                                                      new("8k", 1920 * 4, 1080 * 4),
                                                                      new("4k Portrait", 1080 * 2, 1920 * 2),
                                                                  };

        public static readonly Resolution DefaultResolution = Resolutions[0];
        private static Resolution _resolutionForEdit = new("untitled", 255, 255);

        public class Resolution
        {
            public Resolution(string title, int width, int height, bool useAsAspectRatio = false)
            {
                Title = title;
                Size.Width = width;
                Size.Height = height;
                UseAsAspectRatio = useAsAspectRatio;
            }

            public string Title;
            public Size2 Size;
            public bool UseAsAspectRatio;

            public Size2 ComputeResolution()
            {
                if (!UseAsAspectRatio)
                    return Size;

                var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
                if (Size.Width <= 0 || Size.Height <= 0)
                {
                    var borderSize = (int)ImGui.GetStyle().WindowBorderSize;
                    return new Size2((int)windowSize.X - 2 * borderSize,
                                     (int)windowSize.Y - 2 * borderSize);
                }

                var windowAspectRatio = windowSize.X / windowSize.Y;
                var requestedAspectRatio = (float)Size.Width / Size.Height;

                return (requestedAspectRatio > windowAspectRatio)
                           ? new Size2((int)windowSize.X, (int)(windowSize.X / requestedAspectRatio))
                           : new Size2((int)(windowSize.Y * requestedAspectRatio), (int)windowSize.Y);
            }

            public bool IsValid
            {
                get
                {
                    return !string.IsNullOrEmpty(Title)
                           && !Resolutions.Any(res => res != this && res.Title == Title)
                           && Size.Width > 0 && Size.Width < 16384
                           && Size.Height > 0 && Size.Height < 16384;
                }
            }
        }
    }
}