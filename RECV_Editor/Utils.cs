using System;
using System.IO;

namespace RECV_Editor
{
    static class Utils
    {
        public static ushort SwapU16(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        public static uint SwapU32(uint value)
        {
            return (value << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | (value >> 24);
        }

        public static uint Padding(uint value, uint paddingSize)
        {
            uint p = value % paddingSize;
            return p == 0 ? value : value + (paddingSize - p);
        }

        public static void CopySliceTo(this Stream origin, Stream destination, int bytesCount)
        {
            byte[] buffer = new byte[65536];
            int count;

            while ((count = origin.Read(buffer, 0, Math.Min(buffer.Length, bytesCount))) != 0)
            {
                destination.Write(buffer, 0, count);
                bytesCount -= count;
            }
        }

        public static uint ReadUInt32Endian(this BinaryReader br, bool bigEndian)
        {
            if (bigEndian) return SwapU32(br.ReadUInt32());
            else return br.ReadUInt32();
        }

        public static ushort ReadUInt16Endian(this BinaryReader br, bool bigEndian)
        {
            if (bigEndian) return SwapU16(br.ReadUInt16());
            else return br.ReadUInt16();
        }

        public static void WriteEndian(this BinaryWriter bw, uint value, bool bigEndian)
        {
            if (bigEndian) bw.Write(SwapU32(value));
            else bw.Write(value);
        }

        public static void WriteEndian(this BinaryWriter bw, ushort value, bool bigEndian)
        {
            if (bigEndian) bw.Write(SwapU16(value));
            else bw.Write(value);
        }
    }
}