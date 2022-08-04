using RECV_Editor.File_Formats;
using System;
using System.IO;

namespace RECV_Editor
{
    class RECV_DC : RECV
    {
        readonly static string[] languageNames = new string[] { SPA_LANGUAGE_NAME };
        public override string[] LanguageNames => languageNames;

        readonly static string[] languageCodes = new string[] { SPA_LANGUAGE_CODE };
        protected override string[] LanguageCodes => languageCodes;

        readonly static int[] languageIndices = new int[] { 0 };
        protected override int[] LanguageIndices => languageIndices;

        protected override Platforms Platform => Platforms.Dreamcast;
        protected override int DiscCount => 2;
        protected override bool IsBigEndian => false;

        protected override int MaxExtractionProgressSteps => 9;
        protected override int MaxInsertionProgressSteps => 13;

        protected override void ExtractDisc(string discInputFolder, string discOutputFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            if (!Directory.Exists(discInputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{discInputFolder}\" does not exist!");
            }

            // Create output folder

            if (!Directory.Exists(discOutputFolder))
            {
                Logger.Append($"Creating \"{discOutputFolder}\" folder...");
                Directory.CreateDirectory(discOutputFolder);
            }

            // Extract SYSMES

            {
                string sysmesFileName = "SYSMES.ALD";
                string sysmesOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, sysmesFileName), null);
                ExtractSysmes(discInputFolder, sysmesFileName, sysmesOutputFolder, table, progress, ref currentProgress, MaxExtractionProgressSteps);
            }

            // Extract ADV.AFS

            {
                string advFileName = "ADV.AFS";
                string advOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, advFileName), null);
                ExtractAdv(discInputFolder, advFileName, advOutputFolder, table, disc, progress, ref currentProgress, MaxExtractionProgressSteps);
            }

            // Extract MRY

            {
                string mryFileName = "MRY.AFS";
                string mryOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, mryFileName), null);
                ExtractAfs(Path.Combine(discInputFolder, mryFileName), mryOutputFolder, false, disc, progress, ref currentProgress, MaxExtractionProgressSteps);

                PVR.MassExtract(mryOutputFolder, mryOutputFolder, true);
            }

            // Decompress RDX files

            {
                string[] rdxFiles = Directory.GetFiles(discInputFolder, "*.RDX");
                ExtractRdxFiles(rdxFiles, Path.Combine(discOutputFolder, "RDX"), language, disc, table, false, progress, ref currentProgress);
            }
        }

        protected override void InsertDisc(string discInputFolder, string discOutputFolder, string discOriginalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            if (!Directory.Exists(discOriginalDataFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{discOriginalDataFolder}\" does not exist!");
            }

            // Delete existing output folder if it exists

            if (Directory.Exists(discOutputFolder))
            {
                Logger.Append($"Deleting \"{discOutputFolder}\" folder...");
                Directory.Delete(discOutputFolder, true);
            }

            // Create output folder

            if (!Directory.Exists(discOutputFolder))
            {
                Logger.Append($"Creating \"{discOutputFolder}\" folder...");
                Directory.CreateDirectory(discOutputFolder);
            }

            // Generate SYSMES

            {
                string sysmesFileName = "SYSMES.ALD";
                string sysmesDataPath = Path.ChangeExtension(Path.Combine(discInputFolder, sysmesFileName), null);
                string sysmesFilePath = Path.Combine(discOutputFolder, sysmesFileName);

                Logger.Append($"Generating \"{sysmesFilePath}\"...");
                progress?.Report(new ProgressInfo($"Generating \"{sysmesFileName}\"...", ++currentProgress, MaxInsertionProgressSteps));

                InsertSysmes(sysmesDataPath, sysmesFilePath, table);
            }

            // Generate ADV.AFS

            {
                string advFileName = "ADV.AFS";
                string originalAdvFile = Path.Combine(discOriginalDataFolder, advFileName);
                string advInputFolder = Path.ChangeExtension(Path.Combine(discInputFolder, advFileName), null);
                string tempFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, advFileName), null);
                string outputAdvFile = Path.Combine(discOutputFolder, advFileName);
                InsertAdv(originalAdvFile, advInputFolder, outputAdvFile, tempFolder, table, disc, progress, ref currentProgress, MaxInsertionProgressSteps);
            }

            // Generate MRY

            {
                string mryFileName = "MRY.AFS";
                string mryInputFolder = Path.ChangeExtension(Path.Combine(discInputFolder, mryFileName), null);
                string mryOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, mryFileName), null);
                ExtractAfs(Path.Combine(discOriginalDataFolder, mryFileName), mryOutputFolder, true, disc, progress, ref currentProgress, MaxInsertionProgressSteps);

                PVR.MassInsert(mryInputFolder, mryOutputFolder);
                GenerateAfs(mryOutputFolder, Path.Combine(discOutputFolder, mryFileName), true, progress, ref currentProgress);
            }

            // Copy original RDX files to output folder

            {
                string[] rdxFiles = Directory.GetFiles(discOriginalDataFolder, "*.RDX");

                for (int f = 0; f < rdxFiles.Length; f++)
                {
                    string newFile = Path.Combine(discOutputFolder, Path.GetFileName(rdxFiles[f]));
                    File.Copy(rdxFiles[f], newFile);
                    rdxFiles[f] = newFile;
                }

                // Generate RDX files

                InsertRdxFiles(Path.Combine(discInputFolder, "RDX"), rdxFiles, language, disc, table, Platform, progress, ref currentProgress, MaxInsertionProgressSteps);
            }
        }

        public static string GetLanguageCode(int language)
        {
            return languageCodes[language];
        }
    }
}