﻿using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class RDX_GameCube : RDX
    {
        protected override bool IsBigEndian => true;

        readonly string[] languageCodes = new string[] { "USA", "GER", "FRA", "ITA", "SPA", "JPN" };
        protected override string[] LanguageCodes => languageCodes;

        readonly uint[] languageSubBlockIndices = new uint[] { 14, 16, 17, 18, 19, 20 };
        protected override uint[] LanguageSubBlockIndices => languageSubBlockIndices;

        const uint MAGIC = 0x41A00000;

        const uint TEXT_DATA_BLOCK_SUBBLOCK_COUNT = 21;

        const string STRINGS_FILE_NAME = "Strings_{0}.txt";

        public override Results Extract(Stream rdxStream, string outputFolder, int language, Table table)
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
                    string textOutputFileName = Path.Combine(outputFolder, string.Format(STRINGS_FILE_NAME, LanguageCodes[rdxLanguageIndex]));
                    uint textPosition = subBlockPositions[languageSubBlockIndices[rdxLanguageIndex]];
                    ExtractTexts(rdxStream, textOutputFileName, table, textPosition);
                }

                // Read texture block data

                //rdxStream.Position = textureDataBlockPosition;

                //uint numberOfTextures = br.ReadUInt32Endian(IsBigEndian);

                //uint[] texturePositions = new uint[numberOfTextures];
                //for (int tp = 0; tp < numberOfTextures; tp++)
                //{
                //    texturePositions[tp] = br.ReadUInt32Endian(IsBigEndian);
                //}

                //for (int tp = 0; tp < numberOfTextures; tp++)
                //{
                //    rdxStream.Position = texturePositions[tp];

                //    uint textureSize;
                //    if (tp < numberOfTextures - 1) textureSize = texturePositions[tp + 1] - texturePositions[tp];
                //    else textureSize = (uint)rdxStream.Length - texturePositions[tp];

                //    //using (SubStream tm2Stream = new SubStream(rdxStream, 0, textureSize, true))
                //    //{
                //    TM2.Extract(rdxStream, Path.Combine(outputFolder, $"TIM2-{tp:0000}"));
                //    //}
                //}
            }

            return Results.Success;
        }

        public override void Insert(string inputFolder, Stream rdxStream, Table table)
        {
            throw new NotImplementedException();
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