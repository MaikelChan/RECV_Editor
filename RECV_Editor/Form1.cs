using Microsoft.WindowsAPICodePack.Dialogs;
using PSO.PRS;
using RECV_Editor.File_Formats;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RECV_Editor
{
    public partial class Form1 : Form
    {
        Settings settings;
        const string SETTINGS_FILE = "Settings.json";

        bool isProcessRunning;

        public Form1()
        {
            InitializeComponent();

            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        }

        #region Controls events

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.Initialize();

            settings = new Settings(SETTINGS_FILE);
            if (!settings.Load())
            {
                if (!InitializeSettings()) return;
            }

            SetProcessRunning(false);
            UpdateStatus(new RECV.ProgressInfo("Ready...", 0, 100));

            string[] platforms = Enum.GetNames(typeof(RECV.Platforms));
            PlatformComboBox.Items.AddRange(platforms);
            PlatformComboBox.SelectedIndex = 0;

            string[] languages = Enum.GetNames(typeof(RECV.Languages));
            LanguageComboBox.Items.AddRange(languages);
            LanguageComboBox.SelectedIndex = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessRunning)
            {
                MessageBox.Show("There are tasks currently running. Please wait until they finish.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            else
            {
                Logger.Finish();
            }
        }

        private async void ExtractAllButton_Click(object sender, EventArgs e)
        {
            SetProcessRunning(true);

            RECV.Languages language = (RECV.Languages)LanguageComboBox.SelectedIndex;

#if !DEBUG
            try
            {
#endif
            Progress<RECV.ProgressInfo> progress = new Progress<RECV.ProgressInfo>(UpdateStatus);
            await Task.Run(() => RECV.ExtractAll(settings.Data.OriginalGameRootFolder, settings.Data.ProjectFolder, settings.Data.TablesFolder, language, progress));
#if !DEBUG
            }
            catch (Exception ex)
            {
                Logger.Append(ex.Message, Logger.LogTypes.Error);
                MessageBox.Show($"{ex.Message}\n\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif

            SetProcessRunning(false);
        }

        private async void InsertAllButton_Click(object sender, EventArgs e)
        {
            SetProcessRunning(true);

            RECV.Languages language = (RECV.Languages)LanguageComboBox.SelectedIndex;

#if !DEBUG
            try
            {
#endif
            Progress<RECV.ProgressInfo> progress = new Progress<RECV.ProgressInfo>(UpdateStatus);
            await Task.Run(() => RECV.InsertAll(settings.Data.ProjectFolder, settings.Data.GeneratedGameRootFolder, settings.Data.OriginalGameRootFolder, settings.Data.TablesFolder, language, progress));
#if !DEBUG
            }
            catch (Exception ex)
            {
                Logger.Append(ex.Message, Logger.LogTypes.Error);
                MessageBox.Show($"{ex.Message}\n\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif

            SetProcessRunning(false);
        }

        private void DebugExtractButton_Click(object sender, EventArgs e)
        {
            Table table = RECV.GetTableFromLanguage(settings.Data.TablesFolder, RECV.Languages.English);

            ALD.Extract(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\ENG\SYSMES1.ALD", @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\TEST", table);
            ALD.Insert(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\TEST", @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\TEST.ALD", table);

            //using (FileStream fs = File.OpenRead(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\00000082.text"))
            //{
            //    string text = Texts.Extract(fs, table);
            //}

            //string texts = File.ReadAllText(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\ENG\SYSMES1.ALD\01.txt");

            //using (FileStream fs = File.Create(@"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\SYSMES1_2"))
            //{
            //    Texts.Insert(texts, fs, table);
            //}
        }

        private void DebugDecompressButton_Click(object sender, EventArgs e)
        {
            string prsFile = @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\00000162";
            byte[] prsData = File.ReadAllBytes(prsFile);
            byte[] uncompressedPrsData = PRS.Decompress(prsData);

            File.WriteAllBytes(prsFile + ".unc", uncompressedPrsData);

            Table table = RECV.GetTableFromLanguage(settings.Data.TablesFolder, RECV.Languages.English);
            RDX.Extract(uncompressedPrsData, prsFile + "_output", table);
        }

        #endregion

        void SetProcessRunning(bool value)
        {
            isProcessRunning = value;

            PlatformLabel.Enabled = !value;
            PlatformComboBox.Enabled = !value;
            LanguageLabel.Enabled = !value;
            LanguageComboBox.Enabled = !value;

            ExtractAllButton.Enabled = !value;
            InsertAllButton.Enabled = !value;
            //DebugGroup.Enabled = !value;
            //DebugExtractButton.Enabled = !value;
            //DebugDecompressButton.Enabled = !value;

            DebugGroup.Enabled = false;
            DebugExtractButton.Enabled = false;
            DebugDecompressButton.Enabled = false;
        }

        void UpdateStatus(RECV.ProgressInfo progressInfo)
        {
            StatusLabel.Text = progressInfo.statusText;
            StatusProgressBar.Value = progressInfo.progressValue;
            StatusProgressBar.Maximum = progressInfo.maxProgressValue;
        }

        bool InitializeSettings()
        {
            MessageBox.Show("This is the first time the program is run or the settings are invalid. Please configure some folder paths for the program to work properly.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Information);

            CommonOpenFileDialog oDlg = new CommonOpenFileDialog();

            // Original Game Root Folder

            oDlg.IsFolderPicker = true;
            oDlg.Title = "Select the root folder of Resident Evil Code Veronica (PAL) for PS2";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                MessageBox.Show("It is necessary to initialize the settings, so the program will now close.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Close();
                return false;
            }

            settings.Data.OriginalGameRootFolder = oDlg.FileName;

            // Generated Game Root Folder

            oDlg.Title = "Select the folder where the game's generated files will be saved";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                MessageBox.Show("It is necessary to initialize the settings, so the program will now close.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Close();
                return false;
            }

            settings.Data.GeneratedGameRootFolder = oDlg.FileName;

            // Extraction folder

            oDlg.Title = "Select your project folder where you want to extract everything";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                Close();
                return false;
            }

            settings.Data.ProjectFolder = oDlg.FileName;

            // Extraction folder

            oDlg.Title = "Select the folder where your tables are located";
            if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                Close();
                return false;
            }

            settings.Data.TablesFolder = oDlg.FileName;

            // Save

            settings.Save();

            return true;
        }
    }
}