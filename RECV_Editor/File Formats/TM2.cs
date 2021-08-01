using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    /// <summary>
    /// Unconventional TIM2 format that can contain multiple actual TIM2 textures inside.
    /// </summary>
    class TM2
    {
        const uint TIM2_MAGIC = 0x324d4954; // TIM2
        const uint PLI_MAGIC = 0x00494C50; // PLI

        const uint PLI_HEADER_SIZE = 32;

        const string METADATA_FILENAME = "metadata.json";

        class TM2_Entry
        {
            public uint EntrySize { get; set; } = 0;

            public bool HasPLIData { get; set; } = false;
            public string PLIData { get; set; } = string.Empty;

            public bool HasTM2Data { get; set; } = false;
            public uint TM2Size { get; set; } = 0;
            public byte TM2Format { get; set; } = 0;
        }

        class TM2_Metadata
        {
            public List<TM2_Entry> Entries { get; set; } = new List<TM2_Entry>();
        }

        public static void Extract(Stream tm2Stream, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            TM2_Metadata metadata = new TM2_Metadata();

            using (BinaryReader br = new BinaryReader(tm2Stream, Encoding.UTF8, true))
            {
                uint currentTextureIndex = 0;

                for (; ; )
                {
                    string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");

                    TM2_Entry entry = new TM2_Entry();

                    long entryStartPosition = tm2Stream.Position;
                    uint magic = br.ReadUInt32();

                    // Check if there's a PLI section before the TM2 data
                    if (magic == PLI_MAGIC)
                    {
                        uint pliSize = br.ReadUInt32();
                        tm2Stream.Position += 0x18;

                        byte[] pliData = new byte[pliSize];
                        tm2Stream.Read(pliData, 0, pliData.Length);

                        entry.HasPLIData = true;
                        entry.PLIData = Convert.ToBase64String(pliData);

                        magic = br.ReadUInt32();
                    }

                    // There is one case with a null texture, located in RDX #87
                    if (magic == 0xFFFFFFFF)
                    {
                        tm2Stream.Position += 0x1C;

                        currentTextureIndex++;

                        // Add metadata entry

                        entry.EntrySize = (uint)(tm2Stream.Position - entryStartPosition);
                        metadata.Entries.Add(entry);

                        continue;
                    }

                    // Here should be TM2 data, check if that's the case
                    if (magic != TIM2_MAGIC)
                    {
                        throw new InvalidDataException($"Invalid TIM2 data found in \"{outputFileName}\".");
                    }

                    uint size = br.ReadUInt32();
                    entry.HasTM2Data = true;
                    entry.TM2Size = size;

                    tm2Stream.Position += 0x18;

                    using (FileStream fs = new FileStream(outputFileName + ".TM2", FileMode.Create, FileAccess.ReadWrite))
                    {
                        tm2Stream.CopySliceTo(fs, (int)size);

                        // Store the format value located at 0x90 in the metadata and set it to 0.
                        // This makes the TIM2 work in OptPix.

                        fs.Position = 0x90;
                        entry.TM2Format = (byte)fs.ReadByte();

                        fs.Position = 0x90;
                        fs.WriteByte(0);
                    }

                    // Add metadata entry

                    entry.EntrySize = (uint)(tm2Stream.Position - entryStartPosition);
                    metadata.Entries.Add(entry);

                    // Check if there are more TIM2 files
                    if (tm2Stream.Position >= tm2Stream.Length) break;
                    uint hex = br.ReadUInt32();
                    if (hex == 0xFFFFFFFF) break;

                    tm2Stream.Position -= 4;
                    currentTextureIndex++;
                }
            }

            // Save metadata

            File.WriteAllText(Path.Combine(outputFolder, METADATA_FILENAME), JsonConvert.SerializeObject(metadata, Formatting.Indented));
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

            TM2_Metadata metadata = JsonConvert.DeserializeObject<TM2_Metadata>(File.ReadAllText(Path.Combine(inputFolder, METADATA_FILENAME)));

            using (BinaryReader br = new BinaryReader(rdxStream, Encoding.UTF8, true))
            using (BinaryWriter bw = new BinaryWriter(rdxStream, Encoding.UTF8, true))
            {
                for (int e = 0; e < metadata.Entries.Count; e++)
                {
                    TM2_Entry entry = metadata.Entries[e];

                    string inputFileName = Path.Combine(inputFolder, $"{e:0000}.TM2");

                    if (!File.Exists(inputFileName) || !entry.HasTM2Data)
                    {
                        rdxStream.Position += entry.EntrySize;
                        continue;
                    }

                    if (entry.HasPLIData)
                    {
                        byte[] pliData = Convert.FromBase64String(entry.PLIData);

                        bw.Write(PLI_MAGIC);
                        bw.Write(pliData.Length);
                        for (int i = 0; i < 0x18 >> 1; i++) bw.Write((ushort)0x4443);
                        rdxStream.Write(pliData, 0, pliData.Length);
                    }

                    byte[] tm2Data = File.ReadAllBytes(inputFileName);

                    rdxStream.Position += 0x4;
                    uint size = br.ReadUInt32();

                    if (size != tm2Data.Length)
                    {
                        throw new InvalidDataException($"Invalid TIM2 data found in \"{inputFileName}\". The size of the file is {tm2Data.Length} bytes, but {size} bytes are expected");
                    }

                    // Revert the format value that was set to 0 for compatibility reasons
                    tm2Data[0x90] = entry.TM2Format;

                    rdxStream.Position += 0x18;
                    rdxStream.Write(tm2Data, 0, tm2Data.Length);
                }
            }
        }
    }
}