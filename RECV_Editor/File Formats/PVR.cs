using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    /// <summary>
    /// Unconventional PVR format that can contain extra headers and multiple actual PVR textures inside.
    /// </summary>
    class PVR
    {
        const uint GBIX_MAGIC = 0x58494247;
        const uint PVRT_MAGIC = 0x54525650;
        const uint TPVR_MAGIC = 0x52565054;
        const uint PPVP_MAGIC = 0x50565050;
        const uint PVPL_MAGIC = 0x4c505650;
        const uint PPVR_MAGIC = 0x52565050;
        const uint FINAL_CHUNK_MAGIC = 0xFFFFFFFF;

        const string GBIX_PVRT_NAME = "GBIX/PVRT";
        const string TPVR_NAME = "TPVR";
        const string PPVP_NAME = "PPVP";
        const string PVPL_NAME = "PVPL";
        const string PPVR_NAME = "PPVR";

        const uint GBIX_SIZE = 0x10;
        const uint TPVR_SIZE = 0x20;
        const uint PPVP_SIZE = 0x20;
        const uint PPVR_SIZE = 0x20;
        const uint FINAL_CHUNK_SIZE = 0x20;

        const string METADATA_FILENAME = "metadata.json";

        class PVR_Chunk
        {
            public string ChunkType { get; set; } = string.Empty;
            public string ChunkData { get; set; } = string.Empty;
        }

        class PVR_Entry
        {
            public uint EntrySize { get; set; } = 0;
            public List<PVR_Chunk> Chunks { get; set; } = new List<PVR_Chunk>();
        }

        class PVR_Metadata
        {
            public List<PVR_Entry> Entries { get; set; } = new List<PVR_Entry>();
            public string FinalChunk { get; set; } = string.Empty;
        }

        public static void Extract(Stream pvrStream, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            PVR_Metadata metadata = new PVR_Metadata();

            // Per entry data

            PVR_Entry entry = new PVR_Entry();
            uint currentTextureIndex = 0;
            string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
            long entryStartPosition = pvrStream.Position;
            uint paletteCount = 0;

            // Per PVR block data

            bool finalChunkFound = false;

            using (BinaryReader br = new BinaryReader(pvrStream, Encoding.UTF8, true))
            {
                while (!finalChunkFound)
                {
                    uint chunkMagic = br.ReadUInt32();
                    pvrStream.Position -= 4;

                    switch (chunkMagic)
                    {
                        case TPVR_MAGIC:
                        {
                            byte[] chunkData = new byte[TPVR_SIZE];
                            pvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new PVR_Chunk()
                            {
                                ChunkType = TPVR_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case PPVP_MAGIC:
                        {
                            byte[] chunkData = new byte[PPVP_SIZE];
                            pvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new PVR_Chunk()
                            {
                                ChunkType = PPVP_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case PVPL_MAGIC:
                        {
                            // Store external palette with the same name as the .pvr but with .pvp extension
                            // and removing the last 16 extra bytes. That's what Puyo Tools likes.

                            paletteCount++;
                            string paletteName;
                            if (paletteCount > 1) paletteName = paletteName = outputFileName + $"_{(paletteCount - 1):00}.pvp";
                            else paletteName = outputFileName + ".pvp";

                            pvrStream.Position += 4;
                            uint pvplSize = br.ReadUInt32();
                            pvrStream.Position -= 8;

                            using (FileStream fs = File.OpenWrite(paletteName))
                            {
                                pvrStream.CopySliceTo(fs, (int)(pvplSize + 0x8));
                            }

                            pvrStream.Position += 0x10; // There's always 16 extra bytes (always zeroes?)

                            entry.Chunks.Add(new PVR_Chunk()
                            {
                                ChunkType = PVPL_NAME,
                                ChunkData = Path.GetFileName(paletteName) // Store file name, as it will be extracted to a .pvp file
                            });

                            break;
                        }

                        case PPVR_MAGIC:
                        {
                            byte[] chunkData = new byte[PPVR_SIZE];
                            pvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new PVR_Chunk()
                            {
                                ChunkType = PPVR_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case GBIX_MAGIC:
                        {
                            string gbixName = outputFileName + ".pvr";

                            pvrStream.Position += GBIX_SIZE;
                            uint magic = br.ReadUInt32();

                            if (magic != PVRT_MAGIC)
                            {
                                throw new InvalidDataException($"Invalid PVRT data found in \"{outputFileName}\" at 0x{pvrStream.Position:X8}.");
                            }

                            // Size of the PVR data. Despite the GC being big endian, this data is always little endian.
                            // So we don't use br.ReadUInt32Endian()

                            uint pvrSize = br.ReadUInt32();

                            // Apparently size must be aligned to 0x20 bytes

                            pvrSize = Utils.Padding(pvrSize + 0x18, 0x20);

                            // Go back to the beginning of the GBIX chunk

                            pvrStream.Position -= 8 + GBIX_SIZE;

                            using (FileStream fs = File.OpenWrite(gbixName))
                            {
                                pvrStream.CopySliceTo(fs, (int)pvrSize);
                            }

                            entry.Chunks.Add(new PVR_Chunk()
                            {
                                ChunkType = GBIX_PVRT_NAME,
                                ChunkData = Path.GetFileName(gbixName) // Store file name, as it will be extracted to a .pvr file
                            });

                            // We've found all the chunks of this entry, so store the size

                            entry.EntrySize = (uint)(pvrStream.Position - entryStartPosition);
                            metadata.Entries.Add(entry);

                            // Reset per entry variables for next entry

                            entry = new PVR_Entry();
                            currentTextureIndex++;
                            outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
                            entryStartPosition = pvrStream.Position;
                            paletteCount = 0;

                            break;
                        }

                        case FINAL_CHUNK_MAGIC:
                        {
                            finalChunkFound = true;

                            if (entry.Chunks.Count > 0)
                            {
                                // If the chunks count is 0, it means that an entry with a GBIX has just been added previous to this.
                                // But if it is bigger than 0, it means that we've reached the end of a PVR block with a current entry
                                // that has some different chunks but no GBIX. We'll consider this as an "empty" texture.
                                // So finish this entry by storing the size and add it to metadata.

                                entry.EntrySize = (uint)(pvrStream.Position - entryStartPosition);
                                metadata.Entries.Add(entry);
                            }

                            byte[] finalChunkData = new byte[Math.Min(FINAL_CHUNK_SIZE, pvrStream.Length - pvrStream.Position)];
                            pvrStream.Read(finalChunkData, 0, finalChunkData.Length);
                            metadata.FinalChunk = Convert.ToBase64String(finalChunkData);

                            break;
                        }

                        default:
                        {
                            throw new InvalidDataException($"Invalid header data found in \"{outputFileName}\" at 0x{pvrStream.Position:X8}.");
                        }
                    }
                }
            }

            // Save metadata

            File.WriteAllText(Path.Combine(outputFolder, METADATA_FILENAME), JsonConvert.SerializeObject(metadata, Formatting.Indented));
        }

        public static void Insert(string inputFolder, Stream pvrStream)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (pvrStream == null)
            {
                throw new ArgumentNullException(nameof(pvrStream));
            }

            string metadataFileName = Path.Combine(inputFolder, METADATA_FILENAME);
            PVR_Metadata metadata = JsonConvert.DeserializeObject<PVR_Metadata>(File.ReadAllText(metadataFileName));

            using (BinaryWriter bw = new BinaryWriter(pvrStream, Encoding.UTF8, true))
            {
                for (int e = 0; e < metadata.Entries.Count; e++)
                {
                    PVR_Entry entry = metadata.Entries[e];

                    long entryStartPosition = pvrStream.Position;

                    for (int c = 0; c < entry.Chunks.Count; c++)
                    {
                        byte[] data;

                        switch (entry.Chunks[c].ChunkType)
                        {
                            case TPVR_NAME:
                            case PPVP_NAME:
                            case PPVR_NAME:
                            {
                                data = Convert.FromBase64String(entry.Chunks[c].ChunkData);

                                break;
                            }

                            case PVPL_NAME:
                            {
                                string pvplFileName = Path.Combine(inputFolder, entry.Chunks[c].ChunkData);
                                data = File.ReadAllBytes(pvplFileName);
                                Array.Resize(ref data, data.Length + 16);

                                break;
                            }

                            case GBIX_PVRT_NAME:
                            {
                                string gbixFileName = Path.Combine(inputFolder, entry.Chunks[c].ChunkData);
                                data = File.ReadAllBytes(gbixFileName);

                                break;
                            }

                            default:
                            {
                                throw new InvalidDataException($"Invalid header data found in \"{metadataFileName}\".");
                            }
                        }

                        pvrStream.Write(data, 0, data.Length);
                    }

                    uint entrySize = (uint)(pvrStream.Position - entryStartPosition);
                    if (entrySize != entry.EntrySize)
                    {
                        throw new InvalidDataException($"Entry with index {e} in \"{metadataFileName}\" is expected to be {entry.EntrySize} bytes but it is {entrySize} bytes.");
                    }
                }

                byte[] finalChunkData = Convert.FromBase64String(metadata.FinalChunk);
                pvrStream.Write(finalChunkData, 0, finalChunkData.Length);
            }
        }

        public static void ExtractSimplified(Stream pvrStream, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            uint currentTextureIndex = 0;
            string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
            uint paletteCount = 0;

            // Per PVR block data

            using (BinaryReader br = new BinaryReader(pvrStream, Encoding.UTF8, true))
            {
                uint chunkMagic = br.ReadUInt32();
                pvrStream.Position -= 4;

                switch (chunkMagic)
                {
                    case PVPL_MAGIC:
                    {
                        // Store external palette with the same name as the .pvr but with .pvp extension
                        // and removing the last 16 extra bytes. That's what Puyo Tools likes.

                        paletteCount++;
                        string paletteName;
                        if (paletteCount > 1) paletteName = paletteName = outputFileName + $"_{(paletteCount - 1):00}.pvp";
                        else paletteName = outputFileName + ".pvp";

                        pvrStream.Position += 4;
                        uint pvplSize = br.ReadUInt32();
                        pvrStream.Position -= 8;

                        using (FileStream fs = File.OpenWrite(paletteName))
                        {
                            pvrStream.CopySliceTo(fs, (int)(pvplSize + 0x8));
                        }

                        break;
                    }

                    case GBIX_MAGIC:
                    {
                        string gbixName = outputFileName + ".pvr";

                        pvrStream.Position += GBIX_SIZE;
                        uint magic = br.ReadUInt32();

                        if (magic != PVRT_MAGIC)
                        {
                            throw new InvalidDataException($"Invalid PVRT data found in \"{outputFileName}\" at 0x{pvrStream.Position:X8}.");
                        }

                        // Size of the PVR data. Despite the GC being big endian, this data is always little endian.
                        // So we don't use br.ReadUInt32Endian()

                        uint pvrSize = br.ReadUInt32();

                        // Go back to the beginning of the GBIX chunk

                        pvrStream.Position -= 8 + GBIX_SIZE;

                        using (FileStream fs = File.OpenWrite(gbixName))
                        {
                            pvrStream.CopySliceTo(fs, (int)(pvrSize + 0x18));
                        }

                        // Reset per entry variables for next entry

                        currentTextureIndex++;
                        outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
                        paletteCount = 0;

                        break;
                    }

                    default:
                    {
                        throw new InvalidDataException($"Invalid header data found in \"{outputFileName}\" at 0x{pvrStream.Position:X8}.");
                    }
                }
            }
        }

        public static void MassExtract(string inputFolder, string outputFolder, bool deleteOriginalFiles)
        {
            string[] fileNames = Directory.GetFiles(inputFolder);
            for (int f = 0; f < fileNames.Length; f++)
            {
                using (FileStream fs = File.OpenRead(fileNames[f]))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    for (; ; )
                    {
                        if (IsValid(fs))
                        {
                            Extract(fs, Path.Combine(outputFolder, $"{fileNames[f]}_{fs.Position:X8}"));
                        }
                        else
                        {
                            fs.Position += 0x10;
                        }

                        if (fs.Position >= fs.Length) break;
                    }
                }

                if (deleteOriginalFiles) File.Delete(fileNames[f]);
            }
        }

        public static void MassInsert(string inputFolder, string outputFolder)
        {
            string[] directories = Directory.GetDirectories(inputFolder);
            for (int d = 0; d < directories.Length; d++)
            {
                string directoryName = Path.GetFileName(directories[d]);
                string fileName = directoryName.Substring(0, directoryName.Length - 9);
                uint position = uint.Parse(directoryName.Substring(directoryName.Length - 8), System.Globalization.NumberStyles.HexNumber);

                string outputFileName = Path.Combine(outputFolder, fileName);
                if (!File.Exists(outputFileName))
                {
                    throw new FileNotFoundException($"File {outputFileName} has not been found.");
                }

                using (FileStream fs = File.OpenWrite(outputFileName))
                {
                    fs.Position = position;
                    Insert(directories[d], fs);
                }
            }
        }

        public static bool IsValid(Stream pvrStream)
        {
            using (BinaryReader br = new BinaryReader(pvrStream, Encoding.UTF8, true))
            {
                uint chunkMagic = br.ReadUInt32();
                pvrStream.Position -= 4;

                switch (chunkMagic)
                {
                    case GBIX_MAGIC:
                    case TPVR_MAGIC:
                    case PPVP_MAGIC:
                    case PVPL_MAGIC:
                    case PPVR_MAGIC:

                        return true;

                    default:

                        return false;
                }
            }
        }
    }
}