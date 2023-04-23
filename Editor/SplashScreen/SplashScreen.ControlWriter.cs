using System.IO;
using System.Text;
using System.Windows.Forms;

namespace T3.Editor.SplashScreen;

internal static partial class SplashScreen
{
    private class ControlWriter : TextWriter
    {
        private readonly Control _control;
        private readonly StringBuilder _buffer;

        public ControlWriter(Control control)
        {
            _control = control;
            _buffer = new StringBuilder();
        }

        public override void Write(char value)
        {
            lock (_buffer)
            {
                if (value == '\n')
                {
                    UpdateControlText(_buffer);
                }
                else
                {
                    _buffer.Append(value);
                }
            }
        }

        public override void Write(string value)
        {
            lock (_buffer)
            {
                _buffer.Append(value);
                if (value.Contains('\n'))
                {
                    UpdateControlText(_buffer);
                }
            }
        }

        private readonly object _updateLock = new();
        private bool _updateInProgress;

        private void UpdateControlText(StringBuilder builder)
        {
            var logString = builder.ToString();
            var indexOf = logString.IndexOf('\n');

            if (indexOf < 0)
                return;

            builder.Remove(0, indexOf + 1);

            lock (_updateLock)
            {
                if (_updateInProgress)
                    return;

                _updateInProgress = true;

                _control.BeginInvoke(() =>
                                     {
                                         _control.Text = logString;
                                         lock (_updateLock)
                                         {
                                             _updateInProgress = false;
                                         }
                                     });
            }
        }
        
        public override Encoding Encoding => Encoding.ASCII;
    }
}