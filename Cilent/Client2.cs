﻿using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using SharedClass;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cilent
{
    public partial class Client2 : DevExpress.XtraEditors.XtraForm
    {
        private static string host;
        private static int port;
        private static TcpClient client;
        private static string directory;
        private static Stream stream;

        private const int BUFFER_SIZE = 999999999;

        public Client2()
        {
            InitializeComponent();
        }

        private void ConnectToServer()
        {
            try
            {
                // 1. Connect to server
                client = new TcpClient();
                client.Connect(host, port);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Name);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            host = txtHost.Text;
            port = int.Parse(txtPort.Text);

            ConnectToServer();

            lbStatus.Text = "Connected";
            lbDetail.Caption = "Connected to " + client.Client.RemoteEndPoint;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                client.Close();
                client.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public void LoadDirectory(Dir directoryCollection)
        {
            //DirectoryInfo di = new DirectoryInfo(Dir);
            TreeNode tds = directoryView.Nodes.Add(directoryCollection.Name);
            tds.Tag = directoryCollection.Path;
            //tds.StateImageIndex = 0;

            //Load tất cả các file bên trong đường dẫn cha
            LoadFiles(directoryCollection, tds);

            //Load tất cả các thư mục con bên trong đường dẫn cha
            LoadSubDirectories(directoryCollection, tds);
        }

        private void LoadSubDirectories(Dir parrentDirectory, TreeNode td)
        {
            // Lấy tất cả các thư mục con trong đường dẫn cha  
            //string[] subdirectoryEntries = Directory.GetDirectories(parrentDirectory);

            // Lặp qua tất cả các đường dẫn đó
            foreach (Dir subdirectory in parrentDirectory.SubDirectories)
            {

                //DirectoryInfo di = new DirectoryInfo(subdirectory);
                TreeNode tds = td.Nodes.Add(subdirectory.Name);
                //tds.StateImageIndex = 0;
                tds.Tag = subdirectory.Path;
                LoadFiles(subdirectory, tds);
                LoadSubDirectories(subdirectory, tds);
            }
        }

        private void LoadFiles(Dir dir, TreeNode td)
        {
            //string[] Files = Directory.GetFiles(dir, "*.*");

            // Lặp qua các file trong thư mục 
            foreach (FileDir file in dir.SubFiles)
            {
                //FileInfo fi = new FileInfo(file);
                TreeNode tds = td.Nodes.Add(file.Name);
                tds.Tag = file.Path;
                //tds.StateImageIndex = 1;
                //UpdateProgress();

            }
        }

        private object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            object obj = (object)binForm.Deserialize(memStream);
            return obj;
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            directory = txtDirectory.Text;

            try
            {
                // 2. send
                byte[] data = Encoding.UTF8.GetBytes(directory);
                Stream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                // 3. receive
                data = new byte[BUFFER_SIZE];
                stream.Read(data, 0, BUFFER_SIZE);

                Dir directoryCollection = (Dir)ByteArrayToObject(data);
                LoadDirectory(directoryCollection);

                //MessageBox.Show(Encoding.UTF8.GetString(data), this.Name);
                
                // 4. Close
                stream.Close();
                client.Close();

                // 5. Reconnect
                ConnectToServer();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Name);
            }
        }

        private void btnExit_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Dispose();
            Environment.Exit(Environment.ExitCode);
        }
    }


}