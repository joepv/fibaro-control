using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace Fibaro_Control
{
    public partial class Form1 : Form
    {
        // Protection of saved password with DPAPI
        // More information https://stackoverflow.com/questions/34194223/dpapi-password-encryption-in-c-sharp-and-saving-into-database-then-decrypting-it

        public static string Protect(string stringToEncrypt, string optionalEntropy, DataProtectionScope scope)
        {
            return Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(stringToEncrypt)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , scope));
        }
        public static string Unprotect(string encryptedString, string optionalEntropy, DataProtectionScope scope)
        {
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedString)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , scope));
        }

        readonly Dictionary<string, int> sceneList = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (hcTextBox.Text == "" | loginTextBox.Text == "" | pwdTextBox.Text == "") {
                MessageBox.Show("Please fill in all parameters to connect to you HC2!", "Fibaro Control");
            }
            else
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Joep\FibaroControl");
                key.SetValue("HomeCenterIP", hcTextBox.Text);
                key.SetValue("LogIn", loginTextBox.Text);
                key.SetValue("Password", Protect(pwdTextBox.Text, null, DataProtectionScope.CurrentUser));
                key.Close();
                GetScenes();
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //loginTextBox.Text = Unprotect(hcTextBox.Text, null, DataProtectionScope.CurrentUser);
            System.Windows.Forms.Application.Exit();
        }

        private async void GetScenes()
        {
            //start scene:
            ///api/sceneControl?id=1&action=start

            var fibaroURL = "http://" + hcTextBox.Text + "/api/scenes";
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(loginTextBox.Text + ":" + pwdTextBox.Text);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            HttpContent content = response.Content;
            string result = await content.ReadAsStringAsync();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic scenesJson = serializer.Deserialize<object>(result);

            contextMenuStrip1.Items.Clear();
            sceneList.Clear();

            foreach (var sceneName in scenesJson)
            {
                if (sceneName["visible"] == true)
                {
                    sceneList.Add(sceneName["name"], sceneName["id"]);
                    contextMenuStrip1.Items.Add(sceneName["name"]);
                }
            }
            contextMenuStrip1.Items.Add("-");
            contextMenuStrip1.Items.Add("Settings");
            contextMenuStrip1.Items.Add("Exit");

            /*          contextMenuStrip1.Items.Add("Relaxen");
                        contextMenuStrip1.Items.Add("Binnentuin");
                        contextMenuStrip1.Items.Add("Zomeravond");
                        contextMenuStrip1.Items.Add("Goedemorgen");
                        contextMenuStrip1.Items.Add("Welterusten");
                        contextMenuStrip1.Items.Add("Thuiskomst");
                        contextMenuStrip1.Items.Add("Weggaan");
            */

    }

    private void RunScene(int sceneId)
        {
            MessageBox.Show(sceneId.ToString(), "Fibaro Control");
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string clickedOption = e.ClickedItem.Text;
            switch (clickedOption)
            {
                case "Settings":
                    notifyIcon1.Visible = false;
                    this.Show();
                    break;
                case "Exit":
                    System.Windows.Forms.Application.Exit();
                    break;
                default:
                    RunScene(sceneList[e.ClickedItem.Text]);
                    break;
            }

           /* foreach (KeyValuePair<string, int> joep in sceneList)
            {
                textBox1.Text = textBox1.Text + joep.Key + " --> " + joep.Value + "\r\n" ;
            }*/
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://docs.joepverhaeg.nl");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/joepv/fibaro-control");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // check registry for user information
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Joep\FibaroControl");
            if (key != null)
            {
                hcTextBox.Text    = (string)key.GetValue("HomeCenterIP");
                loginTextBox.Text = (string)key.GetValue("LogIn");
                pwdTextBox.Text   = Unprotect((string)key.GetValue("Password"), null, DataProtectionScope.CurrentUser);
                key.Close();
            }
        }
    }
}
