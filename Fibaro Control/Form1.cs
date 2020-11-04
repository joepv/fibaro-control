﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Windows.Forms.VisualStyles;
using System.Threading.Tasks;

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
                BuildMenu();
                button1.Text = "Reload";
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private async System.Threading.Tasks.Task<dynamic> GetFibaroDataAsync(string apiURL)
        {
            var fibaroURL = "http://" + hcTextBox.Text + "/api/" + apiURL;
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(loginTextBox.Text + ":" + pwdTextBox.Text);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            HttpContent content = response.Content;
            string result = await content.ReadAsStringAsync();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            //dynamic scenesJson = serializer.Deserialize<object>(result);
            return serializer.Deserialize<object>(result);
        }

        private async void BuildMenu() 
        {
            // Retrieve defined rooms from the Fibaro System.
            dynamic fibaroRooms = GetFibaroDataAsync("rooms");
            await fibaroRooms;
            foreach (var room in fibaroRooms.Result)
            {
                ToolStripMenuItem roomMenuItem = new ToolStripMenuItem(room["name"])
                {
                    Name = "room" + room["id"], //room29
                    Tag = room["id"],
                };
                contextMenuStrip1.Items.Add(roomMenuItem);
            }

            // Retrieve devices from the Fibaro System and add them to the rooms as submenu.
            dynamic fibaroDevices = GetFibaroDataAsync("devices");
            await fibaroDevices;
            foreach (var device in fibaroDevices.Result)
            {
                if (device["roomID"] != 0 && device["type"] != "virtual_device" && device["visible"] == true && device["enabled"] == true)
                {
                    ToolStripMenuItem deviceMenuItem = new ToolStripMenuItem(device["name"])
                    {
                        Name = "device" + device["id"],
                        Tag = device["id"]
                    };
                    deviceMenuItem.Click += new EventHandler(ToggleDevice);
                    int menuId = contextMenuStrip1.Items.IndexOfKey("room" + device["roomID"]);
                    (contextMenuStrip1.Items[menuId] as ToolStripMenuItem).DropDownItems.Add(deviceMenuItem);
                }
            }

            // Remove all rooms with no devices (empty submenu's).
            // I add all items to remove to a list first, else the index changes when removing an item and the program crashes.
            List<string> emptyRooms = new List<string>();
            foreach (ToolStripMenuItem roomMenuItem in contextMenuStrip1.Items)
            {
                if (roomMenuItem.DropDownItems.Count == 0)
                {
                    emptyRooms.Add(roomMenuItem.Name);
                }
            }
            foreach (string emptyRoom in emptyRooms)
            {
                contextMenuStrip1.Items.RemoveByKey(emptyRoom);
            }

            // Add a separator and add a scenes menu.
            contextMenuStrip1.Items.Add("-");

            ToolStripMenuItem scenesMenuItem = new ToolStripMenuItem("Scenes")
            {
                Name = "Scenes"
            };
            contextMenuStrip1.Items.Add(scenesMenuItem);

            // Load scenes from the Fibaro System.
            dynamic fibaroScenes = GetFibaroDataAsync("scenes");
            await fibaroScenes;
            foreach (var scene in fibaroScenes.Result)
            {
                if (scene["visible"] == true)
                {
                    ToolStripMenuItem sceneMenuItem = new ToolStripMenuItem(scene["name"])
                    {
                        Name = "scene" + scene["id"],
                        Tag = scene["id"],
                    };
                    sceneMenuItem.Click += new EventHandler(RunScene);
                    int menuId = contextMenuStrip1.Items.IndexOfKey("Scenes");
                    (contextMenuStrip1.Items[menuId] as ToolStripMenuItem).DropDownItems.Add(sceneMenuItem);
                }
            }
            
            // Add the application menu items.
            contextMenuStrip1.Items.Add("-");

            ToolStripMenuItem settingMenuItem = new ToolStripMenuItem("Settings")
            {
                Name = "SettingsMenu"
              
            };
            settingMenuItem.Click += new EventHandler(ContextMenuStrip1_ItemClicked);
            contextMenuStrip1.Items.Add(settingMenuItem);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit")
            {
                Name = "ExitMenu"

            };
            exitMenuItem.Click += new EventHandler(ContextMenuStrip1_ItemClicked);
            contextMenuStrip1.Items.Add(exitMenuItem);
        }

        private void ToggleDevice(object sender, EventArgs e)
        {
            MessageBox.Show("Toggle device!", "Fibaro Control");
        }
        private void RunScene(object sender, EventArgs e)
        {
            MessageBox.Show("Run scene!", "Fibaro Control");

            /* var fibaroURL = "http://" + hcTextBox.Text + "/api/sceneControl?id=" + sceneId.ToString() + "&action=start";
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
             }*/
        }
        private void ContextMenuStrip1_ItemClicked(object sender, EventArgs e) //ToolStripItemClickedEventArgs e
        {
            var menuItem = sender as MenuItem;
            var menuText = menuItem.Text;
            switch (menuText)
                {
                    case "Settings":
                        notifyIcon1.Visible = false;
                        this.Show();
                        break;
                    case "Exit":
                        System.Windows.Forms.Application.Exit();
                        break;
                    default:
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
