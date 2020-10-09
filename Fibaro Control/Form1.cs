using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Security;
using System.Security.Cryptography;

namespace Fibaro_Control
{
    public partial class Form1 : Form
    {
        readonly Dictionary<string, int> sceneList = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //hcTextBox.Text = Protect(pwdTextBox.Text, null, DataProtectionScope.CurrentUser);
            GetScenes();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //loginTextBox.Text = Unprotect(hcTextBox.Text, null, DataProtectionScope.CurrentUser);
            System.Windows.Forms.Application.Exit();
        }

        /* 
           Protection of saved password with DPAPI
           More information https://stackoverflow.com/questions/34194223/dpapi-password-encryption-in-c-sharp-and-saving-into-database-then-decrypting-it
        */
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

        private async void GetScenes()
        {
            //start scene:
            ///api/sceneControl?id=1&action=start

            var fibaroURL = "http://fibaro/api/scenes";
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes("username:password");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            HttpContent content = response.Content;
            string result = await content.ReadAsStringAsync();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic scenesJson = serializer.Deserialize<object>(result);

            contextMenuStrip1.Items.Clear();
            //notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            //var list = new List<KeyValuePair<string, int>>();

            foreach (var sceneName in scenesJson)
            {
                if (sceneName["visible"] == true)
                {
                    //_sceneList.Add(new KeyValuePair<string, int>(sceneName["name"], sceneName["id"]));
                    sceneList.Add(sceneName["name"], sceneName["id"]);
                    contextMenuStrip1.Items.Add(sceneName["name"]);
                }
            }
            contextMenuStrip1.Items.Add("-");
            contextMenuStrip1.Items.Add("About");
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
                case "About":
                    MessageBox.Show("Made by Joep Verhaeg", "Fibaro Control");
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

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
