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
    }
}