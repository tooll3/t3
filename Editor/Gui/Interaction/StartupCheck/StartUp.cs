using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using T3.Core.Logging;

namespace T3.Editor.Gui.Interaction.StartupCheck
{
    /// <summary>
    /// A helper that provides an option use resort backup with startup failed
    /// </summary>
    public static class StartUp
    {
        public static void FlagBeginStartupSequence()
        {
            if (File.Exists(StartUpLockFilePath))
            {
                ShowMessageBox();
            }

            File.WriteAllText(StartUpLockFilePath, "Startup " + DateTime.Now);
        }

        public static void FlagStartupSequenceComplete()
        {
            if (!File.Exists(StartUpLockFilePath))
            {
                Log.Debug("Strange. Startup file missing?");
            }
            else
            {
                File.Delete(StartUpLockFilePath);
            }
        }

        private static void ShowMessageBox()
        {
            var isThereABackup = !string.IsNullOrEmpty(AutoBackup.AutoBackup.GetLatestArchiveFilePath());
            if (!isThereABackup)
            {
                var result2 = MessageBox.Show("It looks like the last startup failed.\nSadly there is no backup yet.", "Startup Failed",
                                              MessageBoxButtons.RetryCancel);
                if (result2 != DialogResult.Retry)
                {
                    Application.Exit();
                }

                return;
            }

            Log.Debug("StartUpProgress lock file exists?");

            var timeOfLastBackup = AutoBackup.AutoBackup.GetTimeOfLastBackup();
            var timeSpan = GetReadableRelativeTime(timeOfLastBackup);

            const string caption = "Oh no! Start up problems...";
            string message = "It looks like last start up was incomplete.\n\n" +
                             "You can press...\n\n" +
                             $"  YES to restore latest backup {timeSpan}\n" +
                             "  NO to open a link to documentation\n" +
                             "  CANCEL to attempt starting anyways.\n";

            var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNoCancel);
            switch (result)
            {
                case DialogResult.Yes:
                {
                    var wasSuccessful = AutoBackup.AutoBackup.RestoreLast();
                    if (wasSuccessful)
                    {
                        FlagStartupSequenceComplete();
                        MessageBox.Show("Backup restored. Click OK to restart.\nFingers crossed.", "Complete", MessageBoxButtons.OK);
                        //Application.Exit();
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show("Restoring backup failed.\nYou might want to try an earlier archive in .t3\\backup\\...", "Failed",
                                        MessageBoxButtons.OK);
                        Environment.Exit(0);
                    }

                    break;
                }
                case DialogResult.No:
                    StartupValidation.OpenUrl(HelpUrl);
                    Environment.Exit(0);
                    break;
                
                case DialogResult.Cancel:
                    break;
            }
        }

        private static string GetReadableRelativeTime(DateTime? timeOfLastBackup)
        {
            if (timeOfLastBackup == null)
                return "Unknown time";

            var timeSinceLastBack = DateTime.Now - timeOfLastBackup;
            var minutes = timeSinceLastBack.Value.TotalMinutes;
            if (minutes < 120)
            {
                return $"{minutes:0} minutes ago";
            }

            var hours = timeSinceLastBack.Value.TotalHours;
            if (hours < 30)
            {
                return $"{hours:0.0} hours ago";
            }

            var days = timeSinceLastBack.Value.TotalDays;
            return $"{days:0.0} days ago";
        }

        private const string HelpUrl = "https://github.com/still-scene/t3/wiki/installation#setup-and-installation";
        private const string StartUpLockFilePath = @".t3\startingUp";
    }
}