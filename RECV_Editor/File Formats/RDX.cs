using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX
    {
        const uint MAGIC = 0x41200000;

        public enum Results { Success, NotValidRdxFile }

        public static Results Extract(string inputFile, string outputFolder, Table table)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            using (FileStream fs = File.OpenRead(inputFile))
            {
                return Extract(fs, outputFolder, table);
            }
        }

        public static Results Extract(byte[] rdxData, string outputFolder, Table table)
        {
            if (rdxData == null)
            {
                throw new ArgumentNullException(nameof(rdxData));
            }

            using (MemoryStream ms = new MemoryStream(rdxData))
            {
                return Extract(ms, outputFolder, table);
            }
        }

        public static Results Extract(Stream rdxStream, string outputFolder, Table table)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            {
                uint magic = br.ReadUInt32();
                if (magic != MAGIC)
                {
                    return Results.NotValidRdxFile;
                }

                rdxStream.Position = 0x10;

                uint textDataBlockPosition = br.ReadUInt32();
                uint unk1DataBlockPosition = br.ReadUInt32();
                uint unk2DataBlockPosition = br.ReadUInt32();
                uint unk3DataBlockPosition = br.ReadUInt32();
                uint textureDataBlockPosition = br.ReadUInt32();

                rdxStream.Position = 0x60;

                byte[] authorName = new byte[32];
                rdxStream.Read(authorName, 0, 32);

                // Start reading the text block data

                rdxStream.Position = textDataBlockPosition;

                uint[] subBlockPositions = new uint[16];
                for (int sbp = 0; sbp < 16; sbp++)
                {
                    subBlockPositions[sbp] = br.ReadUInt32();
                }

                // We are only interested in subBlock 14, which contains texts

                if (subBlockPositions[14] != 0)
                {
                    rdxStream.Position = subBlockPositions[14];

                    using (SubStream textsStream = new SubStream(rdxStream, 0, rdxStream.Length - rdxStream.Position, true))
                    {
                        string texts = Texts.Extract(textsStream, table);
                        File.WriteAllText(Path.Combine(outputFolder, "Strings.txt"), texts);
                    }
                }

                // Read texture block data

                rdxStream.Position = textureDataBlockPosition;

                uint numberOfTextures = br.ReadUInt32();

                uint[] texturePositions = new uint[numberOfTextures];
                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    texturePositions[tp] = br.ReadUInt32();
                }

                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    rdxStream.Position = texturePositions[tp];

                    uint textureSize;
                    if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                    else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                    using (SubStream tm2Stream = new SubStream(rdxStream, 0, textureSize, true))
                    {
                        TM2.Extract(rdxStream, Path.Combine(outputFolder, $"TIM2-{tp:0000}"));
                    }
                }
            }

            return Results.Success;
        }
    }
}