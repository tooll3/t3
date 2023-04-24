using System;
using System.Drawing;
using System.Windows.Forms;
using T3.Core.Logging;

namespace T3.Editor.SplashScreen;

internal class SplashScreen : ILogWriter
{
    
    private delegate void SafeCallDelegate(string text);
    
    public void Show(string imagePath)
    {
        var backgroundImage = Image.FromFile(imagePath);
        var imageSize = GetScaledSize(backgroundImage);

        _splashForm = new Form
                          {
                              FormBorderStyle = FormBorderStyle.None,
                              StartPosition = FormStartPosition.CenterScreen,
                              BackgroundImage = backgroundImage,
                              BackgroundImageLayout = ImageLayout.Stretch,
                              Size = imageSize
                          };

        _logMessageLabel = new Label
                               {
                                   Dock = DockStyle.Bottom,
                                   AutoSize = false,
                                   TextAlign = ContentAlignment.BottomRight,
                                   BackColor = Color.Transparent,
                                   ForeColor = Color.Ivory,
                                   Text = @"Loading T3...",
                                   UseMnemonic = false,
                                   Font = new Font("Arial", 8),
                               };
        _splashForm.Controls.Add(_logMessageLabel);

        _splashForm.Show();
        _splashForm.Refresh();
        _splashForm.TopMost = true;
    }

    public void Close()
    {
        _splashForm.Close();
        _splashForm = null;
    }

    private static Size GetScaledSize(Image image)
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);

        var dpiX = graphics.DpiX;
        var dpiY = graphics.DpiY;

        var width = (int)(image.Width * (dpiX / _baseDpi.Width));
        var height = (int)(image.Height * (dpiY / _baseDpi.Height));

        return new Size(width, height);
    }


    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    
    private void WriteTextSafe(string text)
    {
        if (_logMessageLabel.InvokeRequired)
        {
            var d = new SafeCallDelegate(WriteTextSafe);
            _logMessageLabel.Invoke(d, text);
            
        }
        else
        {
            _logMessageLabel.Text = text;
            _logMessageLabel.Refresh();
        }
    }
    
    
    public LogEntry.EntryLevel Filter { get; set; }

    public void ProcessEntry(LogEntry entry)
    {
        if (_logMessageLabel == null)
            return;
        
        WriteTextSafe(entry.Message);
    }
    
    private static readonly Size _baseDpi = new(96, 96);
    private Form _splashForm;
    private Label _logMessageLabel;

}