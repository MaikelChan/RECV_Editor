using RECV_Editor.File_Formats;
using System;
using System.IO;

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

        protected override void ExtractDisc(string discInputFolder, string discOutputFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];

            // Create output <languageCode> folder

            string outputLanguageFolder = Path.Combine(discOutputFolder, languageCode);
            if (!Directory.Exists(outputLanguageFolder))
            {
                Logger.Append($"Creating \"{outputLanguageFolder}\" folder...");
                Directory.CreateDirectory(outputLanguageFolder);
            }

            // Extract SYSMES

            {
                string sysmesInputFolder = Path.Combine(discInputFolder, languageCode);
                string sysmesFileName = $"SYSMES{languageIndex}.ALD";
                string sysmesOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, languageCode, sysmesFileName), null);
                ExtractSysmes(sysmesInputFolder, sysmesFileName, sysmesOutputFolder, table, progress, ref currentProgress, MaxExtractionProgressSteps * DiscCount);
            }

            // Extract AFS files

            string rdxLnkOutputFolder;

            {
                string rdxLnkInputFolder = Path.Combine(discInputFolder, languageCode);
                string rdxLnkFileName = $"RDX_LNK{languageIndex}.AFS";
                rdxLnkOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, languageCode, rdxLnkFileName), null);
                ExtractAfs(Path.Combine(rdxLnkInputFolder, rdxLnkFileName), rdxLnkOutputFolder, 1, progress, ref currentProgress, MaxExtractionProgressSteps * DiscCount);
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

            ExtractRdxFiles(rdxFiles, language, disc, table, progress, ref currentProgress);
        }

        protected override void InsertDisc(string discInputFolder, string discOutputFolder, string discOriginalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];
            string outputLanguageFolder = Path.Combine(discOutputFolder, languageCode);
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

            {
                string sysmesFileName = $"{languageCode}/SYSMES{languageIndex}.ALD";
                string sysmesFilePath = Path.Combine(discOutputFolder, sysmesFileName);
                string sysmesDataPath = Path.ChangeExtension(Path.Combine(discInputFolder, sysmesFileName), null);

                Logger.Append($"Generating \"{sysmesFilePath}\"...");
                progress?.Report(new ProgressInfo($"Generating \"{sysmesFileName}\"...", ++currentProgress, MaxInsertionProgressSteps));

                InsertSysmes(sysmesDataPath, sysmesFilePath, table);
            }

            // Extract original RDX_LNK1 file

            ExtractAfs(Path.Combine(discOriginalDataFolder, RDX_LNK_AFS_Path), output_RDX_LNK_folder, disc, progress, ref currentProgress, MaxInsertionProgressSteps);

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