using System;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class ALD
    {
        public delegate void DataBlockFoundDelegate(SubStream blockStream, uint blockIndex, uint blockSize);
        public delegate (byte[], bool) DataBlockInsertDelegate(uint blockIndex);

        const ushort FILE_END_PADDING = 0xFFFF;

        public static void Extract(string inputFile, bool bigEndian, DataBlockFoundDelegate dataBlockFoundCallback)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"File \"{inputFile}\" does not exist!", inputFile);
            }

            using (FileStream fs = File.OpenRead(inputFile))
            {
                Extract(fs, bigEndian, dataBlockFoundCallback);
            }
        }

        public static void Extract(Stream aldStream, bool bigEndian, DataBlockFoundDelegate dataBlockFoundCallback)
        {
            if (aldStream == null)
            {
                throw new ArgumentNullException(nameof(aldStream));
            }

            if (dataBlockFoundCallback == null)
            {
                throw new ArgumentNullException(nameof(dataBlockFoundCallback));
            }

            using (BinaryReader br = new BinaryReader(aldStream, Encoding.UTF8, true))
            {
                uint currentBlockIndex = 0;

                // Read each block
                for (; ; )
                {
                    uint blockSize = br.ReadUInt32Endian(bigEndian);
                    if ((blockSize & 0x80000000) != 0) aldStream.Position += 4; // TODO: Is this always the case?
                    blockSize &= 0x7fffffff;

                    long blockStartPosition = aldStream.Position;
                    long blockEndPosition = blockStartPosition + blockSize;

                    using (SubStream blockStream = new SubStream(aldStream, 0, blockSize, true))
                    {
                        dataBlockFoundCallback(blockStream, currentBlockIndex, blockSize);
                    }

                    // Check if there are other blocks after this one
                    if (blockEndPosition >= aldStream.Length) break;
                    aldStream.Position = blockEndPosition;
                    ushort value = br.ReadUInt16Endian(bigEndian);
                    if (value == FILE_END_PADDING) break;

                    aldStream.Position -= 2;
                    currentBlockIndex++;
                }
            }
        }

        public static void Insert(string outputAld, bool bigEndian, uint blockCount, DataBlockInsertDelegate dataBlockInsertCallback)
        {
            if (string.IsNullOrEmpty(outputAld))
            {
                throw new ArgumentNullException(nameof(outputAld));
            }

            using (FileStream aldStream = File.Create(outputAld))
            {
                Insert(aldStream, bigEndian, blockCount, dataBlockInsertCallback);
            }
        }

        public static void Insert(Stream aldStream, bool bigEndian, uint blockCount, DataBlockInsertDelegate dataBlockInsertCallback)
        {
            if (aldStream == null)
            {
                throw new ArgumentNullException(nameof(aldStream));
            }

            if (dataBlockInsertCallback == null)
            {
                throw new ArgumentNullException(nameof(dataBlockInsertCallback));
            }

            using (BinaryWriter bw = new BinaryWriter(aldStream, Encoding.UTF8, true))
            {
                for (uint b = 0; b < blockCount; b++)
                {
                    (byte[] blockData, bool isGvr) = dataBlockInsertCallback(b);

                    if (isGvr)
                    {
                        bw.WriteEndian((uint)(0x80000000 | blockData.Length), bigEndian); // TODO: Is this always the case?
                        aldStream.Position += 4;
                    }
                    else
                    {
                        bw.WriteEndian((uint)blockData.Length, bigEndian);
                    }

                    aldStream.Write(blockData, 0, blockData.Length);
                }

                bw.WriteEndian(0xFFFFFFFF, bigEndian);
            }
        }
    }
}