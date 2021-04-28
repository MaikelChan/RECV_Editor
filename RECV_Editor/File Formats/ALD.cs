using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class ALD
    {
        const ushort FILE_END_PADDING = 0xFFFF;

        public static void Extract(string inputFile, string outputFolder, Table table, bool bigEndian)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            using (FileStream fs = File.OpenRead(inputFile))
            {
                Extract(fs, outputFolder, table, bigEndian);
            }
        }

        public static void Extract(Stream aldStream, string outputFolder, Table table, bool bigEndian)
        {
            if (aldStream == null)
            {
                throw new ArgumentNullException(nameof(aldStream));
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

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
                    uint blockSize = br.ReadUInt32Endian(bigEndian);

                    long blockStartPosition = aldStream.Position;
                    long blockEndPosition = blockStartPosition + blockSize;

                    // Obtain all texts in the block
                    using (SubStream blockStream = new SubStream(aldStream, 0, blockSize, true))
                    {
                        string texts = Texts.Extract(blockStream, table, bigEndian);

                        // Write all the texts in the block to a txt file
                        string outputFile = Path.Combine(outputFolder, $"{currentBlock:00}.txt");
                        File.WriteAllText(outputFile, texts);
                    }

                    // Check if there are other blocks after this one
                    if (blockEndPosition >= aldStream.Length) break;
                    aldStream.Position = blockEndPosition;
                    ushort value = br.ReadUInt16Endian(bigEndian);
                    if (value == FILE_END_PADDING) break;

                    aldStream.Position -= 2;
                    currentBlock++;
                }
            }
        }

        public static void Insert(string inputFolder, string outputAld, Table table, bool bigEndian)
        {
            if (string.IsNullOrEmpty(outputAld))
            {
                throw new ArgumentNullException(nameof(outputAld));
            }

            using (FileStream aldStream = File.Create(outputAld))
            {
                Insert(inputFolder, aldStream, table, bigEndian);
            }
        }

        public static void Insert(string inputFolder, Stream aldStream, Table table, bool bigEndian)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (aldStream == null)
            {
                throw new ArgumentNullException(nameof(aldStream));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            string[] textFilesNames = Directory.GetFiles(inputFolder, "*.txt");

            using (BinaryWriter bw = new BinaryWriter(aldStream, Encoding.UTF8, true))
            {
                for (int t = 0; t < textFilesNames.Length; t++)
                {
                    string text = File.ReadAllText(textFilesNames[t]);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        Texts.Insert(text, ms, table, bigEndian);

                        bw.WriteEndian((uint)ms.Length, bigEndian);

                        ms.Position = 0;
                        ms.CopyTo(aldStream);
                    }
                }

                bw.WriteEndian(0xFFFFFFFF, bigEndian);
            }
        }
    }
}