using AFSLib;
using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.Diagnostics;
using System.IO;
#if MULTITHREADING
using System.Threading.Tasks;
#endif
using System.Windows.Forms;

namespace RECV_Editor
{
    class RECV_PS2 : RECV
    {
        readonly static string[] languageNames = new string[] { ENG_LANGUAGE_NAME, FRA_LANGUAGE_NAME, GER_LANGUAGE_NAME, SPA_LANGUAGE_NAME };
        public override string[] LanguageNames => languageNames;

        readonly static string[] languageCodes = new string[] { ENG_LANGUAGE_CODE, FRA_LANGUAGE_CODE, GER_LANGUAGE_CODE, SPA_LANGUAGE_CODE };
        protected override string[] LanguageCodes => languageCodes;

        readonly static int[] languageIndices = new int[] { 1, 2, 5, 4 };
        protected override int[] LanguageIndices => languageIndices;

        protected override int DiscCount => 1;
        protected override bool IsBigEndian => false;

        //public enum Languages { English, French, German, Spanish }

        const int MAX_EXTRACTION_PROGRESS_STEPS = 4;
        const int MAX_INSERTION_PROGRESS_STEPS = 5;

        public override void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            inputFolder = Path.Combine(inputFolder, Platforms.PS2.ToString());

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            outputFolder = Path.Combine(outputFolder, Platforms.PS2.ToString());

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

            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];

            // Create output <languageCode> folder

            string outputLanguageFolder = Path.Combine(outputFolder, languageCode);
            if (!Directory.Exists(outputLanguageFolder))
            {
                Logger.Append($"Creating \"{outputLanguageFolder}\" folder...");
                Directory.CreateDirectory(outputLanguageFolder);
            }

            // Extract SYSMES

            {
                string sysmesInputFolder = Path.Combine(inputFolder, languageCode);
                string sysmesFileName = $"SYSMES{languageIndex}.ALD";
                string sysmesOutputFolder = Path.ChangeExtension(Path.Combine(outputFolder, languageCode, sysmesFileName), null);
                ExtractSysmes(sysmesInputFolder, sysmesFileName, sysmesOutputFolder, table, progress, ref currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount);
            }

            // Extract AFS files

            string rdxLnkOutputFolder;

            {
                string rdxLnkInputFolder = Path.Combine(inputFolder, languageCode);
                string rdxLnkFileName = $"RDX_LNK{languageIndex}.AFS";
                rdxLnkOutputFolder = Path.ChangeExtension(Path.Combine(outputFolder, languageCode, rdxLnkFileName), null);
                ExtractRdxLnk(rdxLnkInputFolder, rdxLnkFileName, rdxLnkOutputFolder, progress, ref currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount);
            }

            // Rename PS2 RDX files for convenience

            string[] rdxFiles = Directory.GetFiles(rdxLnkOutputFolder);
            string[] rdxNames = Constants.PS2_RDXFileNames[language];

            for (int r = 0; r < rdxFiles.Length; r++)
            {
                string newName = Path.Combine(Path.GetDirectoryName(rdxFiles[r]), rdxNames[r]);
                File.Move(rdxFiles[r], newName);
                rdxFiles[r] = newName;
            }

            // Decompress files in RDX_LNK1

            RDX rdx = RDX.GetRDX(Platforms.PS2);

