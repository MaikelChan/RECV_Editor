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
    }
}