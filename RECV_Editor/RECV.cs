using AFSPacker;
using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RECV_Editor
{
    static class RECV
    {
        const string ENG_PATH = "ENG";
        const string ENG_SYSMES1_ALD_PATH = ENG_PATH + "/SYSMES1.ALD";
        const string ENG_RDX_LNK1_AFS_PATH = ENG_PATH + "/RDX_LNK1.AFS";

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
        const int MAX_INSERTION_PROGRESS_STEPS = 4;

        public static void ExtractAll(string inputFolder, string outputFolder, Table table, IProgress<ProgressInfo> progress)
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

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            // Begin process

            int currentProgressValue = 0;

            Logger.Append("Extract all process has begun. ---------------------------------------------------------------------");

            // Create output ENG folder

            string outputEngFolder = Path.Combine(outputFolder, ENG_PATH);
            if (!Directory.Exists(outputEngFolder))
            {
                Logger.Append($"Creating \"{outputEngFolder}\" folder...");
                Directory.CreateDirectory(outputEngFolder);
            }

            // Extract SYSMES1.ALD

            string SYSMES1 = Path.Combine(inputFolder, ENG_SYSMES1_ALD_PATH);

            if (!File.Exists(SYSMES1))
            {
                throw new FileNotFoundException($"File \"{SYSMES1}\" does not exist!", SYSMES1);
            }

            Logger.Append($"Extracting \"{SYSMES1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_SYSMES1_ALD_PATH}\"...", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

            ALD.Extract(SYSMES1, Path.Combine(outputFolder, ENG_SYSMES1_ALD_PATH), table);

            // Extract AFS files

            string RDX_LNK1 = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!File.Exists(RDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{RDX_LNK1}\" does not exist!", RDX_LNK1);
            }

            Logger.Append($"Extracting \"{RDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

            string engRDX_LNK1OutputPath = Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH);
            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(RDX_LNK1, engRDX_LNK1OutputPath);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Decompress files in RDX_LNK1

            string[] rdxFiles = Directory.GetFiles(engRDX_LNK1OutputPath);

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
        }

        public static void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, Table table, IProgress<ProgressInfo> progress)
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

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            // Begin process

            int currentProgressValue = 0;

            Logger.Append("Insert all process has begun. ----------------------------------------------------------------------");

            // Delete existing output folder if it exists

            if (Directory.Exists(outputFolder))
            {
                Logger.Append($"Deleting \"{outputFolder}\" folder...");
                Directory.Delete(outputFolder, true);
            }

            // Create output folder

            string outputEngFolder = Path.Combine(outputFolder, ENG_PATH);
            if (!Directory.Exists(outputEngFolder))
            {
                Logger.Append($"Creating \"{outputEngFolder}\" folder...");
                Directory.CreateDirectory(outputEngFolder);
            }

            // Generate SYSMES1.ALD

            string SYSMES1 = Path.Combine(outputFolder, ENG_SYSMES1_ALD_PATH);

            Logger.Append($"Generating \"{SYSMES1}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{ENG_SYSMES1_ALD_PATH}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            ALD.Insert(Path.Combine(inputFolder, ENG_SYSMES1_ALD_PATH), SYSMES1, table);

            // Extract original AFS files

            string originalRDX_LNK1 = Path.Combine(originalDataFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!File.Exists(originalRDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{originalRDX_LNK1}\" does not exist!", originalRDX_LNK1);
            }

            Logger.Append($"Extracting \"{originalRDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            string original_RDX_LNK1_folder = Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH);
            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(originalRDX_LNK1, original_RDX_LNK1_folder);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Generate RDX files

            string RDX_LNK1_folder = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!Directory.Exists(RDX_LNK1_folder))
            {
                throw new DirectoryNotFoundException($"Directory \"{RDX_LNK1_folder}\" does not exist!");
            }

            string[] rdxPaths = Directory.GetDirectories(RDX_LNK1_folder);
            string[] originalRdxFiles = Directory.GetFiles(original_RDX_LNK1_folder);

            if (rdxPaths.Length != originalRdxFiles.Length)
            {
                throw new InvalidDataException($"There should be {originalRdxFiles.Length} RDX folder, but found {rdxPaths.Length} instead in \"{RDX_LNK1_folder}\"");
            }

            for (int r = 0; r < rdxPaths.Length; r++)
            {
                byte[] rdxData = File.ReadAllBytes(originalRdxFiles[r]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                File.WriteAllBytes(originalRdxFiles[r], rdxUncompressedData);

                RDX.Insert(rdxPaths[r], originalRdxFiles[r], table);
            }

            Logger.Append($"Extracting \"{RDX_LNK1_folder}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            // Finish process

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));
            Logger.Append("Insert all process has finished. -------------------------------------------------------------------");

            GC.Collect();
        }

        static void AFS_NotifyProgress(AFS.NotificationTypes type, string message)
        {
            Logger.Append(message, (Logger.LogTypes)type);
        }
    }
}