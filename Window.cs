using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Security.Policy;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace _4DModLoader_Installer
{
    public partial class Window : Form, IMessageFilter
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_LBUTTONDOWN = 0x0201;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private HashSet<Control> controlsToMove = new HashSet<Control>();

        public Window()
        {
            InitializeComponent();
            Application.AddMessageFilter(this);

            controlsToMove.Add(this);
        }

        void DownloadVer(string gameVer, string modLoaderVer, string gamePath)
        {
            if (!Directory.Exists(gamePath) || !(File.Exists(Path.Combine(gamePath, "4D Miner.exe")) || File.Exists(Path.Combine(gamePath, "4DM.exe"))))
            {
                MessageBox.Show("Please select a path containing the game!", "Error",  MessageBoxButtons.OK, MessageBoxIcon.Error); return;
            }

            using (var client = new WebClient())
            {
                var formData = new NameValueCollection
                {
                    { "gameVer", gameVer },
                    { "modLoaderVer", modLoaderVer }
                };

                byte[] responseBytes = client.UploadValues("https://www.4dmodding.me/updater/getFiles.php", "POST", formData);
                string response = System.Text.Encoding.UTF8.GetString(responseBytes);

                if (response == "WRONG_GAME_VERSION" || response == "WRONG_MODLOADER_VERSION")
                    return;

                dynamic json = JsonConvert.DeserializeObject(response);
                List<(byte[] data, string path)> files = new List<(byte[] data, string path)>();
                foreach (string file in json.versionFiles)
                {
                    string fileName = Path.GetFileName(file);

                    files.Add((client.DownloadData(file), Path.Combine(gamePath, fileName)));
                }

                files.Add((client.DownloadData("https://www.4dmodding.me/core-files/4D Miner.exe"), Path.Combine(gamePath, "4D Miner.exe")));

                // rename original 4D Miner.exe into 4DM.exe
                if (!File.Exists(Path.Combine(gamePath, "4DM.exe")))
                    File.Move(Path.Combine(gamePath, "4D Miner.exe"), Path.Combine(gamePath, "4DM.exe"));

                foreach ((byte[] data, string path) file in files)
                    File.WriteAllBytes(file.path, file.data);

                MessageBox.Show("Done!", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        static string versions = "";
        private static VersionsObject versionsJson;

        private void Window_Load(object sender, EventArgs e)
        {
            // update the list of game versions
            gameVer.SelectedIndex = -1;
            gameVer.Items.Clear();

            using (var client = new WebClient())
                versions = client.DownloadString("https://www.4dmodding.me/core-files/versions.json");

            versionsJson = JsonConvert.DeserializeObject<VersionsObject>(versions);

            foreach (var gameVer in versionsJson.Versions.Reverse()) // reverse so that it goes from newer to older since newer ones are at bottom of the list
            {
                this.gameVer.Items.Add(gameVer.Key);
            }

            if (gameVer.Items.Count > 0)
                gameVer.SelectedIndex = 0;
        }
        private void gameVer_SelectedIndexChanged(object sender, EventArgs e)
        {
            // update the list of modloader versions
            if (gameVer.Items.Count > 0)
            {
                modLoaderVer.SelectedIndex = -1;
                modLoaderVer.Items.Clear();

                foreach (string version in versionsJson.Versions[gameVer.SelectedItem.ToString()].ToArray().Reverse())
                {
                    modLoaderVer.Items.Add(version);
                }

                if (modLoaderVer.Items.Count > 0)
                    modLoaderVer.SelectedIndex = 0;
            }
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN &&
                controlsToMove.Contains(Control.FromHandle(m.HWnd)))
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                return true;
            }
            return false;
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DownloadVer(gameVer.SelectedItem.ToString(), modLoaderVer.SelectedItem.ToString(), gamePath.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Select your 4D Miner Directory";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                gamePath.Text = dialog.FileName;
            }
        }
    }
    public class VersionsObject
    {
        public List<string> Libs { get; set; }
        public Dictionary<string, List<string>> Versions { get; set; }
    }
}
