using System;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;

namespace sbs_popcon_recovery
{
    public partial class MainForm : Form
    {
        private string statusBarBackup = "";
        private string paramFileName = null;

        public MainForm(string FileName)
        {
            InitializeComponent();
            paramFileName = FileName;
        }

        private string decryptPass(string ciphertext, byte[] salt)
        {
            return Encoding.Unicode.GetString(
                ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), salt, DataProtectionScope.LocalMachine)
            );
        }

        private void decryptFile(string FileName)
        {
            try
            {
                toolStripStatusLabel1.Text = "Seaching passwords... please wait.";

                string[] lines = File.ReadAllLines(FileName);
                byte[] salt = { 0 };
                listView1.Items.Clear();

                string type = "";
                string user = "";
                string pass = "";
                string server = "";
                string port = "";

                foreach (string line in lines)
                {
                    // Find salt
                    if (line.Length > 8)
                    {
                        if (line.Substring(0, 8) == "Entropy:")
                        {
                            salt = Convert.FromBase64String(line.Substring(8));
                            continue;
                        }
                    }

                    // Reset fields on new entry / push data to list
                    if (line == "-")
                    {
                        if ((type == "SMTP Server") | (type == "POP3 Account"))
                        {
                            ListViewItem lvi = new ListViewItem(user);
                            lvi.SubItems.Add(pass);
                            lvi.SubItems.Add(type);
                            lvi.SubItems.Add(server);
                            lvi.SubItems.Add(port);
                            listView1.Items.Add(lvi);
                        }

                        type = "";
                        user = "";
                        pass = "";
                        server = "";
                        port = "";
                        continue;
                    }

                    // Find items
                    if (line.Length > 11)
                    {
                        if (line.Substring(0, 12) == "Pop3Account:")
                        {
                            type = "POP3 Account";
                            continue;
                        }
                    }

                    if (line.Length > 10)
                    {
                        if (line.Substring(0, 11) == "SmtpServer:")
                        {
                            type = "SMTP Server";
                            continue;
                        }
                    }

                    if (line.Length > 7)
                    {
                        if (line.Substring(0, 7) == "Server:")
                        {
                            server = line.Substring(7);
                            continue;
                        }
                    }

                    if (line.Length > 5)
                    {
                        if (line.Substring(0, 5) == "Port:")
                        {
                            port = line.Substring(5);
                            continue;
                        }
                    }

                    if (line.Length > 9)
                    {
                        if (line.Substring(0, 9) == "Username:")
                        {
                            user = line.Substring(9);
                            continue;
                        }

                        if (line.Substring(0, 9) == "Password:")
                        {
                            pass = decryptPass(line.Substring(9), salt);
                            continue;
                        }
                    }
                }

                toolStripStatusLabel1.Text = "Found " + listView1.Items.Count + " entrys.";
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("File not found, please specify an existing file!");
                toolStripStatusLabel1.Text = "File not found, please specify an existing file!";
            }
            catch (UnauthorizedAccessException e)
            {
                DialogResult res = MessageBox.Show("The specified file is not accessible, do you wan't to try again as administrator?", "Unauthorized!", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath, "\"" + FileName + "\"");
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                    Close();
                }

                toolStripStatusLabel1.Text = "File not accessible!";
            }
            catch (CryptographicException e)
            {
                MessageBox.Show("Error on decryption, passwords are encrypted machine dependent. Please use this tool on the target server!", "Crypto exception!");
                toolStripStatusLabel1.Text = "Decryption error, is this the target server? o_O";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (paramFileName != null)
            {
                pathField.Text = paramFileName;
                btnRecover_Click(btnRecover, null);
                return;
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Windows Small Business Server\\Data\\pop3records.dat";
            if (File.Exists(path))
            {
                pathField.Text = path;
                btnRecover_Click(btnRecover, null);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pathField.Text = openFileDialog1.FileName;
            }
        }

        private void btnRecover_Click(object sender, EventArgs e)
        {
            decryptFile(pathField.Text);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            int lenUser = 0;
            int lenPass = 0;
            int lenType = 0;
            int lenServer = 0;
            int lenPort = 0;

            string result = "";

            if (listView1.Items.Count == 0)
            {
                MessageBox.Show("Nothing to copy, please recover some passwords first.");
                return;
            }

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Text.Length > lenUser) { lenUser = item.Text.Length; }
                if (item.SubItems[1].Text.Length > lenPass) { lenPass = item.SubItems[1].Text.Length; }
                if (item.SubItems[2].Text.Length > lenType) { lenType = item.SubItems[2].Text.Length; }
                if (item.SubItems[3].Text.Length > lenServer) { lenServer = item.SubItems[3].Text.Length; }
                if (item.SubItems[4].Text.Length > lenPort) { lenPort = item.SubItems[4].Text.Length; }
            }

            foreach (ListViewItem item in listView1.Items)
            {
                result += item.Text.PadRight(lenUser) + " | ";
                result += item.SubItems[1].Text.PadRight(lenPass) + " | ";
                result += item.SubItems[2].Text.PadRight(lenType) + " | ";
                result += item.SubItems[3].Text.PadRight(lenServer) + " | ";
                result += item.SubItems[4].Text.PadRight(lenPort) + "\x0d\x0a";
            }

            Clipboard.SetText(result);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/LFriede/sbs-popcon-recovery");
        }

        private void linkLabel1_MouseEnter(object sender, EventArgs e)
        {
            statusBarBackup = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = "Visit this project on GitHub =)";
        }

        private void linkLabel1_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = statusBarBackup;
        }
    }
}
