﻿using System;
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
            GetScenes();
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
    }
}
