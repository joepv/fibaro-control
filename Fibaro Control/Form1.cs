﻿using System;
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
                button1.Text = "Reload";
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private async void GetScenes()
        {
            var fibaroURL = "http://" + hcTextBox.Text + "/api/scenes";
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(loginTextBox.Text + ":" + pwdTextBox.Text);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            HttpContent content = response.Content;
            string result = await content.ReadAsStringAsync();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic scenesJson = serializer.Deserialize<object>(result);

            // load rooms
            var roomsURL = "http://" + hcTextBox.Text + "/api/rooms";
            HttpResponseMessage responseRooms = await client.GetAsync(roomsURL);
            HttpContent contentRooms = responseRooms.Content;
            string resultRooms = await contentRooms.ReadAsStringAsync();
            dynamic roomsJson = serializer.Deserialize<object>(resultRooms);

            var rooms = new Dictionary<int, string>();
            foreach (var room in roomsJson)
            {
                rooms[room["id"]] = room["name"];
            }

            //https://stackoverflow.com/questions/5868446/how-to-add-sub-menu-items-in-contextmenustrip-using-c4-0

            // end load rooms

            // load devices

            var devicesURL = "http://" + hcTextBox.Text + "/api/rooms";
            HttpResponseMessage responseDevices = await client.GetAsync(devicesURL);
            HttpContent contentDevices = responseDevices.Content;
            string resultDevices = await contentDevices.ReadAsStringAsync();
            dynamic devicesJson = serializer.Deserialize<object>(resultDevices);

            var devices = new Dictionary<int, string>();
            foreach (var device in devicesJson)
            {
                if (device["roomID"] != 0 || device["enabled"] == true) { 
                    devices[device["id"]] = device["name"]; // roomId toevoegen, of hier al submenu's maken/toevoegen.
                }
            }

            // end load devices

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

            /*          
            // fake demo scenes to spice my screenshots up :)
            contextMenuStrip1.Items.Add("Relaxen");
            contextMenuStrip1.Items.Add("Binnentuin");
            contextMenuStrip1.Items.Add("Zomeravond");
            contextMenuStrip1.Items.Add("Goedemorgen");
            contextMenuStrip1.Items.Add("Welterusten");
            contextMenuStrip1.Items.Add("Thuiskomst");
            contextMenuStrip1.Items.Add("Weggaan");
            */
    }

    private async void RunScene(int sceneId)
        {
            var fibaroURL = "http://" + hcTextBox.Text + "/api/sceneControl?id=" + sceneId.ToString() + "&action=start";
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(loginTextBox.Text + ":" + pwdTextBox.Text);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            //HttpContent content = response.Content;
            //string result = await content.ReadAsStringAsync();

            //JavaScriptSerializer serializer = new JavaScriptSerializer();
            //dynamic scenesJson = serializer.Deserialize<object>(result);
            if (response.StatusCode.ToString() != "Accepted")
            {
                MessageBox.Show("Error starting scene! :(", "Fibaro Control");
            }
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

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            contextMenuStrip1.Show(Control.MousePosition);
        }
    }
}
