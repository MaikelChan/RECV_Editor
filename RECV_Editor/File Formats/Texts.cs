using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    static class Texts
    {
        const ushort TEXT_END = 0xFFFF;
        const ushort BLOCK_END = 0xFF01;

        const string TEXT_END_STRING = "\n[TEXT END]----------------------------------------------\n";
        const string BLOCK_END_STRING = "[BLOCK END]---------------------------------------------\n";

        public static string Extract(string inputFile, Table table)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            byte[] data = File.ReadAllBytes(inputFile);

            return Extract(data, table);
        }

        public static string Extract(byte[] textData, Table table)
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
                                // 0xFFFF is usually an TEXT_END code, but it can also appear after
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
    }
}