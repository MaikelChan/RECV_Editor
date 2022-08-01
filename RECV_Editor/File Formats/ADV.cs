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
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

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

                string pathBaseName = Path.Combine(outputFolder, o.ToString());

                uint headerLength = br.ReadUInt32Endian(bigEndian);
                advStream.Position -= 4;

                using (SubStream ss = new SubStream(advStream, 0, endOffset - advStream.Position, true))
                {
                    if (headerLength == HEADER_SIZE)
                    {
                        ExtractFile(ss, br, pathBaseName, table, bigEndian);
                    }
                    else if (headerLength == 0)
                    {
                        // Some rare case where 32 bytes of zeroes are found

                        using (FileStream fs = File.Create(pathBaseName + ".bin"))
                        {
                            ss.CopyTo(fs);
                        }
                    }
                    else if (PVR.IsValid(ss))
                    {
                        PVR.ExtractSimplified(ss, pathBaseName);
                    }
                    else
                    {
                        string texts = Texts.Extract(ss, table, bigEndian);
                        File.WriteAllText(pathBaseName + ".txt", texts);
                    }
                }
            }
        }

        public static void Insert(string inputFolder, Stream advStream, Table table, bool bigEndian)
        {
            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (advStream == null)
            {
                throw new ArgumentNullException(nameof(advStream));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            using (BinaryWriter bw = new BinaryWriter(advStream, Encoding.UTF8, true))
            {
                InsertFile(inputFolder, advStream, bw, table, bigEndian);
            }
        }

        static void InsertFile(string inputFolder, Stream advStream, BinaryWriter bw, Table table, bool bigEndian)
        {
            uint baseOffset = (uint)advStream.Position;
            uint headerPosition = baseOffset;
            uint dataPosition = headerPosition + HEADER_SIZE;

            string[] files = Directory.GetFiles(inputFolder);
            string[] folders = Directory.GetDirectories(inputFolder);

            int totalFiles = files.Length + folders.Length;

            for (int f = 0; f < totalFiles; f++)
            {
                string name = FindFileOrFolder(inputFolder, files, folders, f);
                string extension = Path.GetExtension(name);

                advStream.Position = headerPosition;
                bw.Write(dataPosition - baseOffset);

                advStream.Position = dataPosition;

                switch (extension)
                {
                    case ".txt":
                    {
                        string text = File.ReadAllText(name);
                        Texts.Insert(text, advStream, table, bigEndian);

                        break;
                    }
                    case ".pvp":
                    {
                        byte[] bytes = File.ReadAllBytes(name);
                        advStream.Write(bytes, 0, bytes.Length);

                        for (int i = 0; i < 4; i++) bw.Write(0);

                        break;
                    }
                    case ".pvr":
                    case ".bin":
                    {
                        byte[] bytes = File.ReadAllBytes(name);
                        advStream.Write(bytes, 0, bytes.Length);

                        break;
                    }
                    case "":
                    {
                        InsertFile(name, advStream, bw, table, bigEndian);

                        break;
                    }
                    default:
                    {
                        throw new Exception($"Unexpected extension {extension}.");
                    }
                }

                uint padding = Utils.Padding((uint)advStream.Position, 0x20) - (uint)advStream.Position;
                for (uint p = 0; p < padding; p++) bw.Write((byte)0);

                headerPosition += 4;
                dataPosition = (uint)advStream.Position;
            }
        }

        static string FindFileOrFolder(string inputFolder, string[] files, string[] folders, int index)
        {
            for (int f = 0; f < files.Length; f++)
            {
                string name = Path.GetFileNameWithoutExtension(files[f]);
                if (int.Parse(name) == index) return files[f];
            }

            for (int f = 0; f < folders.Length; f++)
            {
                string name = Path.GetFileNameWithoutExtension(folders[f]);
                if (int.Parse(name) == index) return folders[f];
            }

            throw new FileNotFoundException($"File with index {index} in {inputFolder} has not been found.");
        }
    }
}