using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Numerics;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.Graph.Rendering;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Windows
{
    public class ImageOutputCanvas : ScalableCanvas
    {
        public override IEnumerable<ISelectableNode> SelectableChildren => new List<ISelectableNode>();

        public void Update()
        {
            Current = this;
            UpdateCanvas();
            UpdateViewMode();
        }

        /// <summary>
        /// The image canvas that is currently being drawn from the UI.
        /// Note that <see cref="ImageOutputCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Draw()ing context.
        /// It is used by <see cref="Texture2dOutputUi"/> to draw its content.
        /// </summary>
        public static ImageOutputCanvas Current = null;


        public void DrawTexture(Texture2D texture)
        {
            if (texture == null)
                return;

            var size = new Vector2(texture.Description.Width, texture.Description.Height);
            var area = new ImRect(0, 0, size.X, size.Y);

            if (_viewMode == Modes.Fitted)
                ImageOutputCanvas.Current.FitAreaOnCanvas(area);

            var topLeft = Vector2.Zero;
            var topLeftOnScreen = ImageOutputCanvas.Current.TransformPosition(topLeft);
            ImGui.SetCursorScreenPos(topLeftOnScreen);

            var sizeOnScreen = ImageOutputCanvas.Current.TransformDirection(size);
            var srv = SrvManager.GetSrvForTexture(texture);
            ImGui.Image((IntPtr)srv, sizeOnScreen);

            string format = "";
            if (UserSettings.Config.ShowExplicitTextureFormatInOutputWindow)
            {
                format = srv.Description.Format.ToString();
            }
            else
            {
                switch (srv.Description.Format)
                {
                    case Format.R16G16B16A16_Float:
                        format = "RGBA:16";
                        break;
                    case Format.R8G8B8A8_SNorm:
                        format = "RGBA:8";
                        break;
                    default:
                        format = srv.Description.Format.ToString();
                        break;
                }
            }
            
            ImGui.PushFont(Fonts.FontSmall);
            var description = $"{size.X}x{size.Y}  {format}";
            var descriptionWidth = ImGui.CalcTextSize(description).X;

            ImGui.SetCursorScreenPos(new Vector2(WindowPos.X + (WindowSize.X - descriptionWidth) / 2,
                                                 WindowPos.Y + WindowSize.Y - 16));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f,0.8f,0.8f, 0.8f));
            ImGui.Text(description);
            ImGui.PopFont();
            ImGui.PopStyleColor();
        }


        public void SetViewMode(Modes newMode)
        {
            _viewMode = newMode;
            if (newMode == Modes.Pixel)
            {
                SetScaleToMatchPixels();
            }
        }


        /// <summary>
        /// Updated the view mode if user interacted 
        /// </summary>
        private void UpdateViewMode()
        {
            switch (_viewMode)
            {
                case Modes.Fitted:
                    if (UserScrolledCanvas || UserZoomedCanvas)
                        _viewMode = Modes.Custom;
                    break;

                case Modes.Pixel:
                    if (UserZoomedCanvas)
                        _viewMode = Modes.Custom;
                    break;
            }
        }


        public enum Modes
        {
            Fitted,
            Pixel,
            Custom,
        }

        private Modes _viewMode = Modes.Fitted;
    }
}
