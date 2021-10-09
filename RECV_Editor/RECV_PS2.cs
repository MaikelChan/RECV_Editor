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

        protected override Platforms Platform => Platforms.PS2;
        protected override int DiscCount => 1;
        protected override bool IsBigEndian => false;

        protected override int MaxExtractionProgressSteps => 4;
        protected override int MaxInsertionProgressSteps => 5;

        public override void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            inputFolder = Path.Combine(inputFolder, Platform.ToString());

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            outputFolder = Path.Combine(outputFolder, Platform.ToString());

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
                ExtractSysmes(sysmesInputFolder, sysmesFileName, sysmesOutputFolder, table, progress, ref currentProgressValue, MaxExtractionProgressSteps * DiscCount);
            }

            // Extract AFS files

            string rdxLnkOutputFolder;

            {
                string rdxLnkInputFolder = Path.Combine(inputFolder, languageCode);
                string rdxLnkFileName = $"RDX_LNK{languageIndex}.AFS";
                rdxLnkOutputFolder = Path.ChangeExtension(Path.Combine(outputFolder, languageCode, rdxLnkFileName), null);
                ExtractRdxLnk(rdxLnkInputFolder, rdxLnkFileName, rdxLnkOutputFolder, 1, progress, ref currentProgressValue, MaxExtractionProgressSteps * DiscCount);
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

            RDX rdx = RDX.GetRDX(Platform);

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
                progress?.Report(new ProgressInfo($"Extracting RDX files... ({currentRdxFile++}/{rdxFiles.Length})", currentProgressValue, MaxExtractionProgressSteps * DiscCount));

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

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MaxExtractionProgressSteps * DiscCount));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void InsertDisc(string discInputFolder, string discOutputFolder, string discOriginalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];
            string outputLanguageFolder = Path.Combine(discOutputFolder, languageCode);
            string SYSMES_ALD_Path = $"{languageCode}/SYSMES{languageIndex}.ALD";
            string RDX_LNK_AFS_Path = $"{languageCode}/RDX_LNK{languageIndex}.AFS";

            string input_RDX_LNK = Path.Combine(discInputFolder, RDX_LNK_AFS_Path);
            string input_RDX_LNK_folder = Path.ChangeExtension(input_RDX_LNK, null);
            string output_RDX_LNK = Path.Combine(discOutputFolder, RDX_LNK_AFS_Path);
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

            string SYSMES = Path.Combine(discOutputFolder, SYSMES_ALD_Path);

            Logger.Append($"Generating \"{SYSMES}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{SYSMES_ALD_Path}\"...", ++currentProgress, MaxInsertionProgressSteps));

            ALD.Insert(Path.ChangeExtension(Path.Combine(discInputFolder, SYSMES_ALD_Path), null), SYSMES, table, IsBigEndian);

            // Extract original RDX_LNK1 file

            ExtractRdxLnk(discOriginalDataFolder, RDX_LNK_AFS_Path, output_RDX_LNK_folder, disc, progress, ref currentProgress, MaxInsertionProgressSteps);

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

            InsertRdxFiles(input_RDX_LNK_folder, outputRdxFiles, language, disc, table, Platform, progress, ref currentProgress, MaxInsertionProgressSteps);

            // Insert RDX files into new RDX_LNK1 file

            GenerateAfs(output_RDX_LNK_folder, output_RDX_LNK, progress, ref currentProgress);
        }

        public static string GetLanguageCode(int language)
        {
            return languageCodes[language];
        }
    }
}