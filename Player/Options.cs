using System.Drawing;
using CommandLine;

namespace T3.Player;

internal class Options
{
    [Option(Default = false, Required = false, HelpText = "Disable vsync")]
    public bool NoVsync { get; set; }

    [Option(Default = 1920, Required = false, HelpText = "Defines the width")]
    public int Width { get; set; }

    [Option(Default = 1080, Required = false, HelpText = "Defines the height")]
    public int Height { get; set; }

    public Size Size => new(Width, Height);

    [Option(Default = false, Required = false, HelpText = "Run in windowed mode")]
    public bool Windowed { get; set; }

    [Option(Default = false, Required = false, HelpText = "Loops the demo")]
    public bool Loop { get; set; }

    [Option(Default = true, Required = false, HelpText = "Show log messages.")]
    public bool Logging { get; set; }
}