using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RECV_Editor.File_Formats
{
    class Table
    {
        public const ushort TIME_CODE = 0xFF02;
        public const ushort ITEM_CODE = 0xFF03;

        readonly Dictionary<ushort, string> hexToString = null;
        readonly Dictionary<string, ushort> stringToHex = null;

        public Table(string tableFile)
        {
            if (!File.Exists(tableFile))
            {
                throw new FileNotFoundException($"Table file \"{tableFile}\" does not exist!", tableFile);
            }

            hexToString = new Dictionary<ushort, string>();
            stringToHex = new Dictionary<string, ushort>();

            string[] lines = File.ReadAllLines(tableFile);

            foreach (string line in lines)
            {
                if (line.Length == 0) continue;

                string[] parts = line.Split('=');

                if (parts.Length == 2)
                {
                    // Normal line with the format XXXX=X
                    ushort hex = Utils.SwapU16(Convert.ToUInt16(parts[0], 16));
                    parts[1] = ProcessValue(parts[1]);
                    hexToString.Add(hex, parts[1]);
                    stringToHex.Add(parts[1], hex);
                }
                else if (parts.Length == 3)
                {
                    // Special case for "=" (XXXX==)
                    ushort hex = Utils.SwapU16(Convert.ToUInt16(parts[0], 16));
                    hexToString.Add(hex, "=");
                    stringToHex.Add("=", hex);
                }
                else
                {
                    // Ignore invalid lines
                    continue;
                }
            }
        }

        public string HexToString(ushort hex)
        {
            bool found = hexToString.TryGetValue(hex, out string str);

            if (!found)
            {
                str = $"[HEX:{hex:X}]";
                Logger.Append($"Hex code 0x{hex:X} not found in table.", Logger.LogTypes.Warning);
            }

            return str;
        }

        public ushort StringToHex(string str)
        {
            bool found = stringToHex.TryGetValue(str, out ushort hex);

            if (!found && str.StartsWith("["))
            {
                throw new Exception($"Code {str} has not found in table.");
            }

            return hex;
        }

        public string GetString(ushort[] hexData)
        {
            StringBuilder sb = new StringBuilder();

            for (int h = 0; h < hexData.Length; h++)
            {
                // Check if it's a special code
                if (hexData[h] == TIME_CODE)
                {
                    if (hexData[h + 1] == 0x0000) sb.Append("[PAGE]\n");
                    else sb.Append($"[TIME:{hexData[h + 1]:X}]");
                    h++;
                }
                else if (hexData[h] == ITEM_CODE)
                {
                    sb.Append($"[ITEM:{hexData[h + 1]:X}]");
                    h++;
                }
                else
                {
                    // It's a normal code
                    sb.Append(HexToString(hexData[h]));
                }
            }

            return sb.ToString();
        }

        string ProcessValue(string value)
        {
            switch (value)
            {
                case @"\n": return "\n";
                default: return value;
            }
        }
    }
}