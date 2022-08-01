using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX_Dreamcast : RDX
    {
        protected override bool IsBigEndian => false;

        readonly uint[] languageSubBlockIndices = new uint[] { 14 };
        protected override uint[] LanguageSubBlockIndices => languageSubBlockIndices;

        const uint MAGIC = 0x41200000;
        const uint MAGIC_2 = 0x40051eb8;

        const uint TEXT_DATA_BLOCK_SUBBLOCK_COUNT = 16;

        public override Results Extract(Stream rdxStream, string rdxFileName, string outputFolder, int language, Table table)
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
                uint magic = br.ReadUInt32Endian(IsBigEndian);

                if (magic != MAGIC && magic != MAGIC_2)
                {
                    return Results.NotValidRdxFile;
                }

                rdxStream.Position = 0x10;

                uint textDataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk1DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk2DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk3DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint textureDataBlockPosition = br.ReadUInt32Endian(IsBigEndian);

                rdxStream.Position = 0x60;

                byte[] authorName = new byte[32];
                rdxStream.Read(authorName, 0, 32);

                // Start reading the text block data

                rdxStream.Position = textDataBlockPosition;

                uint[] subBlockPositions = new uint[TEXT_DATA_BLOCK_SUBBLOCK_COUNT];
                for (int sb = 0; sb < TEXT_DATA_BLOCK_SUBBLOCK_COUNT; sb++)
                {
                    subBlockPositions[sb] = br.ReadUInt32Endian(IsBigEndian);
                }

                // Extract all the subBlocks that contain texts

                string textOutputFileName = Path.Combine(outputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_DC.GetLanguageCode(language)));
                uint textPosition = subBlockPositions[languageSubBlockIndices[0]];
                ExtractTexts(rdxStream, textOutputFileName, table, textPosition);

                // Read texture block data

                rdxStream.Position = textureDataBlockPosition;

                uint numberOfTextures = br.ReadUInt32Endian(IsBigEndian);

                uint[] texturePositions = new uint[numberOfTextures];
                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    texturePositions[tp] = br.ReadUInt32Endian(IsBigEndian);
                }

                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    rdxStream.Position = texturePositions[tp];
                    PVR.Extract(rdxStream, Path.Combine(outputFolder, $"PVR-{tp:0000}"));
                }
            }

            return Results.Success;
        }

        public override void Insert(string inputFolder, Stream rdxStream, string rdxFileName, int language, Table table)
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

            //string stringsFile = Path.Combine(inputFolder, STRINGS_FILE_NAME);

            //if (!File.Exists(stringsFile))
            //{
            //    throw new FileNotFoundException($"File \"{stringsFile}\" does not exist!", stringsFile);
            //}

            //string[] tim2Paths = Directory.GetDirectories(inputFolder);

            //if (tim2Paths.Length == 0)
            //{
            //    throw new DirectoryNotFoundException($"No TIM2 directories have been found in \"{inputFolder}\".");
            //}

            // Gather data we need to preserve from the original RDX

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            using (BinaryWriter bw = new BinaryWriter(rdxStream, Encoding.UTF8, true))
            {
                uint magic = br.ReadUInt32Endian(IsBigEndian);
                if (magic != MAGIC && magic != MAGIC_2)
                {
                    throw new InvalidDataException("Not a valid RDX file.");
                }

                rdxStream.Position = 0x10;

                uint textDataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk1DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk2DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint unk3DataBlockPosition = br.ReadUInt32Endian(IsBigEndian);
                uint textureDataBlockPosition = br.ReadUInt32Endian(IsBigEndian);

                rdxStream.Position = 0x60;

                byte[] authorName = new byte[32];
                rdxStream.Read(authorName, 0, 32);

                // Start reading the text block data

                rdxStream.Position = textDataBlockPosition;

                uint[] subBlockPositions = new uint[TEXT_DATA_BLOCK_SUBBLOCK_COUNT];
                for (int sb = 0; sb < TEXT_DATA_BLOCK_SUBBLOCK_COUNT; sb++)
                {
                    subBlockPositions[sb] = br.ReadUInt32Endian(IsBigEndian);
                }

                // We are only interested in subBlock 14, which contains texts

                if (subBlockPositions[14] != 0)
                {
                    uint subBlock14Size = (subBlockPositions[15] == 0 ? unk1DataBlockPosition : subBlockPositions[15]) - subBlockPositions[14];

                    string texts = File.ReadAllText(Path.Combine(inputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_PS2.GetLanguageCode(language))));

                    using (MemoryStream textsStream = new MemoryStream())
                    {
                        Texts.Insert(texts, textsStream, table, IsBigEndian);

                        if (textsStream.Length > subBlock14Size)
                        {
                            // If the new textsBlock is bigger than subBlock 14,
                            // place it just where the textureDataBlock is and move the textureDataBlock after the texts.
                            // The game overwrites the textureDataBlock after textures have been loaded to GPU,
                            // so anything stored after the textureDataBlock can be overwritten at any time, causing crashes.
                            // That's why we put texts before textures. We could just leave the textsBlock in its original
                            // place and move everything after that, it would require to reverse engineer the whole format.
                            // Moving the texture block is easy.

                            // Delete previous sub subBlock 14 and 15 data

                            rdxStream.Position = subBlockPositions[14];
                            for (int b = 0; b < subBlock14Size; b++) rdxStream.WriteByte(0);

                            // Copy the whole texture block

                            byte[] textureData = new byte[rdxStream.Length - textureDataBlockPosition];
                            rdxStream.Position = textureDataBlockPosition;
                            rdxStream.Read(textureData, 0, textureData.Length);

                            // Update texts block pointer

                            rdxStream.Position = textDataBlockPosition + (4 * 14);
                            bw.WriteEndian(textureDataBlockPosition, IsBigEndian);

                            // Copy texts data to texture block area

                            rdxStream.Position = textureDataBlockPosition;
                            textsStream.Position = 0;
                            textsStream.CopyTo(rdxStream);

                            // Place the texture block after the new texts block

                            uint previousTextureDataBlockPosition = textureDataBlockPosition;
                            textureDataBlockPosition = Utils.Padding((uint)rdxStream.Position, 16);
                            rdxStream.Position = textureDataBlockPosition;
                            rdxStream.Write(textureData, 0, textureData.Length);

                            // Update texture block pointer

                            rdxStream.Position = 0x20;
                            bw.WriteEndian(textureDataBlockPosition, IsBigEndian);

                            // Change all absolute pointers in the texture block

                            rdxStream.Position = textureDataBlockPosition;

                            uint textureCount = br.ReadUInt32Endian(IsBigEndian);

                            for (int tp = 0; tp < textureCount; tp++)
                            {
                                uint pointer = br.ReadUInt32Endian(IsBigEndian);
                                pointer += textureDataBlockPosition - previousTextureDataBlockPosition;
                                rdxStream.Position -= 4;
                                bw.WriteEndian(pointer, IsBigEndian);
                            }
                        }
                        else
                        {
                            // If the new textsBlock fits inside subBlock 14, just overwrite.

                            // Copy texts data

                            rdxStream.Position = subBlockPositions[14];
                            textsStream.Position = 0;
                            textsStream.CopyTo(rdxStream);

                            // Fill remaining bytes with zeroes

                            int remainingBytes = (int)(subBlockPositions[15] - rdxStream.Position);
                            for (int b = 0; b < remainingBytes; b++) rdxStream.WriteByte(0);
                        }
                    }
                }

                // Read texture block data

                rdxStream.Position = textureDataBlockPosition;

                uint numberOfTextures = br.ReadUInt32Endian(IsBigEndian);

                uint[] texturePositions = new uint[numberOfTextures];
                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    texturePositions[tp] = br.ReadUInt32Endian(IsBigEndian);
                }

                for (int tp = 0; tp < numberOfTextures; tp++)
                {
                    string TIM2Folder = Path.Combine(inputFolder, $"TIM2-{tp:0000}");
                    if (!Directory.Exists(TIM2Folder)) continue;

                    rdxStream.Position = texturePositions[tp];

                    //uint textureSize;
                    //if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                    //else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                    TM2.Insert(TIM2Folder, rdxStream);
                }
            }
        }
    }
}