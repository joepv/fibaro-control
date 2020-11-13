using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.IO;

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
        public static void Log(string logMessage)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            
            if (arguments.Length > 1)
            {
                if (arguments[1] == "/debug")
                {
                    string logFile = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Fibaro-Control.txt";
                    using (StreamWriter w = File.AppendText(logFile))
                    {
                        //2020-08-06 21:20:41   this is a log message
                        w.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\t{logMessage}");
                    }
                }
            }

            
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
                Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private async System.Threading.Tasks.Task<dynamic> GetFibaroDataAsync(string apiURL)
        {
            var fibaroURL = "http://" + hcTextBox.Text + "/api/" + apiURL;
            Log("Start HttpClient to get data from " + fibaroURL);
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(loginTextBox.Text + ":" + pwdTextBox.Text);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            HttpResponseMessage response = await client.GetAsync(fibaroURL);
            HttpContent content = response.Content;
            string result = await content.ReadAsStringAsync();

            Log("Got a result from HttpClient with " + result.Length + " characters in lenght, start JSON decoding with JavaScriptSerializer.");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            // Set the maximum length of JSON strings. Default this is 4 MB text, but Fibaro puts its Virtual Device code into
            // the devices JSON and with big Fibaro configurations the loaded JSON is > 10 MB. Unfortunately you cannot filter with
            // the API call.
            serializer.MaxJsonLength = int.MaxValue;
            //dynamic scenesJson = serializer.Deserialize<object>(result);
            return serializer.Deserialize<object>(result);
        }

        private async void BuildMenu() 
        {
            contextMenuStrip1.Items.Clear();
            // Retrieve defined rooms from the Fibaro System.
            dynamic fibaroRooms = GetFibaroDataAsync("rooms");
            await fibaroRooms;
            Log("Decoded the rooms JSON, start building the menu.");
            foreach (var room in fibaroRooms.Result)
            {
                ToolStripMenuItem roomMenuItem = new ToolStripMenuItem(room["name"])
                {
                    Name = "room" + room["id"], //room29
                    Tag = room["id"],
                };
                Log("Add room " + room["name"] + "(" + room["id"] + ") to the menu.");
                contextMenuStrip1.Items.Add(roomMenuItem);
            }

            // Retrieve devices from the Fibaro System and add them to the rooms as submenu.
            dynamic fibaroDevices = GetFibaroDataAsync("devices");
            await fibaroDevices;
            Log("Decoded the devices JSON, start building the menu.");
            foreach (var device in fibaroDevices.Result)
            {
                if (device["roomID"] != 0 && device["type"] != "virtual_device" && device["visible"] == true && device["enabled"] == true)
                {
                    // Only add lights to the menu.
                    Log("Check is device named " + device["name"] + "(" + device["id"] + ") is a light");
                    if (device["properties"].ContainsKey("isLight"))
                    {
                        // In HC2 this property is a string (yes, this is bad) and in HC3 this is a bool (as it should), therefore
                        // I check the type convert it to a bool.
                        bool isLight;
                        if (device["properties"]["isLight"].GetType() == typeof(string)) { 
                            isLight = bool.Parse(device["properties"]["isLight"]);
                        }
                        else
                        {
                            isLight = device["properties"]["isLight"];
                        }

                        if (isLight == true)
                        {
                            ToolStripMenuItem deviceMenuItem = new ToolStripMenuItem(device["name"])
                            {
                                Name = "device" + device["id"],
                                Tag = device["id"]
                            };
                            deviceMenuItem.Click += new EventHandler(ToggleDevice);
                            int menuId = contextMenuStrip1.Items.IndexOfKey("room" + device["roomID"]);
                            Log("Add device " + device["name"] + "(" + device["id"] + ") to room ( " + device["roomID"] + ").");
                            (contextMenuStrip1.Items[menuId] as ToolStripMenuItem).DropDownItems.Add(deviceMenuItem);
                        }
                    }
                    else
                    {
                        Log("Skip device " + device["name"] + "(" + device["id"] + "). It's not a light.");
                    }
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
            Log("Removed " + emptyRooms.Count + " empty menu's, because rooms have no devices.");

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
            Log("Decoded the scenes JSON, start building the menu.");
            int sceneCount = 0;
            foreach (var scene in fibaroScenes.Result)
            {
                Log("Check if scene " + scene["name"] + "(" + scene["id"] + ") is visible");
                bool sceneVisible;
                // HC2
                if (scene.ContainsKey("visible"))
                {
                    if (scene["visible"] == true)
                    {
                        sceneVisible = true;
                    }
                    else
                    {
                        sceneVisible = false;
                    }
                }
                else
                // HC3
                {
                    if (scene["hidden"] == true)
                    {
                        sceneVisible = false;
                    }
                    else
                    {
                        sceneVisible = true;
                    }
                }

                if (sceneVisible == true)
                {
                    ToolStripMenuItem sceneMenuItem = new ToolStripMenuItem(scene["name"])
                    {
                        Name = "scene" + scene["id"],
                        Tag = scene["id"],
                    };
                    sceneMenuItem.Click += new EventHandler(RunScene);

                    Log("Add scene " + scene["name"] + "(" + scene["id"] + ") to the scenes menu.");
                    int menuId = contextMenuStrip1.Items.IndexOfKey("Scenes");
                    (contextMenuStrip1.Items[menuId] as ToolStripMenuItem).DropDownItems.Add(sceneMenuItem);

                    sceneCount++;
                }
                else
                {
                    Log("Skip scene " + scene["name"] + "(" + scene["id"] + "). It's a hidden scene.");
                }
                
                if (sceneCount == 16)
                {
                    Log("Skipped scene processing. To many visible scene's.");
                    MessageBox.Show("You have to many visible scene's, only showing the first 16!", "Fibaro Control");
                    break;
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

        private async void ToggleDevice(object sender, EventArgs e)
        {
            // First check if device is turned on/off.
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            int fibaroDeviceId = (int)menuItem.Tag;
            dynamic fibaroDeviceInfo = GetFibaroDataAsync("devices?id=" + fibaroDeviceId.ToString());
            await fibaroDeviceInfo;

            // In HC2 this property is a string (yes, this is bad) and in HC3 this is a bool (as it should), therefore
            // I check the type convert it to a bool.
            bool lightStatus;
            if (fibaroDeviceInfo.Result["properties"]["value"].GetType() == typeof(string))
            {
                if (fibaroDeviceInfo.Result["properties"]["value"] == "false" || fibaroDeviceInfo.Result["properties"]["value"] == "0")
                {
                    lightStatus = false;
                }
                else
                {
                    lightStatus = true;
                }
            }
            else if(fibaroDeviceInfo.Result["properties"]["value"].GetType() == typeof(int))
            {
                if (fibaroDeviceInfo.Result["properties"]["value"] == 0)
                {
                    lightStatus = false;
                }
                else
                {
                    lightStatus = true;
                }
            }
            else
            {
                lightStatus = fibaroDeviceInfo.Result["properties"]["value"];
            }

            // Send opposite command to flip light on/off.
            if (lightStatus == false) // light is off
            {
                dynamic fibaroDeviceTurnOn = GetFibaroDataAsync("callAction?deviceID=" + fibaroDeviceId.ToString() + "&name=turnOn");
                await fibaroDeviceTurnOn;
                // Do nothing with the reply, Fibaro HC2 status reply is not that usefull.
            } else // light is on
            {
                dynamic fibaroDeviceTurnOff = GetFibaroDataAsync("callAction?deviceID=" + fibaroDeviceId.ToString() + "&name=turnOff");
                await fibaroDeviceTurnOff;
                // Do nothing with the reply, Fibaro HC2 status reply is not that usefull.
            }

        }
        private async void RunScene(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            int fibaroSceneId = (int)menuItem.Tag;
            dynamic fibaroRunScene = GetFibaroDataAsync("sceneControl?id=" + fibaroSceneId.ToString() + "&action=start");
            await fibaroRunScene;
            // There is no JSON reply, just a text reply from Fibaro HC2 with the text "Accepted".
        }
        private void ContextMenuStrip1_ItemClicked(object sender, EventArgs e) //ToolStripItemClickedEventArgs e
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            string menuText = menuItem.Text;
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
