using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using T3.Gui.Windows;

namespace t3.Gui.Interaction.StartupCheck
{
    /// <summary>
    /// Looks for required files and folders
    /// and shows a warning popup instead of an exception...
    /// </summary>
    public static class StartupValidation
    {
        public static void CheckInstallation()
        {
            new Check
                {
                    RequiredFilePaths = new List<string>()
                                            {
                                                @"Resources\",
                                                @"Resources\t3-editor\images\t3-icons.png",
                                                @"Resources\t3-editor\images\t3.ico",
                                                @"Resources\t3-editor\fonts\Roboto-Regular.ttf",
                                            },
                    Message = @"Please make sure to set the correct start up directory.\n ",
                    URL = "https://github.com/still-scene/t3/wiki/installation#setting-the-startup-directory-in-visual-studio",
                }.Do();
            

            
            new Check
                {
                    RequiredFilePaths = new List<string>()
                                            {  
                                                WindowManager.LayoutPath + "layout1.json",
                                                @"T3\bin\Release\net6.0-windows\bass.dll",
                                            },
                    Message = "Please run Install/install.bat.",
                    URL = "https://github.com/still-scene/t3/wiki/installation#setup-and-installation",
                }.Do();
            
            new Check
                {
                    RequiredFilePaths = new List<string>()
                                            {
                                                @"Player\bin\Release\net6.0-windows\Player.exe",
                                            },
                    Message = "This will prevent you from exporting as executable.\nPlease rebuild your solution.",
                    URL = "https://github.com/still-scene/t3/wiki/installation#setup-and-installation",
                }.Do();            
        }

        private struct Check
        {
            public List<string> RequiredFilePaths;
            public string Message;
            public string URL;

            public void Do()
            {
                var missingPaths = new List<string>();
                foreach (var filepath in RequiredFilePaths)
                {
                    if (filepath.EndsWith(@"\"))
                    {
                        if (!Directory.Exists(filepath))
                        {
                            missingPaths.Add(filepath);
                        }
                    }
                    else if (!File.Exists(filepath))
                    {
                        missingPaths.Add(filepath);
                    }
                }

                if (missingPaths.Count <= 0)
                    return;

                const string caption = "Tooll3 setup looks incomplete";

                var sb = new StringBuilder();
                sb.Append("We can't find the following files...\n\n  " + string.Join("\n  ", missingPaths));
                sb.Append("\n\n");
                sb.Append(Message);
                if (!string.IsNullOrEmpty(URL))
                {
                    sb.Append("\n\n");
                    sb.Append("Click Yes to get help");
                }

                const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

                var result = MessageBox.Show(sb.ToString(), caption, buttons);
                if (result == DialogResult.Yes)
                {
                    OpenUrl(URL);
                }
                Application.Exit();
                Application.ExitThread();
            }

            private static void OpenUrl(string url)
            {
                try
                {
                    Process.Start(url);
                }
                catch
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}