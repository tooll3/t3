using System.IO;

namespace T3.Editor.SplashScreen;

internal static partial class SplashScreen
{
    private class DualTextWriter : TextWriter
    {
        private readonly TextWriter _originalConsoleOut;
        private readonly TextWriter _controlWriter;

        public DualTextWriter(TextWriter originalConsoleOut, TextWriter controlWriter)
        {
            _originalConsoleOut = originalConsoleOut;
            _controlWriter = controlWriter;
        }

        public override void Write(char value)
        {
            _originalConsoleOut.Write(value);
            _controlWriter.Write(value);
        }

        public override void Write(string value)
        {
            _originalConsoleOut.Write(value);
            _controlWriter.Write(value);
        }

        public override System.Text.Encoding Encoding => _originalConsoleOut.Encoding;
    }
}