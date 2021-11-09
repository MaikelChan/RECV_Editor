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

        protected abstract void ExtractDisc(string discInputFolder, string discOutputFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress);
        protected abstract void InsertDisc(string discInputFolder, string discOutputFolder, string discOriginalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress);

        public void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            if (string.IsNullOrEmpty(tablesFolder))
            {
                throw new ArgumentNullException(nameof(tablesFolder));
            }

            Table table = GetTableFromLanguage(tablesFolder, language);

            // Begin process

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentProgressValue = 0;

            Logger.Append("Extract all process has begun. ---------------------------------------------------------------------");

            for (int disc = 1; disc < DiscCount + 1; disc++)
            {
                string discFolderName = DiscCount > 1 ? $"Disc {disc}" : string.Empty;
                string discInputFolder = Path.Combine(inputFolder, Platform.ToString(), discFolderName);

                if (!Directory.Exists(discInputFolder))
                {
                    throw new DirectoryNotFoundException($"Directory \"{discInputFolder}\" does not exist!");
                }

                string discOutputFolder = Path.Combine(outputFolder, Platform.ToString(), discFolderName);

                ExtractDisc(discInputFolder, discOutputFolder, table, language, disc, progress, ref currentProgressValue);
            }

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MaxExtractionProgressSteps));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            Logger.Append($"Extracting \"{sysmesPath}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{sysmesFileName}\"...", ++currentProgressValue, maxProgressSteps));

            ALD.Extract(sysmesPath, IsBigEndian, (blockStream, blockIndex, blockSize) =>
            {
                string texts = Texts.Extract(blockStream, table, IsBigEndian);

                // Write all the texts in the block to a txt file
                string outputFile = Path.Combine(outputFolder, $"{blockIndex:00}.txt");
                File.WriteAllText(outputFile, texts);
            });
        }

        protected void ExtractSyseff(string syseffFolder, string syseffFileName, string outputFolder, Table table, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            string syseffPath = Path.Combine(syseffFolder, syseffFileName);

            if (!File.Exists(syseffPath))
            {
                throw new FileNotFoundException($"File \"{syseffPath}\" does not exist!", syseffPath);
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            Logger.Append($"Extracting \"{syseffPath}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{syseffFileName}\"...", ++currentProgressValue, maxProgressSteps));

            ALD.Extract(syseffPath, IsBigEndian, (blockStream, blockIndex, blockSize) =>
            {
                bool isGvr = GVR.IsValid(blockStream);

                string outputFile = Path.Combine(outputFolder, $"{blockIndex:00}.bin");

                using (FileStream fs = File.Create(outputFile))
                {
                    blockStream.CopySliceTo(fs, (int)blockSize);
                }

                if (isGvr)
                {
                    using (FileStream gvrStream = File.OpenRead(outputFile))
                    {
                        string gvrOutputFolder = Path.ChangeExtension(outputFile, null);
                        GVR.Extract(gvrStream, gvrOutputFolder);
                    }
                }
            });
        }

        protected void ExtractAfs(string inputAfsFile, string outputFolder, int disc, IProgress<ProgressInfo> progress, ref int currentProgressValue, int maxProgressSteps)
        {
            Logger.Append($"Extracting \"{inputAfsFile}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{Path.GetFileName(inputAfsFile)}\" (Disc {disc})...", ++currentProgressValue, maxProgressSteps));

            using (AFS afs = new AFS(inputAfsFile))
            {
                afs.NotifyProgress += AFS_NotifyProgress;
                afs.ExtractAllEntriesToDirectory(outputFolder);
                afs.NotifyProgress -= AFS_NotifyProgress;
            }
        }

        protected void ExtractRdxFiles(string[] rdxFiles, int language, int disc, Table table, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            RDX rdx = RDX.GetRDX(Platform);

            currentProgress++;
            int currentRdxFile = 1;

            // Needed for Parallel.For, which can't have ref values.
            int progressValue = currentProgress;

#if MULTITHREADING
            Parallel.For(0, rdxFiles.Length, (f) =>
            {
#else
            for (int f = 0; f < rdxFiles.Length; f++)
            {
#endif
                Logger.Append($"Extracting RDX file \"{rdxFiles[f]}\"...");
                progress?.Report(new ProgressInfo($"Extracting RDX files (Disc {disc})... ({currentRdxFile++}/{rdxFiles.Length})", progressValue, MaxExtractionProgressSteps));

                byte[] rdxData = File.ReadAllBytes(rdxFiles[f]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                //File.WriteAllBytes(rdxFiles[f] + ".unc", rdxUncompressedData);

                File.Delete(rdxFiles[f]);

                RDX.Results result = rdx.Extract(rdxUncompressedData, Path.GetFileName(rdxFiles[f]), rdxFiles[f] + RDX_EXTRACTED_FOLDER_SUFFIX, language, table);
                if (result == RDX.Results.NotValidRdxFile) Logger.Append($"\"{rdxFiles[f]}\" is not a valid RDX file. Ignoring.", Logger.LogTypes.Warning);
#if MULTITHREADING
            });
#else
            }
#endif
        }

        #endregion

        #region Insertion Methods

        protected void InsertSysmes(string inputFolder, string outputFilePath, Table table)
        {
            string[] textFilesNames = Directory.GetFiles(inputFolder, "*.txt");

            ALD.Insert(outputFilePath, IsBigEndian, (uint)textFilesNames.Length, (blockIndex) =>
            {
                string text = File.ReadAllText(textFilesNames[blockIndex]);

                using (MemoryStream ms = new MemoryStream())
                {
                    Texts.Insert(text, ms, table, IsBigEndian);
                    return (ms.ToArray(), false);
                }
            });
        }

        protected void InsertSyseff(string inputFolder, string outputFilePath, Table table)
        {
            string[] fileNames = Directory.GetFiles(inputFolder, "*.bin");

            ALD.Insert(outputFilePath, IsBigEndian, (uint)fileNames.Length, (blockIndex) =>
            {
                string extractedDataPath = Path.ChangeExtension(fileNames[blockIndex], null);
                bool isGvr = Directory.Exists(extractedDataPath);

                if (isGvr)
                {
                    using (FileStream gvrStream = File.Create(fileNames[blockIndex]))
                    {
                        GVR.Insert(extractedDataPath, gvrStream);
                    }
                }

                return (File.ReadAllBytes(fileNames[blockIndex]), isGvr);
            });
        }

        protected void InsertRdxFiles(string inputRdxLnkFolder, string[] outputRdxFiles, int language, int disc, Table table, Platforms platform, IProgress<ProgressInfo> progress, ref int currentProgress, int maxProgressSteps)
        {
            if (!Directory.Exists(inputRdxLnkFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputRdxLnkFolder}\" does not exist!");
            }

            RDX rdx = RDX.GetRDX(platform);

            int currentRdxFile = 1;
            currentProgress++;

            // Needed for Parallel.For, which can't have ref values.
            int progressValue = currentProgress;

#if MULTITHREADING
            Parallel.For(0, outputRdxFiles.Length, (r) =>
#else
            for (int r = 0; r < outputRdxFiles.Length; r++)
#endif
            {
                string inputRdxPath = Path.Combine(inputRdxLnkFolder, Path.GetFileName(outputRdxFiles[r]) + RDX_EXTRACTED_FOLDER_SUFFIX);

                progress?.Report(new ProgressInfo($"Inserting RDX files (Disc {disc})... ({currentRdxFile++}/{outputRdxFiles.Length})", progressValue, maxProgressSteps));

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
            progress?.Report(new ProgressInfo($"Generating \"{Path.GetFileName(outputAfsFile)}\"...", ++currentProgress, MaxInsertionProgressSteps));

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