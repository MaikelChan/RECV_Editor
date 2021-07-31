using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace RECV_Editor
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            Text = "About " + assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            TitleLabel.Text = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            VersionLabel.Text = $"Version {version.Major}.{version.Minor}";
            DescriptionLabel.Text = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/MaikelChan/RECV_Editor");
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}