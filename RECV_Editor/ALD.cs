using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor
{
    class ALD
    {
        const ushort TEXT_END = 0xFFFF;
        const ushort BLOCK_END = 0xFF01;
        const ushort FILE_END_PADDING = 0xFFFF;

        const string TEXT_END_STRING = "\n[TEXT END]----------------------------------------------\n";
        const string BLOCK_END_STRING = "[BLOCK END]---------------------------------------------\n";

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
                    string texts = ExtractTexts(blockData, table);

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

        public static string ExtractTexts(byte[] textData, Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table", "Table cannot be null.");
            }

            StringBuilder sb = new StringBuilder();
            List<ushort> characters = new List<ushort>();

            using (MemoryStream ms = new MemoryStream(textData))
            using (BinaryReader br = new BinaryReader(ms))
            {
                uint numberOfTexts = br.ReadUInt32();

                uint[] pointers = new uint[numberOfTexts];

                // Read each pointer
                for (int p = 0; p < numberOfTexts; p++)
                {
                    pointers[p] = br.ReadUInt32();
                }

                // Read each text
                for (int t = 0; t < numberOfTexts; t++)
                {
                    ms.Position = pointers[t];

                    characters.Clear();

                    // Read each text character until we reach an TEXT_END code
                    for (; ; )
                    {
                        ushort character = br.ReadUInt16();

                        if (character == TEXT_END)
                        {
                            if (characters.Count == 0)
                            {
                                // We can find empty texts that consist only of a TEXT_END code.
                                // Don't try to read the previous character in those cases, as there isn't any.

                                break;
                            }
                            else
                            {
                                // 0xFFFF is usually an end of text code, but it can also appear after
                                // some special codes in the middle of a text. Make sure we don't treat
                                // special codes as end of texts by checking the previous character.

                                ushort previousCharacter = characters[characters.Count - 1];
                                if (previousCharacter != Table.TIME_CODE && previousCharacter != Table.ITEM_CODE)
                                {
                                    break;
                                }
                            }
                        }

                        characters.Add(character);
                    }

                    sb.Append(table.GetString(characters.ToArray()));
                    sb.Append(TEXT_END_STRING);
                }

                // Make sure of being at the end of the text data block
                ushort value = br.ReadUInt16();
                if (value != BLOCK_END && value != 0x0)
                {
                    Logger.Append($"Expected end of block but 0x{value:X} found at 0x{(ms.Position - 2):X}. This could mean that there are some unused contents at the end of this file.", Logger.LogTypes.Warning);
                }

                sb.Append(BLOCK_END_STRING);
            }

            return sb.ToString();
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