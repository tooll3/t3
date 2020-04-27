using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;

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
            _lastPlayback = playback;
            if(playback.TimeDisplayMode != Playback.TimeDisplayModes.Bars)
            {
                switch (playback.TimeDisplayMode)
                {
                    case Playback.TimeDisplayModes.Secs:
                        _standardRaster.UnitsPerSecond = 1;
                        break;
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

        private TimeRaster ActiveRaster
        {
            get
            {
                switch (_lastPlayback.TimeDisplayMode)
                {
                    case Playback.TimeDisplayModes.Bars:
                        return _beatRaster;
                    default:
                        return _standardRaster;
                }
            }
        }
        
        private Playback _lastPlayback;
        private readonly StandardTimeRaster _standardRaster = new StandardTimeRaster();
        private readonly BeatTimeRaster _beatRaster = new BeatTimeRaster();
    }
}