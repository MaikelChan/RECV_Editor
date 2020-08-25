using csharp_prs;
using RECV_Editor.File_Formats;
using System.IO;
using System.Threading.Tasks;

namespace RECV_Editor
{
    static class RECV
    {
        const string ENG_PATH = "ENG";
        const string ENG_SYSMES1_ALD_PATH = ENG_PATH + "/SYSMES1.ALD";
        const string ENG_RDX_LNK1_AFS_PATH = ENG_PATH + "/RDX_LNK1.AFS";

        public static void ExtractAll(string inputFolder, string outputFolder, Table table)
        {
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
            ALD.Extract(SYSMES1, Path.Combine(outputFolder, ENG_SYSMES1_ALD_PATH), table);

            // Extract AFS files

            string RDX_LNK1 = Path.Combine(inputFolder, ENG_RDX_LNK1_AFS_PATH);

            if (!File.Exists(RDX_LNK1))
            {
                throw new FileNotFoundException($"File \"{RDX_LNK1}\" does not exist!", RDX_LNK1);
            }

            Logger.Append($"Extracting \"{RDX_LNK1}\"...");
            string engRDX_LNK1OutputPath = Path.Combine(outputFolder, ENG_RDX_LNK1_AFS_PATH);
            AFS.ExtractAFS(RDX_LNK1, Path.Combine(outputFolder, engRDX_LNK1OutputPath));

            // Decompress files in RDX_LNK1

            string[] rdxFiles = Directory.GetFiles(engRDX_LNK1OutputPath);

            for (int f = 0; f < rdxFiles.Length; f++)
            {
                byte[] rdxData = File.ReadAllBytes(rdxFiles[f]);
                byte[] rdxUncompressedData = Prs.Decompress(rdxData);
                //File.WriteAllBytes(rdxFiles[f] + ".unc", rdxUncompressedData);

                File.Delete(rdxFiles[f]);

                Logger.Append($"Extracting RDX file \"{rdxFiles[f]}\"...");
                RDX.Results result = RDX.Extract(rdxUncompressedData, rdxFiles[f], table);
                if (result == RDX.Results.NotValidRdxFile) Logger.Append($"\"{RDX_LNK1}\" is not a valid RDX file. Ignoring.", Logger.LogTypes.Warning);
            }

            Logger.Append("Extract all process has finished. ------------------------------------------------------------------");
        }
    }
}