using AFSPacker;
using RECV_Editor.File_Formats;
using System;
using System.IO;

namespace RECV_Editor
{
    abstract class RECV
    {
        public enum Platforms { PS2, GameCube }

        public delegate void UpdateStatusDelegate(ProgressInfo progress);

        public struct ProgressInfo
        {
            public string statusText;
            public int progressValue;
            public int maxProgressValue;

            public ProgressInfo(string statusText, int progressValue, int maxProgressValue)
            {
                this.statusText = statusText;
                this.progressValue = progressValue;
                this.maxProgressValue = maxProgressValue;
            }
        }

        public abstract string[] LanguageNames { get; }
        protected abstract string[] LanguageCodes { get; }
        protected abstract int[] LanguageIndices { get; }

        protected abstract int DiscCount { get; }
        protected abstract bool IsBigEndian { get; }

        public abstract void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress);
        public abstract void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress);

        public Table GetTableFromLanguage(string tablesFolder, int languageIndex)
        {
            string tableFile = $"{Path.Combine(tablesFolder, LanguageCodes[languageIndex])}.tbl";

            if (!File.Exists(tableFile))
            {
                throw new FileNotFoundException($"Table file \"{tableFile}\" has not been found.");
            }

            return new Table(tableFile);
        }

        public static RECV GetRECV(Platforms platform)
        {
            switch (platform)
            {
                case Platforms.PS2: return new RECV_PS2();
                case Platforms.GameCube: return new RECV_GameCube();
                default: throw new NotImplementedException($"Platform {platform} is not implemented.");
            }
        }

        protected void AFS_NotifyProgress(AFS.NotificationTypes type, string message)
        {
            Logger.Append(message, (Logger.LogTypes)type);
        }

        #region Extraction methods

        protected void ExtractSysmes(string sysmesFolder, string sysmesFileName, string outputFolder, Table table, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            string sysmesPath = Path.Combine(sysmesFolder, sysmesFileName);

            if (!File.Exists(sysmesPath))
            {
                throw new FileNotFoundException($"File \"{sysmesPath}\" does not exist!", sysmesPath);
            }

            Logger.Append($"Extracting \"{sysmesPath}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{sysmesFileName}\"...", ++currentProgressValue, maxProgressSteps));

            ALD.Extract(sysmesPath, outputFolder, table, IsBigEndian);
        }

        protected void ExtractRdxLnk(string rdxLnkFolder, string rdxLinkFileName, string outputFolder, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            string rdxLnkPath = Path.Combine(rdxLnkFolder, rdxLinkFileName);

            if (!File.Exists(rdxLnkPath))
            {
                throw new FileNotFoundException($"File \"{rdxLnkPath}\" does not exist!", rdxLnkPath);
            }

            Logger.Append($"Extracting \"{rdxLnkPath}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{rdxLinkFileName}\"...", ++currentProgressValue, maxProgressSteps));

            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(rdxLnkPath, outputFolder);
            AFS.NotifyProgress -= AFS_NotifyProgress;
        }

        #endregion
    }
}