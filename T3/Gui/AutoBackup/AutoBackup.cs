using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using T3.Core.Logging;

namespace t3.Gui.AutoBackup
{
    public class AutoBackup : IDisposable
    {
        public int SecondsBetweenSaves { get; set; }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value == _enabled)
                    return;
                _enabled = value;
                if (_enabled)
                    Start();
                else
                    Stop();
            }
        }

        public AutoBackup()
        {
            SecondsBetweenSaves = 60*5;
            Enabled = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private void Start()
        {
            Stop();
            _autoEvent = new AutoResetEvent(false);
            _timer = new Timer(CreateBackupCallback, _autoEvent, 0, SecondsBetweenSaves*1000);
        }

        private void Stop()
        {
            if (_autoEvent != null)
                _autoEvent.WaitOne();

            if (_timer != null)
            {
                _timer.Dispose();
            }
                

            if (_autoEvent != null)
                _autoEvent.Dispose();
        }


        private static void CreateBackupCallback(Object stateInfo)
        {
            Log.Debug("CreateBackupCallback()");
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
            autoEvent.Reset();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            
//            var metaOpsToStore = App.Current.Model.MetaOpManager.ChangedMetaOperators.ToList();
//            var savedOperatorCount = metaOpsToStore.Count();

            //FIXME: can we check if home-op has been modified?

            // if (savedOperatorCount == 0)
            // {
            //     //Logger.Info("Skipped backup, nothing changed", savedOperatorCount);
            //     autoEvent.Set();
            //     return;
            // }

            var index = GetIndexOfLastBackup();
            index++;
            ReduceNumberOfBackups();

            var zipFilePath = Path.Join( BackupDirectory,$"#{index:D5}-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_fff")}.zip");
            var tempPath = @"Temp\" + Guid.NewGuid() + @"\";

            try
            {
                var tempConfigPath = tempPath + ConfigDirectory;
                var tempOperatorsPath = tempPath + OperatorsDirectory + @"\";

                var zipPath = Path.GetDirectoryName(zipFilePath);
                if (!Directory.Exists(zipPath))
                    Directory.CreateDirectory(zipPath);

                if (!Directory.Exists(tempConfigPath))
                    Directory.CreateDirectory(tempConfigPath);

                if (!Directory.Exists(tempOperatorsPath))
                    Directory.CreateDirectory(tempOperatorsPath);

                CopyDirectory(ConfigDirectory, tempConfigPath, "*");
                // App.Current.Model.StoreHomeOperator(tempConfigPath, clearChangedFlags:false);
                // App.Current.ProjectSettings.SaveAs(tempConfigPath + "ProjectSettings.json");
                // App.Current.UserSettings.SaveAs(tempConfigPath + "UserSettings.json");
                // App.Current.OperatorPresetManager.SavePresetsAs(tempConfigPath + "Presets.json");

                CopyDirectory(OperatorsDirectory, tempOperatorsPath, "*");
                //MetaManager.WriteOperators(metaOpsToStore, tempOperatorsPath, clearChangedFlags: false);

                var filesToBackup = new Dictionary<String, String[]>();
                filesToBackup[ConfigDirectory] = Directory.GetFiles(tempConfigPath, "*");
                filesToBackup[OperatorsDirectory] = Directory.GetFiles(tempOperatorsPath, "*");

                using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var fileEntry in filesToBackup)
                    {
                        foreach (var file in fileEntry.Value)
                        {
                            archive.CreateEntryFromFile(file, fileEntry.Key + @"\" + Path.GetFileName(file),
                                CompressionLevel.Fastest);
                        }
                    }
                }

                //var names = metaOpsToStore.Select(metaOperator => metaOperator.Name).ToArray();
                //Logger.Info("Backing up {0} changed ops: {1}", savedOperatorCount, String.Join(", ", names));
            }
            catch (Exception ex)
            {
                DeleteFile(zipFilePath);
                Log.Error("auto backup failed: {0}", ex.Message);
            }
            finally
            {
                DeletePath(tempPath);
            }

            autoEvent.Set();
        }

        private static void CopyDirectory(string sourcePath, string destPath, string searchPattern)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(file));
                File.Copy(file, dest);
            }

            foreach (string folder in Directory.GetDirectories(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(folder));
                CopyDirectory(folder, dest, searchPattern);
            }
        }
        
        private static void DeletePath(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                Log.Info("Failed to delete path:" + e.Message);
            }
        }

        private static void DeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                Log.Info("Failed to delete file:" + e.Message);
            }
        }

        public static void RestoreLast()
        {
            var lastFile = GetLatestsArchiveFile();
            if (lastFile == null)
                return;

            var latestArchiveName = lastFile.FullName;

            using (ZipArchive archive = ZipFile.Open(latestArchiveName, ZipArchiveMode.Read))
            {
                const string DESTINATION_DIRECTORY_NAME = ".";


                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    var completeFileName = Path.Combine(DESTINATION_DIRECTORY_NAME, file.FullName);

                    if (file.Name == "")
                    {
                        // Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }

                    if(File.Exists(completeFileName))
                    {
                        File.Delete(completeFileName);   
                    }
                    file.ExtractToFile(completeFileName, true);
                }
            }
        }


        public static DateTime? GetTimeOfLastBackup()
        {
            var lastFile = GetLatestsArchiveFile();
            if (lastFile == null)
                return null;

            var result = Regex.Match(lastFile.Name, @"(#\d\d\d\d\d)?-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");

            if (!result.Success)
                return null;
            
            var index = result.Groups[1].Value;
            var year = result.Groups[2].Value;
            var month = result.Groups[3].Value;
            var day = result.Groups[4].Value;
            var hour = result.Groups[5].Value;
            var min = result.Groups[6].Value;
            var second = result.Groups[7].Value;

            var timeFromName = year + "-" + month + "-" + day + " " + hour + ":" + min + ":" + second;

            var date = DateTime.Parse(timeFromName);
            return date;
        }


        public static int GetIndexOfLastBackup()
        {
            var lastFile = GetLatestsArchiveFile();
            if (lastFile == null)
                return -1;

            var result = Regex.Match(lastFile.Name, @"#(\d\d\d\d\d)-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");

            if (!result.Success)
                return -1;

            var index = int.Parse(result.Groups[1].Value);
            return index;
        }


        private static FileInfo GetLatestsArchiveFile()
        {
            if (!Directory.Exists(BackupDirectory))
                return null;

            var backupDirectory = new DirectoryInfo(BackupDirectory);
            return backupDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        }


        /*
         * Reduce the number of backups by removing some of the older backups. The older the backup the
         * less versions are kept. We're using the binary representation of the backup index to seperate
         * the deleted versions from the ones we keep.
         * 
         * This algorithm is a hard to describe in words, but it basically thins out the backup-copies 
         * according to their respective binary code:
         * 
         *     bits    significant bit
         *     43210   bit         theshold for 2 saves per generation
         *     ------- ----------- ------------------------------------   
         * 10. 01011 - 1           +0 keep(level0/1st)          <- example of 10 saved versions
         *  9. 01001 - 0           +0 keep(level0/2nd)      
         *  8. 01000 - 4           +1 keep(level1/1st)
         *  7. 00111 - 0            1 remove
         *  6. 00110 - 1           +1 keep(level1/2nd)
         *  5. 00101 - 0            2 remove
         *  4. 00100 - 2           +2 Keep(level2/1st)
         *  3. 00011 - 0            2 remove
         *  2. 00010 - 1            2 remove
         *  1. 00001 - 0            2 remove
         *  0. 00000   inf         +2 keep(level2/2nd)
         * 
         * This means that we're keeping N*log2 backups (e.g. 3*16 out of 65536 saved versions) where N is the backup density.
         */
        public static void ReduceNumberOfBackups(int backupDensity=3)
        {
            // Gather list of backups with indexes and find latest index
            var regexMatchIndex = new Regex(@"#(\d\d\d\d\d)-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");
            var backupFilepathsByIndex = new Dictionary<int, string>();
            var highestIndex = int.MinValue;

            if (!Directory.Exists(BackupDirectory))
                return;

            foreach (var filename in Directory.GetFiles(BackupDirectory))
            {
                var result = regexMatchIndex.Match(filename);
                if (!result.Success)
                    continue;

                var index = int.Parse(result.Groups[1].Value);
                if (index > highestIndex)
                    highestIndex = index;

                backupFilepathsByIndex[index] = filename;
            }
            
            // Iterate over all files and thin out the backups
            var limit = 0;
            var limitCount = 0;
            for (var i = highestIndex - 1; i >= 0; i--)
            {
                var b = GetSignificantBit(0xffffff - i) + 1;

                // Keep
                if (b > limit)
                {
                    limitCount++;
                    if (limitCount >= backupDensity)
                    {
                        limitCount = 0;
                        limit++;
                    }
                }
                // Remove
                else
                {
                    if (!backupFilepathsByIndex.ContainsKey(i))
                        continue;
                    
                    Log.Debug($"removing... old backup {backupFilepathsByIndex[i]} (level 2^{b})...");
                    File.Delete(backupFilepathsByIndex[i]);
                }
            }
        }

        /**
         * Get the significant bit in an integer
         */
        private static int GetSignificantBit(int n)
        {
            var a= new bool[32];
            var rest = n;

            // Break down integer into bits
            while(rest > 0) {
                var h = (int)Math.Floor( Math.Log(rest,2));
                rest = rest - (int)Math.Pow(2,h);
                a[h]=true;
            }

            rest=n+1; 
            while(rest > 0) {
                var h = (int)Math.Floor( Math.Log(rest,2));
                rest = rest - (int)Math.Pow(2,h);
                if(a[h] == false) {
                    return h;
                }
            }
            return 0;
        }
    

        private bool _enabled;
        private Timer _timer;
        private AutoResetEvent _autoEvent;
        private const string ConfigDirectory = ".t3";
        private const string OperatorsDirectory = @"Operators\Types";
        private const string BackupDirectory = ".backup";
    }
}
