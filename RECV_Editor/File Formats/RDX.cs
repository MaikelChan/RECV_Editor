using System.IO;

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

            byte[] data = File.ReadAllBytes(inputFile);

            return Extract(data, outputFolder, table);
        }

        public static Results Extract(byte[] rdxData, string outputFolder, Table table)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (MemoryStream ms = new MemoryStream(rdxData))
            using (BinaryReader br = new BinaryReader(ms))
            {
                uint magic = br.ReadUInt32();
                if (magic != MAGIC)
                {
                    return Results.NotValidRdxFile;
                }

                ms.Position = 0x10;

                uint textDataBlockPosition = br.ReadUInt32();
                uint unk1DataBlockPosition = br.ReadUInt32();
                uint unk2DataBlockPosition = br.ReadUInt32();
                uint unk3DataBlockPosition = br.ReadUInt32();
                uint textureDataBlockPosition = br.ReadUInt32();

                ms.Position = 0x60;

                byte[] authorName = new byte[32];
                ms.Read(authorName, 0, 32);

                // Start reading the text block data

                ms.Position = textDataBlockPosition;

                uint[] subBlockPositions = new uint[16];
                for (int sbp = 0; sbp < 16; sbp++)
                {
                    subBlockPositions[sbp] = br.ReadUInt32();
                }

                // We are only interested in subBlock 14, which contains texts

                if (subBlockPositions[14] != 0)
                {
                    ms.Position = subBlockPositions[14];

                    uint subBlockSize;
                    if (subBlockPositions[15] != 0)
                    {
                        subBlockSize = subBlockPositions[15] - subBlockPositions[14];
                    }
                    else
                    {
                        subBlockSize = unk1DataBlockPosition - subBlockPositions[14];
                    }

                    byte[] subBlockData = new byte[subBlockSize];
                    ms.Read(subBlockData, 0, (int)subBlockSize);

                    string texts = Texts.Extract(subBlockData, table);
                    File.WriteAllText(Path.Combine(outputFolder, "Strings.txt"), texts);
                }

                // Read texture block data

                ms.Position = textureDataBlockPosition;

                uint numberOfTextures = br.ReadUInt32();

                uint[] texturePositions = new uint[numberOfTextures];
                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    texturePositions[tp] = br.ReadUInt32();
                }

                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    ms.Position = texturePositions[tp];

                    uint textureSize;
                    if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                    else textureSize = (uint)ms.Length - texturePositions[tp];

                    byte[] textureData = new byte[textureSize];
                    ms.Read(textureData, 0, (int)textureSize);

                    TM2.Extract(textureData, Path.Combine(outputFolder, $"TIM2-{tp:0000}"));
                }
            }

            return Results.Success;
        }
    }
}