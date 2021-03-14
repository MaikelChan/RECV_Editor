using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    // Unconventional TIM2 format that can contain multiple actual TIM2 textures inside 

    class TM2
    {
        const uint TIM2_MAGIC = 0x324d4954; // TIM2
        const uint PLI_MAGIC = 0x00494C50; // PLI

        const uint PLI_HEADER_SIZE = 32;

        public static void Extract(Stream tm2Stream, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (BinaryReader br = new BinaryReader(tm2Stream, Encoding.UTF8, true))
            {
                uint currentTextureIndex = 0;

                for (; ; )
                {
                    string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");

                    uint magic = br.ReadUInt32();

                    // Check if there's a PLI section before the TM2 data
                    if (magic == PLI_MAGIC)
                    {
                        uint pliSize = br.ReadUInt32();
                        tm2Stream.Position -= 8;

                        using (FileStream fs = File.OpenWrite(outputFileName + ".PLI"))
                        {
                            tm2Stream.CopySliceTo(fs, (int)(PLI_HEADER_SIZE + pliSize));
                        }

                        magic = br.ReadUInt32();
                    }

                    // There is one case with a null texture, located in RDX #87
                    if (magic == 0xFFFFFFFF)
                    {
                        tm2Stream.Position -= 4;

                        using (FileStream fs = File.OpenWrite(outputFileName + ".TM2"))
                        {
                            tm2Stream.CopySliceTo(fs, 0x20); // TODO: Is it always 0x20?
                        }

                        currentTextureIndex++;

                        continue;
                    }

                    // Here should be TM2 data, check if that's the case
                    if (magic != TIM2_MAGIC)
                    {
                        throw new InvalidDataException($"Invalid TIM2 data found in \"{outputFileName}\".");
                    }

                    uint size = br.ReadUInt32();
                    tm2Stream.Position += 24;

                    using (FileStream fs = File.OpenWrite(outputFileName + ".TM2"))
                    {
                        tm2Stream.CopySliceTo(fs, (int)size);
                    }

                    // Check if there are more TIM2 files
                    if (tm2Stream.Position >= tm2Stream.Length) break;
                    uint hex = br.ReadUInt32();
                    if (hex == 0xFFFFFFFF) break;

                    tm2Stream.Position -= 4;
                    currentTextureIndex++;
                }
            }
        }

        public static void Insert(string inputFolder, Stream rdxStream)
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

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            {
                uint currentTextureIndex = 0;

                for (; ; )
                {
                    string inputFileName = Path.Combine(inputFolder, $"{currentTextureIndex:0000}");

                    uint magic = br.ReadUInt32();

                    // Check if there's a PLI section before the TM2 data
                    if (magic == PLI_MAGIC)
                    {
                        uint pliSize = br.ReadUInt32() + PLI_HEADER_SIZE;
                        rdxStream.Position -= 8;

                        if (!File.Exists(inputFileName + ".PLI"))
                        {
                            throw new FileNotFoundException($"Required file \"{inputFileName + ".PLI"}\" not found.");
                        }

                        using (FileStream fs = File.OpenRead(inputFileName + ".PLI"))
                        {
                            if (pliSize != fs.Length)
                            {
                                throw new InvalidDataException($"File \"{inputFileName + ".PLI"}\" is {fs.Length} bytes but is expected to be {pliSize} bytes.");
                            }

                            fs.CopyTo(rdxStream);
                        }

                        magic = br.ReadUInt32();
                    }

                    // There is one case with a null texture, located in RDX #87
                    if (magic == 0xFFFFFFFF)
                    {
                        rdxStream.Position -= 4;

                        if (!File.Exists(inputFileName + ".TM2"))
                        {
                            throw new FileNotFoundException($"Required file \"{inputFileName + ".TM2"}\" not found.");
                        }

                        using (FileStream fs = File.OpenRead(inputFileName + ".TM2"))
                        {
                            fs.CopyTo(rdxStream);
                        }

                        currentTextureIndex++;

                        continue;
                    }

                    // Here should be TM2 data, check if that's the case
                    if (magic != TIM2_MAGIC)
                    {
                        throw new InvalidDataException($"Invalid TIM2 data found in \"{inputFileName}\".");
                    }

                    uint size = br.ReadUInt32();
                    rdxStream.Position += 24;

                    if (!File.Exists(inputFileName + ".TM2"))
                    {
                        throw new FileNotFoundException($"Required file \"{inputFileName + ".TM2"}\" not found.");
                    }

                    using (FileStream fs = File.OpenRead(inputFileName + ".TM2"))
                    {
                        if (size != fs.Length)
                        {
                            throw new InvalidDataException($"File \"{inputFileName + ".TM2"}\" is {fs.Length} bytes but is expected to be {size} bytes.");
                        }

                        fs.CopyTo(rdxStream);
                    }

                    // Check if there are more TIM2 files
                    if (rdxStream.Position >= rdxStream.Length) break;
                    uint hex = br.ReadUInt32();
                    if (hex == 0xFFFFFFFF) break;

                    rdxStream.Position -= 4;
                    currentTextureIndex++;
                }
            }
        }
    }
}