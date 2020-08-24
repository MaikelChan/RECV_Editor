using csharp_prs;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RECV_Editor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region Controls events

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.Initialize();

            LoadSettings();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logger.Finish();
        }

        private void ExtractAllButton_Click(object sender, EventArgs e)
        {
            EnableControls(false);

#if !DEBUG
            try
            {
#endif
                Table table = new Table(settings.TableFile);
                RECV.ExtractAll(settings.GameRootFolder, settings.ProjectFolder, table);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Logger.Append(ex.Message, Logger.LogTypes.Error);
                MessageBox.Show($"{ex.Message}\n\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif

            EnableControls(true);
        }

        private void DebugExtractButton_Click(object sender, EventArgs e)
        {
            Table table = new Table(settings.TableFile);
            //Table frItEsTable = new Table(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\CodeVeronica_FRA_ITA_ESP_export.tbl");
            //ALD.Extract(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\TEST.ALD", @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\TEST", table);
            string text = ALD.ExtractTexts(File.ReadAllBytes(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\00000082.text"), table);
        }

        private void DebugDecompressButton_Click(object sender, EventArgs e)
        {
            string prsFile = @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\00000162";
            byte[] prsData = File.ReadAllBytes(prsFile);
            byte[] uncompressedPrsData = Prs.Decompress(prsData);

            File.WriteAllBytes(prsFile + ".unc", uncompressedPrsData);

            Table table = new Table(settings.TableFile);
            RDX.Extract(uncompressedPrsData, prsFile + "_output", table);
        }

        #endregion

        #region Settings

        const string SETTINGS_FILE = "Settings.json";

        Settings settings;

        void LoadSettings()
        {
            if (!File.Exists(SETTINGS_FILE))
            {
                Logger.Append("Settings file not found. Initializing settings...");
                InitializeSettings();
            }
            else
            {
                Logger.Append("Loading settings file...");

                JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
                settings = jsSerializer.Deserialize<Settings>(File.ReadAllText(SETTINGS_FILE));

                if (!settings.CheckIfValid())
                {
                    Logger.Append("Settings file contains invalid entries. Initializing settings...");
                    InitializeSettings();
                }
            }

            Logger.Append("Settings have been loaded.");
        }

        void InitializeSettings()
        {
            MessageBox.Show("This is the first time the program is run or the settings are invalid. Please configure some folder paths for the program to work properly.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Information);

            settings = new Settings();

            CommonOpenFileDialog oDlg = new CommonOpenFileDialog();
            oDlg.IsFolderPicker = true;
            oDlg.Title = "Select the root folder of Resident Evil Code Veronica (PAL) for PS2";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                MessageBox.Show("It is necessary to initialize the settings, so the program will now close.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Close();
                return;
            }

            settings.GameRootFolder = oDlg.FileName;

            oDlg.Title = "Select your project folder where you want to extract everything";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                Close();
                return;
            }

            settings.ProjectFolder = oDlg.FileName;

            oDlg.Title = "Select the table file";
            oDlg.IsFolderPicker = false;
            oDlg.Filters.Add(new CommonFileDialogFilter("Table files (*.tbl)", "*.tbl"));
            oDlg.Filters.Add(new CommonFileDialogFilter("Text files (.txt)", "*.txt"));
            oDlg.Filters.Add(new CommonFileDialogFilter("All files (*.*)", "*.*"));
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                Close();
                return;
            }

            settings.TableFile = oDlg.FileName;

            SaveSettings();
        }

        void SaveSettings()
        {
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
            File.WriteAllText(SETTINGS_FILE, jsSerializer.Serialize(settings));
        }

        class Settings
        {
            public string GameRootFolder { get; set; }
            public string ProjectFolder { get; set; }
            public string TableFile { get; set; }

            public bool CheckIfValid()
            {
                if (!Directory.Exists(GameRootFolder)) return false;
                if (!Directory.Exists(ProjectFolder)) return false;
                if (!File.Exists(TableFile)) return false;

                return true;
            }
        }

        #endregion

        void EnableControls(bool value)
        {
            ExtractAllButton.Enabled = value;
            DebugGroup.Enabled = value;
            DebugExtractButton.Enabled = value;
            DebugDecompressButton.Enabled = value;
        }

        void UpdateStatus(string statusText, int progressValue, int maxProgressValue)
        {
            StatusLabel.Text = statusText;
            StatusProgressBar.Value = progressValue;
            StatusProgressBar.Maximum = maxProgressValue;
        }
    }
}