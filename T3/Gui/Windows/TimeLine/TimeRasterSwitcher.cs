using T3.Gui.Interaction.Snapping;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// A small helper class that switches the drawing of time rasters depending on the
    /// setting in <see cref="ClipTime"/> 
    /// </summary>
    public class TimeRasterSwitcher:IValueSnapAttractor
    {
        public void Draw(ClipTime clipTime)
        {
            _lastClipTime = clipTime;
            if(clipTime.TimeMode != ClipTime.TimeModes.Bars)
            {
                switch (clipTime.TimeMode)
                {
                    case ClipTime.TimeModes.Seconds:
                        _standardRaster.UnitsPerSecond = 1;
                        break;
                    case ClipTime.TimeModes.F30:
                        _standardRaster.UnitsPerSecond = 30;
                        break;
                    case ClipTime.TimeModes.F60:
                        _standardRaster.UnitsPerSecond = 60;
                        break;
                }
            }
            ActiveRaster?.Draw(clipTime);
        }
        
        public SnapResult CheckForSnap(double value)
        {
            return ActiveRaster?.CheckForSnap(value);
        }

        private TimeRaster ActiveRaster
        {
            get
            {
                switch (_lastClipTime.TimeMode)
                {
                    case ClipTime.TimeModes.Bars:
                        return _beatRaster;
                    default:
                        return _standardRaster;
                }
            }
        }
        
        private ClipTime _lastClipTime;
        private readonly StandardTimeRaster _standardRaster = new StandardTimeRaster();
        private readonly BeatTimeRaster _beatRaster = new BeatTimeRaster();
    }
}