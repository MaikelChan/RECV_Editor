using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX
    {
        const uint MAGIC = 0x41200000;
        const string STRINGS_FILE_NAME = "Strings.txt";

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
            if (rdxStream == null)
            {
                throw new ArgumentNullException(nameof(rdxStream));
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
                for (int sb = 0; sb < 16; sb++)
                {
                    subBlockPositions[sb] = br.ReadUInt32();
                }

                // We are only interested in subBlock 14, which contains texts

                if (subBlockPositions[14] != 0)
                {
                    rdxStream.Position = subBlockPositions[14];

                    using (SubStream textsStream = new SubStream(rdxStream, 0, rdxStream.Length - rdxStream.Position, true))
                    {
                        string texts = Texts.Extract(textsStream, table);
                        File.WriteAllText(Path.Combine(outputFolder, STRINGS_FILE_NAME), texts);
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

        public static void Insert(string inputFolder, string outputFile, Table table)
        {
            if (string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }

            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException($"File \"{outputFile}\" does not exist!", outputFile);
            }

            using (FileStream outputStream = new FileStream(outputFile, FileMode.Open, FileAccess.ReadWrite))
            {
                Insert(inputFolder, outputStream, table);
            }
        }

        public static void Insert(string inputFolder, Stream rdxStream, Table table)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (rdxStream == null)
            {
                throw new ArgumentNullException(nameof(rdxStream));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            string stringsFile = Path.Combine(inputFolder, STRINGS_FILE_NAME);

            if (!File.Exists(stringsFile))
            {
                throw new FileNotFoundException($"File \"{stringsFile}\" does not exist!", stringsFile);
            }

            string[] tim2Paths = Directory.GetDirectories(inputFolder);

            if (tim2Paths.Length == 0)
            {
                throw new DirectoryNotFoundException($"No TIM2 directories have been found in \"{inputFolder}\".");
            }

            // Gather data we need to preserve from the original RDX

            Data data = new Data(rdxStream);



            return;

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            {
                uint magic = br.ReadUInt32();
                if (magic != MAGIC)
                {
                    throw new InvalidDataException("Not a valid RDX file.");
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
                for (int sb = 0; sb < 16; sb++)
                {
                    subBlockPositions[sb] = br.ReadUInt32();
                }

                byte[][] subBlockData = new byte[16][];
                for (int sb = 0; sb < 16; sb++)
                {
                    if (subBlockPositions[sb] == 0) continue;

                    rdxStream.Position = subBlockPositions[sb];

                    uint subBlockLength;
                    if (sb == 15) subBlockLength = unk1DataBlockPosition - subBlockPositions[sb];
                    else subBlockLength = subBlockPositions[sb + 1] - subBlockPositions[sb];

                    subBlockData[sb] = new byte[subBlockLength];
                    rdxStream.Read(subBlockData[sb], 0, (int)subBlockLength);
                }

                // We are only interested in subBlock 14, which contains texts

                if (subBlockPositions[14] != 0)
                {
                    string texts = File.ReadAllText(Path.Combine(inputFolder, STRINGS_FILE_NAME));

                    using (MemoryStream textsStream = new MemoryStream())
                    {
                        Texts.Insert(texts, textsStream, table);
                        subBlockData[14] = textsStream.ToArray();
                    }
                }

                // Modify subBlockPositions according to new subBlock lengths

                uint currentPosition = 0;

                for (int sb = 0; sb < 16; sb++)
                {
                    if (subBlockPositions[sb] == 0) continue;

                    currentPosition += (uint)subBlockData[sb].Length;

                    // The first position will always remain the same
                    if (sb == 0) continue;

                    subBlockPositions[sb] = currentPosition;
                }

                // Read texture block data

                //rdxStream.Position = textureDataBlockPosition;

                //uint numberOfTextures = br.ReadUInt32();

                //uint[] texturePositions = new uint[numberOfTextures];
                //for (int tp = 0; tp < numberOfTextures; tp++)
                //{
                //    texturePositions[tp] = br.ReadUInt32();
                //}

                //for (int tp = 0; tp < numberOfTextures; tp++)
                //{
                //    rdxStream.Position = texturePositions[tp];

                //    uint textureSize;
                //    if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                //    else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                //    using (SubStream tm2Stream = new SubStream(rdxStream, 0, textureSize, true))
                //    {
                //        TM2.Extract(rdxStream, Path.Combine(outputFolder, $"TIM2-{tp:0000}"));
                //    }
                //}
            }
        }

        class Data
        {
            readonly IBlock[] Blocks;

            readonly byte[] AuthorName;

            const uint BLOCK_COUNT = 5;

            public Data(Stream rdxStream)
            {
                using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
                {
                    uint magic = br.ReadUInt32();
                    if (magic != MAGIC)
                    {
                        throw new InvalidDataException("Not a valid RDX file.");
                    }

                    // Blocks

                    rdxStream.Position = 0x10;

                    uint textDataBlockPosition = br.ReadUInt32();
                    uint unk1DataBlockPosition = br.ReadUInt32();
                    uint unk2DataBlockPosition = br.ReadUInt32();
                    uint unk3DataBlockPosition = br.ReadUInt32();
                    uint textureDataBlockPosition = br.ReadUInt32();

                    Blocks = new IBlock[BLOCK_COUNT];
                    Blocks[0] = new TextsBlock(rdxStream, textDataBlockPosition, unk1DataBlockPosition);
                    Blocks[1] = new ContainerBlock(rdxStream, 23, unk1DataBlockPosition, unk2DataBlockPosition);
                    Blocks[2] = new ContainerBlock(rdxStream, 6, unk2DataBlockPosition, unk3DataBlockPosition);
                    Blocks[3] = new GenericBlock(rdxStream, unk3DataBlockPosition, textureDataBlockPosition);
                    Blocks[4] = new TextureBlock(rdxStream, textureDataBlockPosition, (uint)rdxStream.Length);

                    // Author name

                    rdxStream.Position = 0x60;

                    AuthorName = new byte[32];
                    rdxStream.Read(AuthorName, 0, 32);
                }
            }

            public void Save(string outputFileName)
            {

            }
        }

        interface IBlock
        {
            uint Size { get; }
        }

        class TextsBlock : IBlock
        {
            public uint Size
            {
                get
                {
                    uint size = 0;

                    // Pointers
                    size += BLOCK_COUNT * 4;

                    size += (uint)UnknownData.Length;

                    for (int b = 0; b < BLOCK_COUNT; b++)
                    {
                        if (Blocks[b] == null) continue;
                        size += Blocks[b].Size;
                    }

                    return size;
                }
            }

            readonly uint UnknownPointer;
            readonly IBlock[] Blocks;
            readonly byte[] UnknownData;

            const uint BLOCK_COUNT = 16;

            public TextsBlock(Stream rdxStream, uint startPosition, uint endPosition)
            {
                Blocks = new IBlock[BLOCK_COUNT];

                using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
                {
                    rdxStream.Position = startPosition;

                    // Block pointers

                    uint[] blockPositions = new uint[BLOCK_COUNT];
                    for (int b = 0; b < BLOCK_COUNT; b++)
                    {
                        // Special case. This is not a position of any block data. We don't know what it is.
                        if (b == 12) UnknownPointer = br.ReadUInt32();
                        else blockPositions[b] = br.ReadUInt32();
                    }

                    // Unknown data

                    uint unknownDataLength = GetNextBlockPosition(blockPositions, -1, endPosition) - (uint)rdxStream.Position;
                    UnknownData = new byte[unknownDataLength];
                    rdxStream.Read(UnknownData, 0, UnknownData.Length);

                    // Block data

                    for (int b = 0; b < BLOCK_COUNT; b++)
                    {
                        if (blockPositions[b] == 0) continue;

                        rdxStream.Position = blockPositions[b];
                        uint blockLength = GetNextBlockPosition(blockPositions, b, endPosition) - blockPositions[b];
                        Blocks[b] = new GenericBlock(rdxStream, blockPositions[b], blockPositions[b] + blockLength);
                    }
                }
            }

            uint GetNextBlockPosition(uint[] blockPositions, int blockIndex, uint endPosition)
            {
                if (blockIndex < -1 || blockIndex > BLOCK_COUNT - 1) throw new ArgumentOutOfRangeException(nameof(blockIndex));

                for (int b = blockIndex + 1; b < BLOCK_COUNT; b++)
                {
                    if (blockPositions[b] != 0) return blockPositions[b];
                }

                return endPosition;
            }
        }

        class GenericBlock : IBlock
        {
            public uint Size => (uint)Data.Length;

            readonly byte[] Data;

            public GenericBlock(Stream rdxStream, uint startPosition, uint endPosition)
            {
                rdxStream.Position = startPosition;
                Data = new byte[endPosition - startPosition];
                rdxStream.Read(Data, 0, Data.Length);
            }
        }

        class ContainerBlock : IBlock
        {
            public uint Size
            {
                get
                {
                    uint size = 0;

                    // Pointers
                    size += (uint)Blocks.Length * 4;

                    // Padding between pointers and first block data
                    size += paddingSize;

                    for (int b = 0; b < Blocks.Length; b++)
                    {
                        size += Blocks[b].Size;
                    }

                    return size;
                }
            }

            readonly IBlock[] Blocks;
            readonly uint paddingSize;

            public ContainerBlock(Stream rdxStream, uint blockCount, uint startPosition, uint endPosition)
            {
                Blocks = new IBlock[blockCount];

                rdxStream.Position = startPosition;

                using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
                {
                    uint[] blockPositions = new uint[blockCount];
                    for (int b = 0; b < blockCount; b++)
                    {
                        blockPositions[b] = br.ReadUInt32();
                    }

                    for (int b = 0; b < blockCount; b++)
                    {
                        rdxStream.Position = blockPositions[b];

                        uint length;
                        if (b == blockCount - 1) length = endPosition - blockPositions[b];
                        else length = blockPositions[b + 1] - blockPositions[b];

                        Blocks[b] = new GenericBlock(rdxStream, blockPositions[b], blockPositions[b] + length);
                    }

                    paddingSize = blockPositions[0] - (startPosition + ((uint)Blocks.Length * 4));
                }
            }
        }

        class TextureBlock : IBlock
        {
            public uint Size => throw new NotImplementedException();

            public TextureBlock(Stream rdxStream, uint startPosition, uint endPosition)
            {

            }
        }
    }
}