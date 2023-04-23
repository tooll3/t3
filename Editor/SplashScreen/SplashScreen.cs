using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CppSharp.Utils.FSM;
using T3.Core.Resource;

namespace T3.Editor.SplashScreen;

internal static partial class SplashScreen
{
    private static readonly Size BaseDpi = new Size(96, 96);
    private static bool _isOpen;
    private static TextWriter _defaultConsoleOut;
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static Form _splashForm;

    public static void OpenSplashScreen(string imagePath)
    {
        if (_isOpen)
            throw new InvalidOperationException("Splash screen already open");
        _isOpen = true;

        var splashScreenThread = new Thread(() =>
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

                                                PrivateFontCollection fontCollection = new PrivateFontCollection();
                                                var fontPath = Path.Combine(ResourceManager.ResourcesFolder, "t3-editor", "fonts", "Roboto-Light.ttf");
                                                fontCollection.AddFontFile(fontPath);
                                                var logsTextBox = new Label
                                                                      {
                                                                          Dock = DockStyle.Bottom,
                                                                          AutoSize = false,
                                                                          TextAlign = ContentAlignment.BottomRight,
                                                                          BackColor = Color.Transparent,
                                                                          ForeColor = Color.Ivory,
                                                                          Text = @"Loading T3...",
                                                                          UseMnemonic = false,
                                                                          Font = new Font(fontCollection.Families[0], 8),
                                                                      };

                                                _splashForm.Controls.Add(logsTextBox);

                                                _defaultConsoleOut = Console.Out;
                                                var splashScreenLogWriter = new ControlWriter(logsTextBox);
                                                var consoleWriter = new DualTextWriter(_defaultConsoleOut, splashScreenLogWriter);

                                                // Redirect the console output to the logs TextBox
                                                Console.SetOut(consoleWriter);

                                                _splashForm.Show();

                                                Application.Run();
                                            });

        splashScreenThread.SetApartmentState(ApartmentState.STA);
        splashScreenThread.Start();
    }

    public static void CloseSplashScreen()
    {
        if (!_isOpen)
            throw new InvalidOperationException("Splash screen cannot be closed when it is not open");

        _isOpen = false;

        _splashForm.Invoke(() => _splashForm.Close());
        Console.SetOut(_defaultConsoleOut);
    }

    private static Size GetScaledSize(Image image)
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);

        var dpiX = graphics.DpiX;
        var dpiY = graphics.DpiY;

        var width = (int)(image.Width * (dpiX / BaseDpi.Width));
        var height = (int)(image.Height * (dpiY / BaseDpi.Height));

        return new Size(width, height);
    }
}