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
        const int MAX_INSERTION_PROGRESS_STEPS = 5;

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

            Stopwatch sw = new Stopwatch();
            sw.Start();

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

            ALD.Extract(SYSMES1, Path.ChangeExtension(Path.Combine(outputFolder, ENG_SYSMES1_ALD_PATH), null), table);

            // Extract AFS files

            string RDX_LNK1 = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!File.Exists(RDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{RDX_LNK1}\" does not exist!", RDX_LNK1);
            }

            Logger.Append($"Extracting \"{RDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_EXTRACTION_PROGRESS_STEPS));

            string engRDX_LNK1OutputPath = Path.ChangeExtension(Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH), null);
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

            sw.Stop();
            MessageBox.Show($"The process has finished successfully in {sw.Elapsed.TotalSeconds} seconds.");
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

            string original_RDX_LNK1 = Path.Combine(originalDataFolder, ENG_RDX_LNK1_AFS_PATH);
            string input_RDX_LNK1 = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);
            string input_RDX_LNK1_folder = Path.ChangeExtension(input_RDX_LNK1, null);
            string output_RDX_LNK1 = Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH);
            string output_RDX_LNK1_folder = Path.ChangeExtension(output_RDX_LNK1, null);

            // Begin process

            Stopwatch sw = new Stopwatch();
            sw.Start();

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

            ALD.Insert(Path.ChangeExtension(Path.Combine(inputFolder, ENG_SYSMES1_ALD_PATH), null), SYSMES1, table);

            // Extract original RDX_LNK1 file

            if (!File.Exists(original_RDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{original_RDX_LNK1}\" does not exist!", original_RDX_LNK1);
            }

            Logger.Append($"Extracting \"{original_RDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(original_RDX_LNK1, output_RDX_LNK1_folder);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Generate RDX files

            if (!Directory.Exists(input_RDX_LNK1_folder))
            {
                throw new DirectoryNotFoundException($"Directory \"{input_RDX_LNK1_folder}\" does not exist!");
            }

            string[] inputRdxPaths = Directory.GetDirectories(input_RDX_LNK1_folder);
            string[] outputRdxFiles = Directory.GetFiles(output_RDX_LNK1_folder);

            if (inputRdxPaths.Length != outputRdxFiles.Length)
            {
                throw new InvalidDataException($"There should be {outputRdxFiles.Length} RDX folder, but found {inputRdxPaths.Length} instead in \"{input_RDX_LNK1_folder}\"");
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

            Logger.Append($"Generating \"{output_RDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Generating \"{output_RDX_LNK1}\"...", ++currentProgressValue, MAX_INSERTION_PROGRESS_STEPS));

            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.CreateAFS(output_RDX_LNK1_folder, output_RDX_LNK1);
            AFS.NotifyProgress -= AFS_NotifyProgress;

            Directory.Delete(output_RDX_LNK1_folder, true);

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
    }
}