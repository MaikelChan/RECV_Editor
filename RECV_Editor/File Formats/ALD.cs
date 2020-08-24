using System;
using System.IO;

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

            byte[] data = File.ReadAllBytes(inputFile);

            Extract(data, outputFolder, table);
        }

        public static void Extract(byte[] aldData, string outputFolder, Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table", "Table cannot be null.");
            }

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (MemoryStream ms = new MemoryStream(aldData))
            using (BinaryReader br = new BinaryReader(ms))
            {
                uint currentBlock = 0;

                // Read each block
                for (; ; )
                {
                    //uint blockStartPosition = (uint)ms.Position;
                    uint blockSize = br.ReadUInt32();

                    byte[] blockData = new byte[blockSize];
                    ms.Read(blockData, 0, (int)blockSize);

                    // Obtain all texts in the block
                    string texts = Texts.Extract(blockData, table);

                    // Write all the texts in the block to a txt file
                    string outputFile = Path.Combine(outputFolder, $"{currentBlock: 00}.txt");
                    File.WriteAllText(outputFile, texts);

                    // Check if there are other blocks after this one
                    if (ms.Position >= ms.Length) break;
                    ushort value = br.ReadUInt16();
                    if (value == FILE_END_PADDING) break;

                    ms.Position -= 2;
                    currentBlock++;
                }
            }
        }

        //public static void Extract(byte[] aldData, string outputFolder, Table table)
        //{
        //    if (table == null)
        //    {
        //        throw new ArgumentNullException("table", "Table cannot be null.");
        //    }

        //    if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        //    using (MemoryStream ms = new MemoryStream(aldData))
        //    using (BinaryReader br = new BinaryReader(ms))
        //    {
        //        uint currentBlock = 0;

        //        // Read each block
        //        for (; ; )
        //        {
        //            uint blockStartPosition = (uint)ms.Position;
        //            uint blockSize = br.ReadUInt32();
        //            uint numberOfTexts = br.ReadUInt32();

        //            uint[] pointers = new uint[numberOfTexts];

        //            // Read each pointer
        //            for (int p = 0; p < numberOfTexts; p++)
        //            {
        //                pointers[p] = br.ReadUInt32();
        //            }

        //            StringBuilder sb = new StringBuilder();

        //            // Read each text inside the block
        //            for (int t = 0; t < numberOfTexts; t++)
        //            {
        //                ms.Position = pointers[t] + blockStartPosition + 4;

        //                uint characterCount;
        //                if (t < numberOfTexts - 1)
        //                    characterCount = pointers[t + 1] + blockStartPosition + 4 - (uint)ms.Position;
        //                else
        //                    characterCount = blockSize + blockStartPosition + 2 - (uint)ms.Position;

        //                characterCount >>= 1;

        //                List<ushort> hexData = new List<ushort>();

        //                // Read each text character except the last end character
        //                for (int c = 0; c < characterCount - 1; c++)
        //                {
        //                    hexData.Add(br.ReadUInt16());
        //                }

        //                sb.Append(table.GetString(hexData.ToArray()));

        //                // Make sure of being at the end of the text
        //                ushort hex = br.ReadUInt16();
        //                if (hex != TEXT_END) throw new Exception($"Expected end of text but 0x{hex:X} found at 0x{(ms.Position - 2):X}.");
        //                sb.Append(TEXT_END_STRING);
        //            }

        //            // Make sure of being at the end of the block
        //            ushort value = br.ReadUInt16();
        //            if (value != BLOCK_END) throw new Exception($"Expected end of block but 0x{value:X} found at 0x{(ms.Position - 2):X}.");
        //            sb.Append(BLOCK_END_STRING);

        //            // Write all the texts in the block to a txt file
        //            string outputFile = Path.Combine(outputFolder, $"{currentBlock: 00}.txt");
        //            File.WriteAllText(outputFile, sb.ToString());

        //            // Check if there are other blocks after this one
        //            if (ms.Position >= ms.Length) break;
        //            value = br.ReadUInt16();
        //            if (value == FILE_END_PADDING) break;

        //            ms.Position -= 2;
        //            currentBlock++;
        //        }
        //    }
        //}
    }
}