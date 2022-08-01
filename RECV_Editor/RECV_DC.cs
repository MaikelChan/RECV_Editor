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

        protected override int MaxExtractionProgressSteps => 11; // TODO
        protected override int MaxInsertionProgressSteps => 15;

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

            // Extract SYSEFF

            //{
            //    string syseffFileName = $"syseff{languageIndexString}.ald";
            //    string syseffOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, languageCode, syseffFileName), null);
            //    ExtractSyseff(discInputFolder, syseffFileName, syseffOutputFolder, table, progress, ref currentProgress, MaxExtractionProgressSteps);
            //}

            // Extract MRY

            {
                string mryFileName = "MRY.AFS";
                string mryOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, mryFileName), null);
                ExtractAfs(Path.Combine(discInputFolder, mryFileName), mryOutputFolder, false, disc, progress, ref currentProgress, MaxExtractionProgressSteps);

                PVR.MassExtract(mryOutputFolder, mryOutputFolder, true);
            }

            // Extract AFS files

            string rdxLnkOutputFolder;

            {
                string rdxLnkFileName = $"rdx_lnk{disc}.afs";
                rdxLnkOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, rdxLnkFileName), null);
                ExtractAfs(Path.Combine(discInputFolder, rdxLnkFileName), rdxLnkOutputFolder, false, disc, progress, ref currentProgress, MaxExtractionProgressSteps);
            }

            // Decompress files in RDX_LNK1

            string[] rdxFiles = Directory.GetFiles(rdxLnkOutputFolder);
            ExtractRdxFiles(rdxFiles, language, disc, table, progress, ref currentProgress);
        }

        protected override void InsertDisc(string discInputFolder, string discOutputFolder, string discOriginalDataFolder, Table table, int language, int disc, IProgress<ProgressInfo> progress, ref int currentProgress)
        {
            discOriginalDataFolder = Path.Combine(discOriginalDataFolder, "files");

            if (!Directory.Exists(discOriginalDataFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{discOriginalDataFolder}\" does not exist!");
            }

            // Generate some paths based on selected language

            string languageCode = languageCodes[language];
            int languageIndex = languageIndices[language];
            string RDX_LNK_AFS_Path = $"rdx_lnk{disc}.afs";

            string input_RDX_LNK = Path.Combine(discInputFolder, RDX_LNK_AFS_Path);
            string input_RDX_LNK_folder = Path.ChangeExtension(input_RDX_LNK, null);
            string output_RDX_LNK = Path.Combine(discOutputFolder, RDX_LNK_AFS_Path);
            string output_RDX_LNK_folder = Path.ChangeExtension(output_RDX_LNK, null);

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
                string sysmesFileName = $"sysmes{languageIndex}.ald";
                string sysmesFilePath = Path.Combine(discOutputFolder, sysmesFileName);
                string sysmesDataPath = Path.ChangeExtension(Path.Combine(discInputFolder, languageCode, sysmesFileName), null);

                Logger.Append($"Generating \"{sysmesFilePath}\"...");
                progress?.Report(new ProgressInfo($"Generating \"{sysmesFileName}\"...", ++currentProgress, MaxInsertionProgressSteps));

                InsertSysmes(sysmesDataPath, sysmesFilePath, table);
            }

            // Generate SYSEFF

            {
                string syseffFileName = $"syseff{languageIndex}.ald";
                string syseffFilePath = Path.Combine(discOutputFolder, syseffFileName);
                string syseffDataPath = Path.ChangeExtension(Path.Combine(discInputFolder, languageCode, syseffFileName), null);

                Logger.Append($"Generating \"{syseffFilePath}\"...");
                progress?.Report(new ProgressInfo($"Generating \"{syseffFileName}\"...", ++currentProgress, MaxInsertionProgressSteps));

                InsertSyseff(syseffDataPath, syseffFilePath, table);
            }

            // Generate MRY

            {
                string mryFileName = $"mry.afs";
                string mryInputFolder = Path.ChangeExtension(Path.Combine(discInputFolder, mryFileName), null);
                string mryOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, mryFileName), null);
                ExtractAfs(Path.Combine(discOriginalDataFolder, mryFileName), mryOutputFolder, true, disc, progress, ref currentProgress, MaxInsertionProgressSteps);

                GVR.MassInsert(mryInputFolder, mryOutputFolder);
                GenerateAfs(mryOutputFolder, Path.Combine(discOutputFolder, mryFileName), true, progress, ref currentProgress);
            }

            // Extract original RDX_LNK1 file

            ExtractAfs(Path.Combine(discOriginalDataFolder, RDX_LNK_AFS_Path), output_RDX_LNK_folder, false, disc, progress, ref currentProgress, MaxInsertionProgressSteps);
            string[] outputRdxFiles = Directory.GetFiles(output_RDX_LNK_folder);

            // Generate RDX files

            InsertRdxFiles(input_RDX_LNK_folder, outputRdxFiles, language, disc, table, Platform, progress, ref currentProgress, MaxInsertionProgressSteps);

            // Insert RDX files into new RDX_LNK1 file

            GenerateAfs(output_RDX_LNK_folder, output_RDX_LNK, false, progress, ref currentProgress);
        }
    }
}
