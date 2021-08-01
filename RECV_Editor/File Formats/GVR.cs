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
            public bool HasEntryData = false;
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

            using (BinaryReader br = new BinaryReader(gvrStream, Encoding.UTF8, true))
            {
                uint currentTextureIndex = 0;

                for (; ; )
                {
                    string outputFileName = Path.Combine(outputFolder, $"{currentTextureIndex:0000}");

                    GVR_Entry entry = new GVR_Entry();

                    long entryStartPosition = gvrStream.Position;
                    uint paletteCount = 0;
                    bool gcixFound = false;
                    bool finalChunkFound = false;

                    while (!gcixFound && !finalChunkFound)
                    {
                        uint chunkMagic = br.ReadUInt32();
                        gvrStream.Position -= 4;

                        switch (chunkMagic)
                        {
                            case GCIX_MAGIC:
                            {
                                gcixFound = true;
                                break;
                            }

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
                                paletteCount++;
                                string paletteName = outputFileName + ".gvp";
                                if (paletteCount > 1) paletteName += (paletteCount - 1).ToString();

                                gvrStream.Position += 4;
                                uint gvplSize = br.ReadUInt32();
                                gvrStream.Position -= 8;

                                byte[] chunkData = new byte[gvplSize + 0x8];
                                gvrStream.Read(chunkData, 0, chunkData.Length);
                                gvrStream.Position += 0x10; // There's always 16 extra bytes (always zeroes?)

                                entry.Chunks.Add(new GVR_Chunk()
                                {
                                    ChunkType = GVPL_NAME,
                                    ChunkData = Path.GetFileName(paletteName) // Store file name, as it will be extracted to a .gvp file
                                });

                                File.WriteAllBytes(paletteName, chunkData);

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

                            case FINAL_CHUNK_MAGIC:
                            {
                                finalChunkFound = true;
                                break;
                            }

                            default:
                            {
                                throw new InvalidDataException($"Invalid header data found in \"{outputFileName}\" at 0x{gvrStream.Position:X8}.");
                            }
                        }
                    }

                    if (finalChunkFound)
                    {
                        // We have found a final chunk prematurely.
                        // That means that there's no texture data.

                        entry.HasEntryData = false;
                        entry.EntrySize = (uint)(gvrStream.Position - entryStartPosition);
                        metadata.Entries.Add(entry);

                        byte[] finalChunkData = new byte[FINAL_CHUNK_SIZE];
                        gvrStream.Read(finalChunkData, 0, finalChunkData.Length);
                        metadata.FinalChunk = Convert.ToBase64String(finalChunkData);

                        break;
                    }

                    // We've found the GCIX header

                    entry.HasEntryData = true;

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

                    using (FileStream fs = new FileStream(outputFileName + ".gvr", FileMode.Create, FileAccess.ReadWrite))
                    {
                        gvrStream.CopySliceTo(fs, (int)(gvrSize + 0x18));
                    }

                    // Add metadata entry

                    entry.EntrySize = (uint)(gvrStream.Position - entryStartPosition);
                    metadata.Entries.Add(entry);

                    // Check if there are more GVR files

                    if (gvrStream.Position >= gvrStream.Length)
                    {
                        throw new InvalidDataException($"Expected final data chunk in \"{outputFileName}\" at 0x{gvrStream.Position:X8}.");
                    }

                    uint hex = br.ReadUInt32();

                    if (hex == FINAL_CHUNK_MAGIC)
                    {
                        gvrStream.Position -= 4;
                        byte[] finalChunkData = new byte[FINAL_CHUNK_SIZE];
                        gvrStream.Read(finalChunkData, 0, finalChunkData.Length);
                        metadata.FinalChunk = Convert.ToBase64String(finalChunkData);

                        break;
                    }

                    gvrStream.Position -= 4;
                    currentTextureIndex++;
                }
            }

            // Save metadata

            File.WriteAllText(Path.Combine(outputFolder, METADATA_FILENAME), JsonConvert.SerializeObject(metadata, Formatting.Indented));
        }
    }
}