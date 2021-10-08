using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX_GameCube : RDX
    {
        protected override bool IsBigEndian => true;

        readonly uint[] languageSubBlockIndices = new uint[] { 14, 16, 17, 18, 19, 20 };
        protected override uint[] LanguageSubBlockIndices => languageSubBlockIndices;

        const uint MAGIC = 0x41A00000;

        const uint TEXT_DATA_BLOCK_SUBBLOCK_COUNT = 21;

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

                if (magic != MAGIC)
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

                // Extract the texts in the subBlock corresponding to the selected language

                int rdxLanguageIndex = GetRDXLanguageIndex(language);
                if (rdxLanguageIndex >= 0)
                {
                    string textOutputFileName = Path.Combine(outputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_GameCube.GetLanguageCode(language)));
                    uint textPosition = subBlockPositions[languageSubBlockIndices[rdxLanguageIndex]];
                    ExtractTexts(rdxStream, textOutputFileName, table, textPosition);
                }

                // Read texture block data

                // First of all, blocks do not appear to be consecutive.
                // In some files, this is the order: 0, 1, 4, 2, 3
                // Is it always like this? Let's do some checks just in case.

                if (textureDataBlockPosition <= unk1DataBlockPosition ||
                    textureDataBlockPosition >= unk2DataBlockPosition)
                {
                    throw new InvalidDataException($"In file \"{rdxFileName}\", the texture block is in an unexpected position: 0x{textureDataBlockPosition:X8}");
                }

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
                    GVR.Extract(rdxStream, Path.Combine(outputFolder, $"GVR-{tp:0000}"));
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

            // Gather data we need to preserve from the original RDX

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            using (BinaryWriter bw = new BinaryWriter(rdxStream, Encoding.UTF8, true))
            {
                uint magic = br.ReadUInt32Endian(IsBigEndian);
                if (magic != MAGIC)
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

                // Get the texts subBlock corresponding to the selected language

                int rdxLanguageIndex = GetRDXLanguageIndex(language);
                uint textSubBlockPosition = subBlockPositions[languageSubBlockIndices[rdxLanguageIndex]];

                // Check if the texts subBlock is null, and do stuff if not null

                if (textSubBlockPosition != 0)
                {
                    // Find the next non-null subBlock to calculate the texts subBlock size

                    uint nextSubBlockPosition = unk1DataBlockPosition;

                    for (uint sb = 0; sb < TEXT_DATA_BLOCK_SUBBLOCK_COUNT; sb++)
                    {
                        if (subBlockPositions[sb] == 0) continue;
                        if (subBlockPositions[sb] <= textSubBlockPosition) continue;
                        if (subBlockPositions[sb] < nextSubBlockPosition) nextSubBlockPosition = subBlockPositions[sb];
                    }

                    uint textsSubBlockSize = nextSubBlockPosition - textSubBlockPosition;

                    string texts = File.ReadAllText(Path.Combine(inputFolder, string.Format(STRINGS_FILE_NAME, rdxFileName, RECV_GameCube.GetLanguageCode(language))));

                    using (MemoryStream textsStream = new MemoryStream())
                    {
                        Texts.Insert(texts, textsStream, table, IsBigEndian);

                        if (textsStream.Length > textsSubBlockSize)
                        {
                            // If the new textsBlock is bigger than the old one,
                            // place it just where the textureDataBlock is and move the textureDataBlock after the texts.
                            // The reasons for this are explained in RDX_PS2. I don't know if the same applies to GameCube,
                            // but let's be safe, and also keep the code consistent.


                            // TODO: Redo this -----------------------------------------------------------------------------

                            // Delete previous sub subBlock 14 and 15 data

                            rdxStream.Position = subBlockPositions[14];
                            for (int b = 0; b < textsSubBlockSize; b++) rdxStream.WriteByte(0);

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
                            // If the new textsBlock fits inside the old one, just overwrite.

                            // Copy texts data

                            rdxStream.Position = textSubBlockPosition;
                            textsStream.Position = 0;
                            textsStream.CopyTo(rdxStream);

                            // Fill remaining bytes with zeroes

                            int remainingBytes = (int)(nextSubBlockPosition - rdxStream.Position);
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
                    string GVRFolder = Path.Combine(inputFolder, $"GVR-{tp:0000}");
                    if (!Directory.Exists(GVRFolder)) continue;

                    rdxStream.Position = texturePositions[tp];

                    //uint textureSize;
                    //if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                    //else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                    GVR.Insert(GVRFolder, rdxStream);
                }
            }
        }

        int GetRDXLanguageIndex(int language)
        {
            switch (language)
            {
                case 0: return 5;  // Japanese
                case 1: return 0;  // American English
                case 2: return -1; // British English (There's only american english in RDX files)
                case 3: return 1;  // German
                case 4: return 2;  // French
                case 5: return 4;  // Spanish
                case 6: return 3;  // Italian
                default: throw new IndexOutOfRangeException($"Language with index {language} is not implemented.");
            }
        }
    }
}