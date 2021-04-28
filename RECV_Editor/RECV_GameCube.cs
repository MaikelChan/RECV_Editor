using System;

namespace RECV_Editor
{
    class RECV_GameCube : RECV
    {
        readonly static string[] languageNames = new string[] { "Japanese", "English (USA)", "English", "German", "French", "Spanish", "Italian" };
        public override string[] LanguageNames => languageNames;

        readonly static string[] languageCodes = new string[] { "JPN", "USA", "ENG", "GER", "FRA", "SPA", "ITA" };
        protected override string[] LanguageCodes => languageCodes;

        readonly static int[] languageIndices = new int[] { 0, 1, 2, 3, 4, 5, 6 };
        protected override int[] LanguageIndices => languageIndices;

        public override void ExtractAll(string inputFolder, string outputFolder, string tablesFolder, int languageIndex, IProgress<ProgressInfo> progress)
        {
            throw new NotImplementedException();
        }

        public override void InsertAll(string inputFolder, string outputFolder, string originalDataFolder, string tablesFolder, int languageIndex, IProgress<ProgressInfo> progress)
        {
            throw new NotImplementedException();
        }
    }
}