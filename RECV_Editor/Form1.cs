using Microsoft.WindowsAPICodePack.Dialogs;
using PSO.PRS;
using RECV_Editor.File_Formats;
using RECV_Editor.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RECV_Editor
{
    public partial class Form1 : Form
    {
        Settings settings;
        const string SETTINGS_FILE = "Settings.json";

        RECV recv;

        bool isProcessRunning;

        public Form1()
        {
            InitializeComponent();

#if MULTITHREADING
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
#endif
        }

        #region Controls events

        private void Form1_Load(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Text = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            using (Icon icon = Resources.Icon)
            {
                Icon = icon;
            }

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

        private void PlatformComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            recv = RECV.GetRECV((RECV.Platforms)PlatformComboBox.SelectedIndex);

            LanguageComboBox.Items.Clear();
            LanguageComboBox.Items.AddRange(recv.LanguageNames);
            LanguageComboBox.SelectedIndex = 0;
        }

        private async void ExtractAllButton_Click(object sender, EventArgs e)
        {
            SetProcessRunning(true);

            int language = LanguageComboBox.SelectedIndex;

#if !DEBUG
            try
            {
#endif
            Progress<RECV.ProgressInfo> progress = new Progress<RECV.ProgressInfo>(UpdateStatus);
            await Task.Run(() => recv.ExtractAll(settings.Data.OriginalGameRootFolder, settings.Data.ProjectFolder, settings.Data.TablesFolder, language, progress));
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

            int language = LanguageComboBox.SelectedIndex;

#if !DEBUG
            try
            {
#endif
            Progress<RECV.ProgressInfo> progress = new Progress<RECV.ProgressInfo>(UpdateStatus);
            await Task.Run(() => recv.InsertAll(settings.Data.ProjectFolder, settings.Data.GeneratedGameRootFolder, settings.Data.OriginalGameRootFolder, settings.Data.TablesFolder, language, progress));
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
            const string rdxfile = @"C:\Users\Miguel\Desktop\RECV_Tests\r4_0010.rdx.unc";
            const string rdxfile2 = @"C:\Users\Miguel\Desktop\RECV_Tests\r4_0010.rdx.unc2";
            const int languageIndex = 3;

            Table table = recv.GetTableFromLanguage(settings.Data.TablesFolder, languageIndex);

            RDX rdx = RDX.GetRDX(RECV.Platforms.PS2);

            rdx.Extract(rdxfile, Path.GetFileName(rdxfile), rdxfile + "_extract", languageIndex, table);

            if (File.Exists(rdxfile2)) File.Delete(rdxfile2);
            File.Copy(rdxfile, rdxfile2);

            using (FileStream outputStream = new FileStream(rdxfile2, FileMode.Open, FileAccess.ReadWrite))
            {
                rdx.Insert(rdxfile + "_extract", outputStream, Path.GetFileName(rdxfile), languageIndex, table);
            }

            rdx.Extract(rdxfile2, Path.GetFileName(rdxfile2), rdxfile + "_extract2", languageIndex, table);
        }

        private void DebugDecompressButton_Click(object sender, EventArgs e)
        {
            string prsFile = @"D:\Romhacking\Proyectos\Resident Evil Code Veronica\Project\00000162";
            byte[] prsData = File.ReadAllBytes(prsFile);
            byte[] uncompressedPrsData = PRS.Decompress(prsData);

            File.WriteAllBytes(prsFile + ".unc", uncompressedPrsData);

            Table table = recv.GetTableFromLanguage(settings.Data.TablesFolder, 0);

            RDX rdx = RDX.GetRDX(RECV.Platforms.PS2);
            rdx.Extract(uncompressedPrsData, Path.GetFileName(prsFile), prsFile + "_output", (int)RECV.Platforms.PS2, table);
        }

        private void ChangePathsMenuItem_Click(object sender, EventArgs e)
        {
            PathSettingsForm pathSettings = new PathSettingsForm(settings);
            pathSettings.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
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

        #region Settings initialization

        const string NECESSARY_INITIALIZATION_TITLE = "Initialize settings";
        const string NECESSARY_INITIALIZATION_MESSAGE = "It is necessary to initialize the settings, so the program will now close.";

        bool InitializeSettings()
        {
            MessageBox.Show("This is the first time the program is run or the settings are invalid. Please configure some folder paths for the program to work properly.", "Initialize settings", MessageBoxButtons.OK, MessageBoxIcon.Information);

            using (CommonOpenFileDialog oDlg = new CommonOpenFileDialog())
            {
                // Original Game Root Folder

                oDlg.IsFolderPicker = true;
                oDlg.Title = PathSettingsForm.ORIGINAL_GAME_PATH_TITLE;
                if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    MessageBox.Show(NECESSARY_INITIALIZATION_MESSAGE, NECESSARY_INITIALIZATION_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Close();
                    return false;
                }

                settings.Data.OriginalGameRootFolder = oDlg.FileName;

                // Generated Game Root Folder

                oDlg.Title = PathSettingsForm.GENERATED_GAME_PATH_TITLE;
                if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    MessageBox.Show(NECESSARY_INITIALIZATION_MESSAGE, NECESSARY_INITIALIZATION_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Close();
                    return false;
                }

                settings.Data.GeneratedGameRootFolder = oDlg.FileName;

                // Extraction folder

                oDlg.Title = PathSettingsForm.PROJECT_PATH_TITLE;
                if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    MessageBox.Show(NECESSARY_INITIALIZATION_MESSAGE, NECESSARY_INITIALIZATION_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Close();
                    return false;
                }

                settings.Data.ProjectFolder = oDlg.FileName;

                // Tables folder

                oDlg.Title = PathSettingsForm.TABLES_PATH_TITLE;
                if (oDlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    MessageBox.Show(NECESSARY_INITIALIZATION_MESSAGE, NECESSARY_INITIALIZATION_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Close();
                    return false;
                }

                settings.Data.TablesFolder = oDlg.FileName;
            }

            // Save

            settings.Save();

            return true;
        }

        #endregion
    }
}