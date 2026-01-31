using System;
using System.Diagnostics;
using System.IO;

namespace AlignTaiko.Gui.Services
{
    internal sealed class BackupService
    {
        public string? LastBackupFolder { get; private set; }

        /// <summary>
        /// baseDir の配下に AlignTaiko_Backup\yyyyMMdd_HHmmss を作って返す。
        /// </summary>
        public string CreateBackupRunFolder(string baseDir)
        {
            var root = Path.Combine(baseDir, "AlignTaiko_Backup");
            Directory.CreateDirectory(root);

            var runFolder = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(runFolder);

            LastBackupFolder = runFolder;
            return runFolder;
        }

        public string BackupFileTo(string srcPath, string backupRunFolder)
        {
            var dstPath = Path.Combine(
                backupRunFolder,
                Path.GetFileName(srcPath)
            );

            File.Copy(srcPath, dstPath, overwrite: false);
            return dstPath;
        }

        public bool CanOpenLastFolder()
            => !string.IsNullOrWhiteSpace(LastBackupFolder) && Directory.Exists(LastBackupFolder);

        public void OpenLastFolder()
        {
            if (!CanOpenLastFolder()) return;

            Process.Start(new ProcessStartInfo
            {
                FileName = LastBackupFolder!,
                UseShellExecute = true,
            });
        }
    }
}
