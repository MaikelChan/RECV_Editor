using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    /// <summary>
    /// Unconventional GVR format that can contain extra headers and multiple actual GVR textures inside.
    /// </summary>
    class GVR
    {
        const uint GCIX_MAGIC = 0x58494347;
        const uint GVRT_MAGIC = 0x54525647;
        const uint TPVR_MAGIC = 0x52565054;
        const uint PPVP_MAGIC = 0x50565050;
        const uint GVPL_MAGIC = 0x4c505647;
        const uint PPVR_MAGIC = 0x52565050;
        const uint FINAL_CHUNK_MAGIC = 0xFFFFFFFF;

        const string GCIX_GVRT_NAME = "GCIX/GVRT";
        const string TPVR_NAME = "TPVR";
        const string PPVP_NAME = "PPVP";
        const string GVPL_NAME = "GVPL";
        const string PPVR_NAME = "PPVR";

        const uint GCIX_SIZE = 0x10;
        const uint TPVR_SIZE = 0x20;
        const uint PPVP_SIZE = 0x20;
        const uint PPVR_SIZE = 0x20;
        const uint FINAL_CHUNK_SIZE = 0x20;

        const string METADATA_FILENAME = "metadata.json";

        class GVR_Chunk
        {
            public string ChunkType { get; set; } = string.Empty;
            public string ChunkData { get; set; } = string.Empty;
        }

        class GVR_Entry
        {
            public uint EntrySize { get; set; } = 0;
            public List<GVR_Chunk> Chunks { get; set; } = new List<GVR_Chunk>();
        }

        class GVR_Metadata
        {
            public List<GVR_Entry> Entries { get; set; } = new List<GVR_Entry>();
            public string FinalChunk { get; set; } = string.Empty;
        }

        public static void Extract(Stream gvrStream, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            GVR_Metadata metadata = new GVR_Metadata();

            // Per entry data

            GVR_Entry entry = new GVR_Entry();
            uint currentTextureIndex = 0;
            string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
            long entryStartPosition = gvrStream.Position;
            uint paletteCount = 0;

            // Per GVR block data

            bool finalChunkFound = false;

            using (BinaryReader br = new BinaryReader(gvrStream, Encoding.UTF8, true))
            {
                while (!finalChunkFound)
                {
                    uint chunkMagic = br.ReadUInt32();
                    gvrStream.Position -= 4;

                    switch (chunkMagic)
                    {
                        case TPVR_MAGIC:
                        {
                            byte[] chunkData = new byte[TPVR_SIZE];
                            gvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new GVR_Chunk()
                            {
                                ChunkType = TPVR_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case PPVP_MAGIC:
                        {
                            byte[] chunkData = new byte[PPVP_SIZE];
                            gvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new GVR_Chunk()
                            {
                                ChunkType = PPVP_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case GVPL_MAGIC:
                        {
                            // Store external palette with the same name as the .gvr but with .gvp extension
                            // and removing the last 16 extra bytes. That's what Puyo Tools likes.

                            paletteCount++;
                            string paletteName;
                            if (paletteCount > 1) paletteName = paletteName = outputFileName + $"_{(paletteCount - 1):00}.gvp";
                            else paletteName = outputFileName + ".gvp";

                            gvrStream.Position += 4;
                            uint gvplSize = br.ReadUInt32();
                            gvrStream.Position -= 8;

                            using (FileStream fs = File.OpenWrite(paletteName))
                            {
                                gvrStream.CopySliceTo(fs, (int)(gvplSize + 0x8));
                            }

                            gvrStream.Position += 0x10; // There's always 16 extra bytes (always zeroes?)

                            entry.Chunks.Add(new GVR_Chunk()
                            {
                                ChunkType = GVPL_NAME,
                                ChunkData = Path.GetFileName(paletteName) // Store file name, as it will be extracted to a .gvp file
                            });

                            break;
                        }

                        case PPVR_MAGIC:
                        {
                            byte[] chunkData = new byte[PPVR_SIZE];
                            gvrStream.Read(chunkData, 0, chunkData.Length);

                            entry.Chunks.Add(new GVR_Chunk()
                            {
                                ChunkType = PPVR_NAME,
                                ChunkData = Convert.ToBase64String(chunkData)
                            });

                            break;
                        }

                        case GCIX_MAGIC:
                        {
                            string gcixName = outputFileName + ".gvr";

                            gvrStream.Position += GCIX_SIZE;
                            uint magic = br.ReadUInt32();

                            if (magic != GVRT_MAGIC)
                            {
                                throw new InvalidDataException($"Invalid GVRT data found in \"{outputFileName}\" at 0x{gvrStream.Position:X8}.");
                            }

                            // Size of the GVR data. Despite the GC being big endian, this data is always little endian.
                            // So we don't use br.ReadUInt32Endian()

                            uint gvrSize = br.ReadUInt32();

                            // Go back to the beginning of the GCIX chunk

                            gvrStream.Position -= 8 + GCIX_SIZE;

                            using (FileStream fs = File.OpenWrite(gcixName))
                            {
                                gvrStream.CopySliceTo(fs, (int)(gvrSize + 0x18));
                            }

                            entry.Chunks.Add(new GVR_Chunk()
                            {
                                ChunkType = GCIX_GVRT_NAME,
                                ChunkData = Path.GetFileName(gcixName) // Store file name, as it will be extracted to a .gvr file
                            });

                            // We've found all the chunks of this entry, so store the size

                            entry.EntrySize = (uint)(gvrStream.Position - entryStartPosition);
                            metadata.Entries.Add(entry);

                            // Reset per entry variables for next entry

                            entry = new GVR_Entry();
                            currentTextureIndex++;
                            outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");
                            entryStartPosition = gvrStream.Position;
                            paletteCount = 0;

                            break;
                        }

                        case FINAL_CHUNK_MAGIC:
                        {
                            finalChunkFound = true;

                            if (entry.Chunks.Count > 0)
                            {
                                // If the chunks count is 0, it means that an entry with a GCIX has just been added previous to this.
                                // But if it is bigger than 0, it means that we've reached the end of a GVR block with a current entry
                                // that has some different chunks but no GCIX. We'll consider this as an "empty" texture.
                                // So finish this entry by storing the size and add it to metadata.

                                entry.EntrySize = (uint)(gvrStream.Position - entryStartPosition);
                                metadata.Entries.Add(entry);
                            }

                            byte[] finalChunkData = new byte[Math.Min(FINAL_CHUNK_SIZE, gvrStream.Length - gvrStream.Position)];
                            gvrStream.Read(finalChunkData, 0, finalChunkData.Length);
                            metadata.FinalChunk = Convert.ToBase64String(finalChunkData);

                            break;
                        }

                        default:
                        {
                            throw new InvalidDataException($"Invalid header data found in \"{outputFileName}\" at 0x{gvrStream.Position:X8}.");
                        }
                    }
                }
            }

            // Save metadata

            File.WriteAllText(Path.Combine(outputFolder, METADATA_FILENAME), JsonConvert.SerializeObject(metadata, Formatting.Indented));
        }

        public static void Insert(string inputFolder, Stream gvrStream)
        {
            if (string.IsNullOrEmpty(inputFolder))
            {
                throw new ArgumentNullException(nameof(inputFolder));
            }

            if (!Directory.Exists(inputFolder))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputFolder}\" does not exist!");
            }

            if (gvrStream == null)
            {
                throw new ArgumentNullException(nameof(gvrStream));
            }

            string metadataFileName = Path.Combine(inputFolder, METADATA_FILENAME);
            GVR_Metadata metadata = JsonConvert.DeserializeObject<GVR_Metadata>(File.ReadAllText(metadataFileName));

            using (BinaryWriter bw = new BinaryWriter(gvrStream, Encoding.UTF8, true))
            {
                for (int e = 0; e < metadata.Entries.Count; e++)
                {
                    GVR_Entry entry = metadata.Entries[e];

                    long entryStartPosition = gvrStream.Position;

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

                            case GVPL_NAME:
                            {
                                string gvplFileName = Path.Combine(inputFolder, entry.Chunks[c].ChunkData);
                                data = File.ReadAllBytes(gvplFileName);
                                Array.Resize(ref data, data.Length + 16);

                                break;
                            }

                            case GCIX_GVRT_NAME:
                            {
                                string gcixFileName = Path.Combine(inputFolder, entry.Chunks[c].ChunkData);
                                data = File.ReadAllBytes(gcixFileName);

                                break;
                            }

                            default:
                            {
                                throw new InvalidDataException($"Invalid header data found in \"{metadataFileName}\".");
                            }
                        }

                        gvrStream.Write(data, 0, data.Length);
                    }

                    uint entrySize = (uint)(gvrStream.Position - entryStartPosition);
                    if (entrySize != entry.EntrySize)
                    {
                        throw new InvalidDataException($"Entry with index {e} in \"{metadataFileName}\" is expected to be {entry.EntrySize} bytes but it is {entrySize} bytes.");
                    }
                }

                byte[] finalChunkData = Convert.FromBase64String(metadata.FinalChunk);
                gvrStream.Write(finalChunkData, 0, finalChunkData.Length);
            }
        }

        public static void MassExtract(string inputFolder, bool deleteOriginalFiles)
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
                            Extract(fs, Path.Combine(inputFolder, $"{fileNames[f]}_{fs.Position:X8}"));
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

        public static bool IsValid(Stream gvrStream)
        {
            using (BinaryReader br = new BinaryReader(gvrStream, Encoding.UTF8, true))
            {
                uint chunkMagic = br.ReadUInt32();
                gvrStream.Position -= 4;

                switch (chunkMagic)
                {
                    case GCIX_MAGIC:
                    case TPVR_MAGIC:
                    case PPVP_MAGIC:
                    case GVPL_MAGIC:
                    case PPVR_MAGIC:

                        return true;

                    default:

                        return false;
                }
            }
        }
    }
}