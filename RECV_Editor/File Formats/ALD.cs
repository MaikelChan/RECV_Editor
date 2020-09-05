using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class ALD
    {
        const ushort FILE_END_PADDING = 0xFFFF;

        public static void Extract(string inputFile, string outputFolder, Table table)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            using (FileStream fs = File.OpenRead(inputFile))
            {
                Extract(fs, outputFolder, table);
            }
        }

        public static void Extract(Stream aldStream, string outputFolder, Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (BinaryReader br = new BinaryReader(aldStream, Encoding.UTF8, true))
            {
                uint currentBlock = 0;

                // Read each block
                for (; ; )
                {
                    uint blockSize = br.ReadUInt32();

                    // Obtain all texts in the block
                    using (SubStream blockStream = new SubStream(aldStream, 0, blockSize, true))
                    {
                        string texts = Texts.Extract(blockStream, table);

                        // Write all the texts in the block to a txt file
                        string outputFile = Path.Combine(outputFolder, $"{currentBlock: 00}.txt");
                        File.WriteAllText(outputFile, texts);
                    }

                    // Check if there are other blocks after this one
                    if (aldStream.Position >= aldStream.Length) break;
                    ushort value = br.ReadUInt16();
                    if (value == FILE_END_PADDING) break;

                    aldStream.Position -= 2;
                    currentBlock++;
                }
            }
        }
    }
}