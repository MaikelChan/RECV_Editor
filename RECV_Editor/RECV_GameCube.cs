using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RECV_Editor
{
    class RECV_GameCube : RECV
    {
        readonly static string[] languageNames = new string[] { "Japanese", "English (USA)", "English", "German", "French", "Spanish", "Italian" };
        public override string[] LanguageNames => languageNames;

        readonly static string[] languageCodes = new string[] { "JPN", "USA", "ENG", "GER", "FRA", "SPA", "ITA" };
        protected override string[] LanguageCodes => languageCodes;

        readonly static int[] languageIndices = new int[] { 0, 1, 2, 3, 4, 5, 6 };
        protected override int[] LanguageIndices => languageIndices;

        protected override int DiscCount => 2;
        protected override bool IsBigEndian => true;

        // TODO: Move to RECV?
        const int MAX_EXTRACTION_PROGRESS_STEPS = 4;
        const int MAX_INSERTION_PROGRESS_STEPS = 5;

        public override void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int language, IProgress<ProgressInfo> progress)
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
                string discInputFolder = Path.Combine(inputFolder, Platforms.GameCube.ToString(), $"Disc {disc}", "files");

                if (!Directory.Exists(discInputFolder))
                {
                    throw new DirectoryNotFoundException($"Directory \"{discInputFolder}\" does not exist!");
                }

                string discOutputFolder = Path.Combine(outputFolder, Platforms.GameCube.ToString(), $"Disc {disc}");

                // Generate some paths based on selected language and current disc

                string languageCode = languageCodes[language];
                int languageIndex = languageIndices[language];
                string languageIndexString = languageIndex == 0 ? string.Empty : languageIndex.ToString();

                // Create output <languageCode> folder

                string outputLanguageFolder = Path.Combine(discOutputFolder, languageCode);
                if (!Directory.Exists(outputLanguageFolder))
                {
                    Logger.Append($"Creating \"{outputLanguageFolder}\" folder...");
                    Directory.CreateDirectory(outputLanguageFolder);
                }

                // Extract SYSMES

                {
                    string sysmesInputFolder = discInputFolder;
                    string sysmesFileName = $"sysmes{languageIndexString}.ald";
                    string sysmesOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, languageCode, sysmesFileName), null);
                    ExtractSysmes(sysmesInputFolder, sysmesFileName, sysmesOutputFolder, table, progress, ref currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount);
                }

                // Extract AFS files

                string rdxLnkOutputFolder;

                {
                    string rdxLnkInputFolder = discInputFolder;
                    string rdxLnkFileName = $"rdx_lnk{disc}.afs";
                    rdxLnkOutputFolder = Path.ChangeExtension(Path.Combine(discOutputFolder, rdxLnkFileName), null);
                    ExtractRdxLnk(rdxLnkInputFolder, rdxLnkFileName, rdxLnkOutputFolder, progress, ref currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount);
                }

                // Decompress files in RDX_LNK1

                string[] rdxFiles = Directory.GetFiles(rdxLnkOutputFolder);

                RDX rdx = RDX.GetRDX(Platforms.GameCube);

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

                    RDX.Results result = rdx.Extract(rdxUncompressedData, rdxFiles[f] + RDX_EXTRACTED_FOLDER_SUFFIX, language, table);
                    if (result == RDX.Results.NotValidRdxFile) Logger.Append($"\"{rdxFiles[f]}\" is not a valid RDX file. Ignoring.", Logger.LogTypes.Warning);
#if MULTITHREADING
                });
#else
                }
#endif
            }

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS * DiscCount));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.");
        }

        public override void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, int languageIndex, IProgress<ProgressInfo> progress)
        {
            throw new NotImplementedException("GameCube data insertion not implemented.");
        }
    }
}