using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Windows.Forms;

namespace RECV_Editor
{
    public partial class PathSettings : Form
    {
        readonly Settings settings;

        public const string ORIGINAL_GAME_PATH_TITLE = "Select the folder where the data extracted from the original ISOs of Resident Evil Code Veronica (PAL) is located";
        public const string PROJECT_PATH_TITLE = "Select your project folder where you want to extract everything";
        public const string GENERATED_GAME_PATH_TITLE = "Select the folder where the game's generated files will be saved";
        public const string TABLES_PATH_TITLE = "Select the folder where your tables are located";

        public PathSettings(Settings settings)
        {
            this.settings = settings;

            InitializeComponent();
        }

        private void PathSettings_Load(object sender, EventArgs e)
        {
            OriginalGamePathTextBox.Text = settings.Data.OriginalGameRootFolder;
            ProjectPathTextBox.Text = settings.Data.ProjectFolder;
            GeneratedGamePathTextBox.Text = settings.Data.GeneratedGameRootFolder;
            TablesPathTextBox.Text = settings.Data.TablesFolder;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(OriginalGamePathTextBox.Text))
            {
                MessageBox.Show("Original Game Path contains an invalid directory.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(ProjectPathTextBox.Text))
            {
                MessageBox.Show("Project Path contains an invalid directory.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(GeneratedGamePathTextBox.Text))
            {
                MessageBox.Show("Generated Game Path contains an invalid directory.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(TablesPathTextBox.Text))
            {
                MessageBox.Show("Tables Path contains an invalid directory.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            settings.Data.OriginalGameRootFolder = OriginalGamePathTextBox.Text;
            settings.Data.ProjectFolder = ProjectPathTextBox.Text;
            settings.Data.GeneratedGameRootFolder = GeneratedGamePathTextBox.Text;
            settings.Data.TablesFolder = TablesPathTextBox.Text;
            settings.Save();

            Close();
        }

        private void OriginalGamePathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenBrowseDialog(OriginalGamePathTextBox, ORIGINAL_GAME_PATH_TITLE);
        }

        private void ProjectPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenBrowseDialog(ProjectPathTextBox, PROJECT_PATH_TITLE);
        }

        private void GeneratedGamePathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenBrowseDialog(GeneratedGamePathTextBox, GENERATED_GAME_PATH_TITLE);
        }

        private void TablesPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenBrowseDialog(TablesPathTextBox, TABLES_PATH_TITLE);
        }

        void OpenBrowseDialog(TextBox textBox, string title)
        {
            using (CommonOpenFileDialog oDlg = new CommonOpenFileDialog())
            {
                oDlg.IsFolderPicker = true;
                oDlg.InitialDirectory = textBox.Text;
                oDlg.Title = title;

                if (oDlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    textBox.Text = oDlg.FileName;
                }
            }
        }
    }
}