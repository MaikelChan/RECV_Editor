using System;
using System.IO;
using System.Text;

namespace RECV_Editor
{
    static class Logger
    {
        const string LOG_FILE_NAME = "log.txt";

        public enum LogTypes { Info, Warning, Error, CatastrophicFailure }

        static StreamWriter sw;

        public static void Initialize()
        {
            if (sw != null) return;
            sw = new StreamWriter(LOG_FILE_NAME, false, Encoding.UTF8);
        }

        public static void Append(string text, LogTypes logType = LogTypes.Info)
        {
            sw.WriteLine($"[{DateTime.Now}] [{logType}] {text}");
            sw.Flush();
        }

        public static void Finish()
        {
            if (sw == null) return;
            sw.Dispose();
            sw = null;
        }
    }
}