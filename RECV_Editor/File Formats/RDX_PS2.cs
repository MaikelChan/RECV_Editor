using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX_PS2 : RDX
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

                //if (magic == MAGIC_2)
                //{
                //    return Results.NotValidRdxFile;
                //}

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

                string textOutputFileName = Path.Combine(outputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_PS2.GetLanguageCode(language)));
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

                    //uint textureSize;
                    //if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                    //else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                    //using (SubStream tm2Stream = new SubStream(rdxStream, 0, textureSize, true))
                    //{
                    TM2.Extract(rdxStream, Path.Combine(outputFolder, $"TIM2-{tp:0000}"));
                    //}
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

                //byte[][] subBlockData = new byte[TEXT_DATA_BLOCK_SUBBLOCK_COUNT][];
                //for (int sb = 0; sb < TEXT_DATA_BLOCK_SUBBLOCK_COUNT; sb++)
                //{
                //    if (subBlockPositions[sb] == 0) continue;

                //    rdxStream.Position = subBlockPositions[sb];

                //    uint subBlockLength;
                //    if (sb == TEXT_DATA_BLOCK_SUBBLOCK_COUNT - 1) subBlockLength = unk1DataBlockPosition - subBlockPositions[sb];
                //    else subBlockLength = subBlockPositions[sb + 1] - subBlockPositions[sb];

                //    subBlockData[sb] = new byte[subBlockLength];
                //    rdxStream.Read(subBlockData[sb], 0, (int)subBlockLength);
                //}

                // We are only interested in subBlock 14, which contains texts

                // Move block 15 and 14 if necessary (breaks when moving 14)
                if (subBlockPositions[14] != 0)
                {
                    uint subBlock14Size = (subBlockPositions[15] == 0 ? unk1DataBlockPosition : subBlockPositions[15]) - subBlockPositions[14];
                    uint subBlock15Size = subBlockPositions[15] == 0 ? 0 : unk1DataBlockPosition - subBlockPositions[15];

                    string texts = File.ReadAllText(Path.Combine(inputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_PS2.GetLanguageCode(language))));

                    using (MemoryStream textsStream = new MemoryStream())
                    {
                        Texts.Insert(texts, textsStream, table, IsBigEndian);

                        // SubBlock 15 (if it exists) must always be after subBlock 14,
                        // or else the game crashes.

                        if (textsStream.Length > subBlock14Size + subBlock15Size)
                        {
                            // If the new textsBlock is bigger than both subBlocks 14 and 15,
                            // move both of them to the end of the file.

                            uint oldSubBlock14Position = subBlockPositions[14];

                            // Set new subBlock 14 position

                            subBlockPositions[14] = Utils.Padding((uint)rdxStream.Length, 16);
                            rdxStream.Position = textDataBlockPosition + (4 * 14);
                            bw.WriteEndian(subBlockPositions[14], IsBigEndian);

                            // Copy texts data

                            rdxStream.Position = subBlockPositions[14];
                            textsStream.Position = 0;
                            textsStream.CopyTo(rdxStream);

                            if (subBlockPositions[15] != 0)
                            {
                                // If it exists, read subBlock 15

                                byte[] subBlock15Data = new byte[subBlock15Size];
                                rdxStream.Position = subBlockPositions[15];
                                rdxStream.Read(subBlock15Data, 0, subBlock15Data.Length);

                                // Write subBlock 15 after 14

                                subBlockPositions[15] = Utils.Padding((uint)rdxStream.Length, 16);
                                rdxStream.Position = textDataBlockPosition + (4 * 15);
                                bw.WriteEndian(subBlockPositions[15], IsBigEndian);

                                rdxStream.Position = subBlockPositions[15];
                                rdxStream.Write(subBlock15Data, 0, subBlock15Data.Length);
                            }

                            // Delete previous sub subBlock 14 and 15 data

                            rdxStream.Position = oldSubBlock14Position;
                            for (int b = 0; b < subBlock14Size + subBlock15Size; b++) rdxStream.WriteByte(0);
                        }
                        else if (textsStream.Length > subBlock14Size && textsStream.Length <= subBlock14Size + subBlock15Size)
                        {
                            // If the new textsBlock is bigger than subBlock 14 but fits
                            // if we move subBlock 15 to the end of the file... well, just move it.
                            // This condition should not happen if subBlock 15 doesn't exist,
                            // as its size would be 0 and the previous condition would be the one triggered.
                            // So no need to check if subBlock 15 exists.

                            // Move subBlock 15

                            byte[] subBlock15Data = new byte[subBlock15Size];
                            rdxStream.Position = subBlockPositions[15];
                            rdxStream.Read(subBlock15Data, 0, subBlock15Data.Length);

                            subBlockPositions[15] = Utils.Padding((uint)rdxStream.Length, 16);
                            rdxStream.Position = textDataBlockPosition + (4 * 15);
                            bw.WriteEndian(subBlockPositions[15], IsBigEndian);

                            rdxStream.Position = subBlockPositions[15];
                            rdxStream.Write(subBlock15Data, 0, subBlock15Data.Length);

                            // Copy texts data

                            rdxStream.Position = subBlockPositions[14];
                            textsStream.Position = 0;
                            textsStream.CopyTo(rdxStream);

                            // Fill remaining bytes with zeroes

                            int remainingBytes = (int)(unk1DataBlockPosition - rdxStream.Position);
                            for (int b = 0; b < remainingBytes; b++) rdxStream.WriteByte(0);
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

                // Move only block 15 (Works, but not much space available for expanding texts)
                //if (subBlockPositions[14] != 0)
                //{
                //    uint subBlock14Size = (subBlockPositions[15] == 0 ? unk1DataBlockPosition : subBlockPositions[15]) - subBlockPositions[14];

                //    string texts = File.ReadAllText(Path.Combine(inputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_PS2.GetLanguageCode(language))));

                //    using (MemoryStream textsStream = new MemoryStream())
                //    {
                //        Texts.Insert(texts, textsStream, table, IsBigEndian);

                //        if (textsStream.Length > subBlock14Size)
                //        {
                //            //// Delete previous sub block 14 data
                //            //rdxStream.Position = subBlockPositions[14];
                //            //for (int b = 0; b < subBlock14Size; b++) rdxStream.WriteByte(0);

                //            //// Set new sub block 14 position
                //            //subBlockPositions[14] = Utils.Padding((uint)rdxStream.Length, 16);
                //            //rdxStream.Position = textDataBlockPosition + (4 * 14);
                //            //bw.WriteEndian(subBlockPositions[14], IsBigEndian);

                //            //// Copy texts data
                //            //rdxStream.Position = subBlockPositions[14];
                //            //textsStream.Position = 0;
                //            //textsStream.CopyTo(rdxStream);

                //            // There's no subBlock 15, so there's no way the new textBlock will fit

                //            if (subBlockPositions[15] == 0)
                //            {
                //                throw new InvalidDataException($"Texts block size in \"{inputFolder}\" is {textsStream.Length} bytes, but can't be bigger than {subBlock14Size} bytes.");
                //            }

                //            // Check if the new textBlock would fit even if moving block 15

                //            uint subBlock15Size = unk1DataBlockPosition - subBlockPositions[15];

                //            if (textsStream.Length > subBlock14Size + subBlock15Size)
                //            {
                //                throw new InvalidDataException($"Texts block size in \"{inputFolder}\" is {textsStream.Length} bytes, but can't be bigger than {subBlock14Size + subBlock15Size} bytes.");
                //            }

                //            // Move block 15

                //            byte[] subBlock15Data = new byte[subBlock15Size];
                //            rdxStream.Position = subBlockPositions[15];
                //            rdxStream.Read(subBlock15Data, 0, subBlock15Data.Length);

                //            subBlockPositions[15] = Utils.Padding((uint)rdxStream.Length, 16);
                //            rdxStream.Position = textDataBlockPosition + (4 * 15);
                //            bw.WriteEndian(subBlockPositions[15], IsBigEndian);

                //            rdxStream.Position = subBlockPositions[15];
                //            rdxStream.Write(subBlock15Data, 0, subBlock15Data.Length);

                //            // Copy texts data

                //            rdxStream.Position = subBlockPositions[14];
                //            textsStream.Position = 0;
                //            textsStream.CopyTo(rdxStream);

                //            // Fill remaining bytes with zeroes

                //            int remainingBytes = (int)(unk1DataBlockPosition - rdxStream.Position);
                //            for (int b = 0; b < remainingBytes; b++) rdxStream.WriteByte(0);
                //        }
                //        else
                //        {
                //            // Copy texts data

                //            rdxStream.Position = subBlockPositions[14];
                //            textsStream.Position = 0;
                //            textsStream.CopyTo(rdxStream);

                //            // Fill remaining bytes with zeroes

                //            int remainingBytes = (int)(subBlockPositions[15] - rdxStream.Position);
                //            for (int b = 0; b < remainingBytes; b++) rdxStream.WriteByte(0);
                //        }
                //    }
                //}

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