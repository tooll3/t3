using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine.Raster;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// A small helper class that switches the drawing of time rasters depending on the
    /// setting in <see cref="Playback"/> 
    /// </summary>
    public class TimeRasterSwitcher:IValueSnapAttractor
    {
        public void Draw(Playback playback)
        {
            if(UserSettings.Config.TimeDisplayMode != Playback.TimeDisplayModes.Bars)
            {
                switch (UserSettings.Config.TimeDisplayMode)
                {
                    case Playback.TimeDisplayModes.F30:
                        _standardRaster.UnitsPerSecond = 30;
                        break;
                    case Playback.TimeDisplayModes.F60:
                        _standardRaster.UnitsPerSecond = 60;
                        break;
                }
            }
            ActiveRaster?.Draw(playback);
        }
        
        public SnapResult CheckForSnap(double value, float canvasScale)
        {
            return ActiveRaster?.CheckForSnap(value, canvasScale);
        }

        private AbstractTimeRaster ActiveRaster
        {
            get
            {
                switch (UserSettings.Config.TimeDisplayMode)
                {
                    case Playback.TimeDisplayModes.Bars:
                        return _beatRaster;
                    case Playback.TimeDisplayModes.Secs:
                        return _siTimeRaster;
                    default:
                        return _standardRaster;
                }
            }
        }
        
        private readonly StandardValueRaster _standardRaster = new StandardValueRaster();
        private readonly BeatTimeRaster _beatRaster = new BeatTimeRaster();
        private readonly SiTimeRaster _siTimeRaster = new SiTimeRaster();
    }
}