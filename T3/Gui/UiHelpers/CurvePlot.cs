using ImGuiNET;

namespace T3.Gui.UiHelpers
{
    public class CurvePlot
    {
        private static int _sampleOffset;
        private readonly int _framerateSampleCount;

        public CurvePlot(int length = 500)
        {
            _framerateSampleCount = length;
            _graphValues = new float[_framerateSampleCount];
        }
        
        public void Draw(float value)
        {
            ImGui.PlotLines($"{value:0.00}", ref _graphValues[0], _framerateSampleCount, _sampleOffset, "");
            _graphValues[_sampleOffset] = value;
            _sampleOffset = (_sampleOffset + 1) % _framerateSampleCount;
        }

        public void Reset(float clearValue = 0)
        {
            for (var index = 0; index < _framerateSampleCount; index++)
            {
                _graphValues[index] = clearValue;
            }
        }

        private static float[] _graphValues;
    }
}