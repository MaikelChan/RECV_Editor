using AFSPacker;
using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RECV_Editor
{
    static class RECV
    {
        public enum Platforms { PS2 }

        readonly static string[] languageCodes = new string[] { "ENG", "FRA", "GER", "SPA" };
        readonly static int[] languageIndices = new int[] { 1, 2, 5, 4 };
        public enum Languages { English, French, German, Spanish }

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

        const int MAX_EXTRACTION_PROGRESS_STEPS = 4;
        const int MAX_INSERTION_PROGRESS_STEPS = 5;

        public static void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, Languages language, IProgress<ProgressInfo> progress)
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

            string languageCode = languageCodes[(int)language];
            int languageIndex = languageIndices[(int)language];
            string SYSMES_ALD_Path = $"{languageCode}/SYSMES{languageIndex}.ALD";
            string RDX_LNK_AFS_Path = $"{languageCode}/RDX_LNK{languageIndex}.AFS";

            // Create output <languageCode> folder

            string outputLanguageFolder = Path.Combine(outputFolder, languageCode);
            if (!Directory.Exists(outputLanguageFolder))
            {
                Logger.Append($"Creating \"{outputLanguageFolder}\" folder...");
                Directory.CreateDirectory(outputLanguageFolder);
            }

            // Extract SYSMES1.ALD

            string SYSMES = Path.Combine(inputFolder, SYSMES_ALD_Path);

            if (!File.Exists(SYSMES))
            {
                throw new FileNotFoundException($"File \"{SYSMES}\" does not exist!", SYSMES);
            }

            Logger.Append($"Extracting \"{SYSMES}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{SYSMES_ALD_Path}\"...", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

            ALD.Extract(SYSMES, Path.ChangeExtension(Path.Combine(outputFolder, SYSMES_ALD_Path), null), table);

            // Extract AFS files

            string RDX_LNK = Path.Combine(inputFolder, RDX_LNK_AFS_Path);

            if (!File.Exists(RDX_LNK))
            {
                throw new FileNotFoundException($"File \"{RDX_LNK}\" does not exist!", RDX_LNK);
            }

            Logger.Append($"Extracting \"{RDX_LNK}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{RDX_LNK_AFS_Path}\"...", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

            string RDX_LNK_OutputPath = Path.ChangeExtension(Path.Combine(outputFolder, RDX_LNK_AFS_Path), null);
            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(RDX_LNK, RDX_LNK_OutputPath);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Decompress files in RDX_LNK1

            string[] rdxFiles = Directory.GetFiles(RDX_LNK_OutputPath);

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
                progress?.Report(new ProgressInfo($"Extracting RDX files... ({currentRdxFile++}/{rdxFiles.Length})", currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

                byte[] rdxData = File.ReadAllBytes(rdxFiles[f]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                //File.WriteAllBytes(rdxFiles[f] + ".unc", rdxUncompressedData);

                File.Delete(rdxFiles[f]);

                RDX.Results result = RDX.Extract(rdxUncompressedData, rdxFiles[f], table);
                if (result == RDX.Results.NotValidRdxFile) Logger.Append($"\"{rdxFiles[f]}\" is not a valid RDX file. Ignoring.", Logger.LogTypes.Warning);
#if MULTITHREADING
            });
#else
            }
#endif

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.");
        }

        public static void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, Languages language, IProgress<ProgressInfo> progress)
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

            Table table = GetTableFromLanguage(tablesFolder, language);

            // Begin process

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int currentProgressValue = 0;

            Logger.Append("Insert all process has begun. ----------------------------------------------------------------------");

            // Generate some paths based on selected language

            string languageCode = languageCodes[(int)language];
            int languageIndex = languageIndices[(int)language];
            string outputLanguageFolder = Path.Combine(outputFolder, languageCode);
            string SYSMES_ALD_Path = $"{languageCode}/SYSMES{languageIndex}.ALD";
            string RDX_LNK_AFS_Path = $"{languageCode}/RDX_LNK{languageIndex}.AFS";

            string original_RDX_LNK = Path.Combine(originalDataFolder, RDX_LNK_AFS_Path);
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

            ALD.Insert(Path.ChangeExtension(Path.Combine(inputFolder, SYSMES_ALD_Path), null), SYSMES, table);

            // Extract original RDX_LNK1 file

            if (!File.Exists(original_RDX_LNK))
            {
                throw new FileNotFoundException($"File \"{original_RDX_LNK}\" does not exist!", original_RDX_LNK);
            }

            Logger.Append($"Extracting \"{original_RDX_LNK}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{RDX_LNK_AFS_Path}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(original_RDX_LNK, output_RDX_LNK_folder);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Generate RDX files

            if (!Directory.Exists(input_RDX_LNK_folder))
            {
                throw new DirectoryNotFoundException($"Directory \"{input_RDX_LNK_folder}\" does not exist!");
            }

            string[] inputRdxPaths = Directory.GetDirectories(input_RDX_LNK_folder);
            string[] outputRdxFiles = Directory.GetFiles(output_RDX_LNK_folder);

            if (inputRdxPaths.Length != outputRdxFiles.Length)
            {
                throw new InvalidDataException($"There should be {outputRdxFiles.Length} RDX folder, but found {inputRdxPaths.Length} instead in \"{input_RDX_LNK_folder}\"");
            }

            int currentRdxFile = 1;
            currentProgressValue++;

#if MULTITHREADING
            Parallel.For(0, inputRdxPaths.Length, (r) =>
            {
#else
            for (int r = 0; r < inputRdxPaths.Length; r++)
            {
#endif
                Logger.Append($"Inserting RDX file \"{inputRdxPaths[r]}\"...");
                progress?.Report(new ProgressInfo($"Inserting RDX files... ({currentRdxFile++}/{inputRdxPaths.Length})", currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

                byte[] rdxData = File.ReadAllBytes(outputRdxFiles[r]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                //File.WriteAllBytes(outputRdxFiles[r], rdxUncompressedData);

                using (MemoryStream ms = new MemoryStream(rdxUncompressedData))
                {
                    RDX.Insert(inputRdxPaths[r], ms, table);
                }

                rdxData = PRS.Compress(rdxUncompressedData);
                File.WriteAllBytes(outputRdxFiles[r], rdxData);
#if MULTITHREADING
            });
#else
            }
#endif

            // Insert RDX files into new RDX_LNK1 file

            Logger.Append($"Generating \"{output_RDX_LNK}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{output_RDX_LNK}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.CreateAFS(output_RDX_LNK_folder, output_RDX_LNK);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            Directory.Delete(output_RDX_LNK_folder, true);

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));
            Logger.Append("Insert all process has finished. -------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.");
        }

        static void AFS_NotifyProgress(AFS.NotificationTypes type, string message)
        {
            Logger.Append(message, (Logger.LogTypes)type);
        }

        public static Table GetTableFromLanguage(string tablesFolder, Languages language)
        {
            string tableFile = $"{Path.Combine(tablesFolder, languageCodes[(int)language])}.tbl";

            if (!File.Exists(tableFile))
            {
                throw new FileNotFoundException($"Table file \"{tableFile}\" has not been found.");
            }

            return new Table(tableFile);
        }
    }
}