using AFSLib;
using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        protected abstract Platforms Platform { get; }
        protected abstract int DiscCount { get; }
        protected abstract bool IsBigEndian { get; }

        protected abstract int MaxExtractionProgressSteps { get; }
        protected abstract int MaxInsertionProgressSteps { get; }

        public const string JPN_LANGUAGE_CODE = "JPN";
        public const string USA_LANGUAGE_CODE = "USA";
        public const string ENG_LANGUAGE_CODE = "ENG";
        public const string GER_LANGUAGE_CODE = "GER";
        public const string FRA_LANGUAGE_CODE = "FRA";
        public const string SPA_LANGUAGE_CODE = "SPA";
        public const string ITA_LANGUAGE_CODE = "ITA";

        public const string JPN_LANGUAGE_NAME = "Japanese";
        public const string USA_LANGUAGE_NAME = "English (USA)";
        public const string ENG_LANGUAGE_NAME = "English";
        public const string GER_LANGUAGE_NAME = "German";
        public const string FRA_LANGUAGE_NAME = "French";
        public const string SPA_LANGUAGE_NAME = "Spanish";
        public const string ITA_LANGUAGE_NAME = "Italian";

        protected const string RDX_EXTRACTED_FOLDER_SUFFIX = "_extract";

        public abstract void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress);

        protected abstract void InsertDisc(string inputFolder, string outputFolder, string originalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress);

        public void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            if (string.IsNullOrEmpty(originalDataFolder))
            {
                throw new ArgumentNullException(nameof(originalDataFolder));
            }

            if (!Directory.Exists(originalDataFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{originalDataFolder}\" does not exist!");
            }

            if (string.IsNullOrEmpty(tablesFolder))
            {
                throw new ArgumentNullException(nameof(tablesFolder));
            }

            Table table = GetTableFromLanguage(tablesFolder, language);

            // Begin process

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentProgress = 0;

            Logger.Append("Insert all process has begun. ----------------------------------------------------------------------");

            for (int disc = 1; disc < DiscCount + 1; disc++)
            {
                string discFolderName = DiscCount > 1 ? $"Disc {disc}" : string.Empty;
                string discInputFolder = Path.Combine(inputFolder, Platform.ToString(), discFolderName);

                if (!Directory.Exists(discInputFolder))
                {
                    throw new DirectoryNotFoundException($"Directory \"{discInputFolder}\" does not exist!");
                }

                string discOutputFolder = Path.Combine(outputFolder, Platform.ToString(), discFolderName);

                string discOriginalDataFolder = Path.Combine(originalDataFolder, Platform.ToString(), discFolderName);

                if (!Directory.Exists(discOriginalDataFolder))
                {
                    throw new DirectoryNotFoundException($"Directory \"{discOriginalDataFolder}\" does not exist!");
                }

                InsertDisc(discInputFolder, discOutputFolder, discOriginalDataFolder, table, language, disc, progress, ref currentProgress);
            }

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgress, MaxInsertionProgressSteps));
            Logger.Append("Insert all process has finished. -------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


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

        protected void AFS_NotifyProgress(NotificationType type, string message)
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

        protected void ExtractRdxLnk(string rdxLnkFolder, string rdxLinkFileName, string outputFolder, int disc, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            string rdxLnkPath = Path.Combine(rdxLnkFolder, rdxLinkFileName);

            if (!File.Exists(rdxLnkPath))
            {
                throw new FileNotFoundException($"File \"{rdxLnkPath}\" does not exist!", rdxLnkPath);
            }

            Logger.Append($"Extracting \"{rdxLnkPath}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{rdxLinkFileName}\" (Disc {disc})...", ++currentProgressValue, maxProgressSteps));

            using (AFS afs = new AFS(rdxLnkPath))
            {
                afs.NotifyProgress += AFS_NotifyProgress;
                afs.ExtractAllEntriesToDirectory(outputFolder);
                afs.NotifyProgress -= AFS_NotifyProgress;
            }
        }

        #endregion

        #region Insertion Methods

        protected void InsertRdxFiles(string inputRdxLnkFolder, string[] outputRdxFiles, int language, int disc, Table table, Platforms platform, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            if (!Directory.Exists(inputRdxLnkFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputRdxLnkFolder}\" does not exist!");
            }

            RDX rdx = RDX.GetRDX(platform);

            int currentRdxFile = 1;
            currentProgressValue++;

            // Needed for Parallel.For, which can't have ref values.
            int currentProgress = currentProgressValue;

#if MULTITHREADING
            Parallel.For(0, outputRdxFiles.Length, (r) =>
#else
            for (int r = 0; r < outputRdxFiles.Length; r++)
#endif
            {
                string inputRdxPath = Path.Combine(inputRdxLnkFolder, Path.GetFileName(outputRdxFiles[r]) + RDX_EXTRACTED_FOLDER_SUFFIX);

                progress?.Report(new ProgressInfo($"Inserting RDX files (Disc {disc})... ({currentRdxFile++}/{outputRdxFiles.Length})", currentProgress, maxProgressSteps));

                if (!Directory.Exists(inputRdxPath))
                {
                    Logger.Append($"Exctracted RDX \"{inputRdxPath}\" not found. Skipping...");
#if MULTITHREADING
                    return;
#else
                    continue;
#endif
                }

                Logger.Append($"Inserting RDX file \"{inputRdxPath}\"...");

                byte[] rdxData = File.ReadAllBytes(outputRdxFiles[r]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                //File.WriteAllBytes(outputRdxFiles[r], rdxUncompressedData);

                using (MemoryStream ms = new MemoryStream(rdxUncompressedData.Length))
                {
                    ms.Write(rdxUncompressedData, 0, rdxUncompressedData.Length);
                    ms.Position = 0;

                    rdx.Insert(inputRdxPath, ms, Path.GetFileName(outputRdxFiles[r]), language, table);

                    rdxData = PRS.Compress(ms.ToArray());
                    //File.WriteAllBytes(outputRdxFiles[r] + ".unc", ms.ToArray());
                }

                File.WriteAllBytes(outputRdxFiles[r], rdxData);
#if MULTITHREADING
            });
#else
            }
#endif
        }

        protected void GenerateAfs(string inputDirectory, string outputAfsFile, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            Logger.Append($"Generating \"{outputAfsFile}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{outputAfsFile}\"...", ++currentProgress, MaxInsertionProgressSteps));

            string[] rdxFiles = Directory.GetFiles(inputDirectory);

            using (AFS afs = new AFS())
            {
                afs.AttributesInfoType = AttributesInfoType.NoAttributes;
                afs.NotifyProgress += AFS_NotifyProgress;

                for (int f = 0; f < rdxFiles.Length; f++)
                {
                    afs.AddEntryFromFile(rdxFiles[f]);
                }

                afs.SaveToFile(outputAfsFile);
                afs.NotifyProgress -= AFS_NotifyProgress;
            }

            Directory.Delete(inputDirectory, true);
        }

        #endregion
    }
}