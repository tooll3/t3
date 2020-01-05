using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace T3.Gui.Windows.Output
{
    public class ResolutionHandling
    {
        public static void DrawSelector(ref Resolution selectedResolution, EditResolutionDialog resolutionDialog)
        {
            resolutionDialog.Draw(_resolutionForEdit);

            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("##ResolutionSelection", selectedResolution.Title))
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
                    _resolutionForEdit =  new Resolution("untitled", 1,1);
                    Resolutions.Add(_resolutionForEdit);
                    resolutionDialog.ShowNextFrame();
                }
            }
        }
        

        public static readonly List<Resolution> Resolutions = new List<Resolution>()
                                                               {
                                                                   new Resolution("Adaptive", 0, 0),
                                                                   new Resolution("1080p", 1920, 1080),
                                                                   new Resolution("4k", 3940, 2160),
                                                               };

        private static Resolution _resolutionForEdit;
        
        public class Resolution
        {
            public Resolution(string title, int width, int height)
            {
                Title = title;
                Width = width;
                Height = height;
            }

            public string Title;
            public int Width;
            public int Height;

            public bool IsValid
            {
                get
                {
                    return !string.IsNullOrEmpty(Title)
                                   && !Resolutions.Any(res => res != this && res.Title == Title)
                                   && Width > 0 && Width < 16384
                                   && Height > 0 && Height < 16384;
                }
            }

            public bool IsAdaptive
            {
                get { return Height == 0 || Width == 0; }
            }
        }
    }
}