            currentProgressValue++;
            int currentRdxFile = 1;

#if MULTITHREADING
            Parallel.For(0, rdxFiles.Length, (f) =>
            {
#else
            for (int f = 0; f < rdxFiles.Length; f++)
            {
#endif
                Logger.Append($"Extracting RDX file \"{rdxFiles[f]}\"...");
                progress?.Report(new ProgressInfo($"Extracting RDX files... ({currentRdxFile++}/{rdxFiles.Length})", currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount));

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

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public override void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            inputFolder = Path.Combine(inputFolder, Platforms.PS2.ToString());

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            outputFolder = Path.Combine(outputFolder, Platforms.PS2.ToString());

            if (string.IsNullOrEmpty(originalDataFolder))
            {
                throw new ArgumentNullException(nameof(originalDataFolder));
            }

            originalDataFolder = Path.Combine(originalDataFolder, Platforms.PS2.ToString());

            if (!Directory.Exists(originalDataFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{originalDataFolder}\" does not exist!");
            }

            Table table = GetTableFromLanguage(tablesFolder, language);

            // Begin process

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentProgressValue = 0;

            Logger.Append("Insert all process has begun. ----------------------------------------------------------------------");

            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];
            string outputLanguageFolder = Path.Combine(outputFolder, languageCode);
            string SYSMES_ALD_Path = $"{languageCode}/SYSMES{languageIndex}.ALD";
            string RDX_LNK_AFS_Path = $"{languageCode}/RDX_LNK{languageIndex}.AFS";

            string input_RDX_LNK = Path.Combine(inputFolder, RDX_LNK_AFS_Path);
            string input_RDX_LNK_folder = Path.ChangeExtension(input_RDX_LNK, null);
            string output_RDX_LNK = Path.Combine(outputFolder, RDX_LNK_AFS_Path);
            string output_RDX_LNK_folder = Path.ChangeExtension(output_RDX_LNK, null);

            // Delete existing output folder if it exists

            if (Directory.Exists(outputLanguageFolder))
            {
                Logger.Append($"Deleting \"{outputLanguageFolder}\" folder...");
                Directory.Delete(outputLanguageFolder, true);
            }

            // Create output folder

            if (!Directory.Exists(outputLanguageFolder))
            {
                Logger.Append($"Creating \"{outputLanguageFolder}\" folder...");
                Directory.CreateDirectory(outputLanguageFolder);
            }

            // Generate SYSMES1.ALD

            string SYSMES = Path.Combine(outputFolder, SYSMES_ALD_Path);

            Logger.Append($"Generating \"{SYSMES}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{SYSMES_ALD_Path}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            ALD.Insert(Path.ChangeExtension(Path.Combine(inputFolder, SYSMES_ALD_Path), null), SYSMES, table, IsBigEndian);

            // Extract original RDX_LNK1 file

            ExtractRdxLnk(originalDataFolder, RDX_LNK_AFS_Path, output_RDX_LNK_folder, progress, ref currentProgressValue, MAX_INSERTION_PROGRESS_STEPS);

            // Rename PS2 RDX files for convenience

            string[] outputRdxFiles = Directory.GetFiles(output_RDX_LNK_folder);
            string[] rdxNames = Constants.PS2_RDXFileNames[language];

            for (int r = 0; r < outputRdxFiles.Length; r++)
            {
                string newName = Path.Combine(Path.GetDirectoryName(outputRdxFiles[r]), rdxNames[r]);
                File.Move(outputRdxFiles[r], newName);
                outputRdxFiles[r] = newName;
            }

            // Generate RDX files

            if (!Directory.Exists(input_RDX_LNK_folder))
            {
                throw new DirectoryNotFoundException($"Directory \"{input_RDX_LNK_folder}\" does not exist!");
            }

            RDX rdx = RDX.GetRDX(Platforms.PS2);

            int currentRdxFile = 1;
            currentProgressValue++;

#if MULTITHREADING
            Parallel.For(0, outputRdxFiles.Length, (r) =>
#else
            for (int r = 0; r < outputRdxFiles.Length; r++)
#endif
            {
                string inputRdxPath = Path.Combine(input_RDX_LNK_folder, Path.GetFileName(outputRdxFiles[r]) + RDX_EXTRACTED_FOLDER_SUFFIX);

                progress?.Report(new ProgressInfo($"Inserting RDX files... ({currentRdxFile++}/{outputRdxFiles.Length})", currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

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

            // Insert RDX files into new RDX_LNK1 file

            Logger.Append($"Generating \"{output_RDX_LNK}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{output_RDX_LNK}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            string[] rdxFiles = Directory.GetFiles(output_RDX_LNK_folder);

            using (AFS afs = new AFS())
            {
                afs.AttributesInfoType = AttributesInfoType.NoAttributes;
                afs.NotifyProgress += AFS_NotifyProgress;

                for (int f = 0; f < rdxFiles.Length; f++)
                {
                    afs.AddEntryFromFile(rdxFiles[f]);
                }

                afs.SaveToFile(output_RDX_LNK);
                afs.NotifyProgress -= AFS_NotifyProgress;
            }

            Directory.Delete(output_RDX_LNK_folder, true);

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));
            Logger.Append("Insert all process has finished. -------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static string GetLanguageCode(int language)
        {
            return languageCodes[language];
        }
    }
}