using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    static class Texts
    {
        const ushort TEXT_END = 0xFFFF;

        const string TEXT_END_STRING = "[TEXT END]----------------------------------------------";
        const string BLOCK_END_STRING = "[BLOCK END]---------------------------------------------";

        public static string Extract(string inputFile, Table table, bool bigEndian)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new ArgumentNullException(nameof(inputFile));
            }

            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            using (FileStream fs = File.OpenRead(inputFile))
            {
                return Extract(fs, table, bigEndian);
            }
        }

        public static string Extract(Stream textStream, Table table, bool bigEndian)
        {
            if (textStream == null)
            {
                throw new ArgumentNullException(nameof(textStream));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            StringBuilder sb = new StringBuilder();
            List<ushort> characters = new List<ushort>();

            using (BinaryReader br = new BinaryReader(textStream, Encoding.UTF8, true))
            {
                uint numberOfTexts = br.ReadUInt32Endian(bigEndian);

                uint[] pointers = new uint[numberOfTexts];

                // Read each pointer
                for (int p = 0; p < numberOfTexts; p++)
                {
                    pointers[p] = br.ReadUInt32Endian(bigEndian);
                }

                // Read each text
                for (int t = 0; t < numberOfTexts; t++)
                {
                    textStream.Position = pointers[t];

                    characters.Clear();

                    // Read each text character until we reach an TEXT_END code
                    for (; ; )
                    {
                        ushort character = br.ReadUInt16Endian(bigEndian);

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
                    sb.Append($"\n{TEXT_END_STRING}\n");
                }

                // Make sure of being at the end of the text data block
                //ushort value = br.ReadUInt16Endian(bigEndian);
                //if (value != BLOCK_END && value != 0x0)
                //{
                //    Logger.Append($"Expected end of block but 0x{value:X} found at 0x{(textStream.Position - 2):X}. This could mean that there are some unused contents at the end of this file.", Logger.LogTypes.Warning);
                //}

                sb.Append($"{BLOCK_END_STRING}\n");
            }

            return sb.ToString();
        }

        public static void Insert(string inputText, Stream outputStream, Table table, bool bigEndian)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                throw new ArgumentNullException(nameof(inputText));
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            StringBuilder sb = new StringBuilder();
            List<uint> textPositions = new List<uint>();
            uint currentTextPosition = 0;

            using (MemoryStream ms = new MemoryStream())
            {
                using (StringReader reader = new StringReader(inputText))
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    for (; ; )
                    {
                        string line = reader.ReadLine();
                        if (line == null) throw new EndOfStreamException("Reached end of text data without finding an end of block code.");

                        if (line == BLOCK_END_STRING)
                        {
                            uint padding = Utils.Padding((uint)ms.Position, 4);
                            padding -= (uint)ms.Position;
                            for (uint p = 0; p < padding; p++) bw.Write((byte)0xFF);
                            break;
                        }

                        if (line == TEXT_END_STRING)
                        {
                            // Remove the "\n" from the previous line
                            sb.Length--;

                            ushort[] hex = table.GetHex(sb.ToString());

                            for (int h = 0; h < hex.Length; h++)
                            {
                                bw.WriteEndian(hex[h], bigEndian);
                            }

                            bw.WriteEndian(TEXT_END, bigEndian);

                            sb.Clear();

                            textPositions.Add(currentTextPosition);
                            currentTextPosition = (uint)ms.Position;
                        }
                        else
                        {
                            sb.Append(line);
                            sb.Append("\n");
                        }
                    }
                }

                using (BinaryWriter bw = new BinaryWriter(outputStream, Encoding.UTF8, true))
                {
                    bw.WriteEndian((uint)textPositions.Count, bigEndian);

                    for (int tp = 0; tp < textPositions.Count; tp++)
                    {
                        bw.WriteEndian((uint)(textPositions[tp] + 4 + (4 * textPositions.Count)), bigEndian);
                    }

                    ms.Position = 0;
                    ms.CopyTo(outputStream);
                }
            }
        }
    }
}