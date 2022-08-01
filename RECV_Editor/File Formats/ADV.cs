using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class ADV
    {
        const uint HEADER_SIZE = 0x20;
        const uint MAX_FILES = 0x8;

        public static void Extract(Stream advStream, string outputFolder, Table table, bool bigEndian)
        {
            if (advStream == null)
            {
                throw new ArgumentNullException(nameof(advStream));
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            using (BinaryReader br = new BinaryReader(advStream, Encoding.UTF8, true))
            {
                // We only need those that start with 0x00000020 

                uint headerLength = br.ReadUInt32Endian(bigEndian);
                if (headerLength != HEADER_SIZE) return;

                advStream.Position -= 4;

                ExtractFile(advStream, br, outputFolder, table, bigEndian);
            }
        }

        static void ExtractFile(Stream advStream, BinaryReader br, string outputFolder, Table table, bool bigEndian)
        {
            uint[] offsets = new uint[MAX_FILES];
            for (int o = 0; o < MAX_FILES; o++)
            {
                offsets[o] = br.ReadUInt32Endian(bigEndian);
            }

            for (int o = 0; o < MAX_FILES; o++)
            {
                if (offsets[o] == 0) break;

                uint endOffset;
                if (o + 1 >= MAX_FILES) endOffset = (uint)advStream.Length;
                else if (offsets[o + 1] == 0) endOffset = (uint)advStream.Length;
                else endOffset = offsets[o + 1];

                advStream.Position = offsets[o];

                string oFolder = Path.Combine(outputFolder, o.ToString());

                uint headerLength = br.ReadUInt32Endian(bigEndian);
                advStream.Position -= 4;

                using (SubStream ss = new SubStream(advStream, 0, endOffset - advStream.Position, true))
                {
                    if (headerLength == HEADER_SIZE)
                    {
                        ExtractFile(ss, br, oFolder, table, bigEndian);
                    }
                    else if (headerLength == 0)
                    {
                        // Some rare case where 32 bytes of zeroes are found

                        if (!Directory.Exists(oFolder)) Directory.CreateDirectory(oFolder);

                        using (FileStream fs = File.Create(Path.Combine(oFolder, "data.bin")))
                        {
                            ss.CopyTo(fs);
                        }
                    }
                    else if (PVR.IsValid(ss))
                    {
                        PVR.ExtractSimplified(ss, oFolder);
                    }
                    else
                    {
                        string texts = Texts.Extract(ss, table, bigEndian);
                        string outputFile = Path.Combine(oFolder, "texts.txt");

                        if (!Directory.Exists(oFolder)) Directory.CreateDirectory(oFolder);
                        File.WriteAllText(outputFile, texts);
                    }
                }
            }
        }
    }
}