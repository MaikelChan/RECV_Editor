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

        const int MAX_PROGRESS_STEPS = 4;

        public static void ExtractAll(string inputFolder, string outputFolder, Table table, IProgress<ProgressInfo> progress)
        {
            int currentProgressValue = 0;

            Logger.Append("Extract all process has begun. ---------------------------------------------------------------------");

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

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
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_SYSMES1_ALD_PATH}\"...", ++currentProgressValue, MAX_PROGRESS_STEPS));

            ALD.Extract(SYSMES1, Path.Combine(outputFolder, ENG_SYSMES1_ALD_PATH), table);

            // Extract AFS files

            string RDX_LNK1 = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!File.Exists(RDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{RDX_LNK1}\" does not exist!", RDX_LNK1);
            }

            Logger.Append($"Extracting \"{RDX_LNK1}\"...");
            progress?.Report(new ProgressInfo($"Extracting \"{ENG_RDX_LNK1_AFS_PATH}\"...", ++currentProgressValue, MAX_PROGRESS_STEPS));

            string engRDX_LNK1OutputPath = Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH);
            AFS.NotifyProgress += AFS_NotifyProgress;
            AFS.ExtractAFS(RDX_LNK1, Path.Combine(outputFolder, engRDX_LNK1OutputPath));
            AFS.NotifyProgress -= AFS_NotifyProgress;

            // Decompress files in RDX_LNK1

            string[] rdxFiles = Directory.GetFiles(engRDX_LNK1OutputPath);

            currentProgressValue++;
            int currentRdxFile = 1;

            Parallel.For(0, rdxFiles.Length, (f) =>
            {
                Logger.Append($"Extracting RDX file \"{rdxFiles[f]}\"...");
                progress?.Report(new ProgressInfo($"Extracting RDX files... ({currentRdxFile++}/{rdxFiles.Length})", currentProgressValue, MAX_PROGRESS_STEPS));

                byte[] rdxData = File.ReadAllBytes(rdxFiles[f]);
                byte[] rdxUncompressedData = PRS.Decompress(rdxData);
                //File.WriteAllBytes(rdxFiles[f] + ".unc", rdxUncompressedData);

                File.Delete(rdxFiles[f]);

                RDX.Results result = RDX.Extract(rdxUncompressedData, rdxFiles[f], table);
                if (result == RDX.Results.NotValidRdxFile) Logger.Append($"\"{rdxFiles[f]}\" is not a valid RDX file. Ignoring.", Logger.LogTypes.Warning);
            });

            progress?.Report(new ProgressInfo("Done!", ++currentProgressValue, MAX_PROGRESS_STEPS));
            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");

            GC.Collect();
        }

        public static void InsertAll(string inputFolder, string outputFolder, Table table, IProgress<ProgressInfo> progress)
        {

        }

        static void AFS_NotifyProgress(AFS.NotificationTypes type, string message)
        {
            Logger.Append(message, (Logger.LogTypes)type);
        }
    }
}