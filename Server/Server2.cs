﻿using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
using MySharedClass;

namespace Server
{
    public partial class Server2 : DevExpress.XtraEditors.XtraForm
    {
        //public static ManualResetEvent allDone = new ManualResetEvent(false);

        private static int port;
        private static String address;
        private static TcpListener server;
        private const int BUFFER_SIZE = 1024;
        private static Thread serverTheard;
        private static IPAddress host;
        private static int _connectionsCount;
        private static int MAX_CONNECTION = 1000;

        public Server2()
        {
            InitializeComponent();
            btnRestart.Enabled = btnStop.Enabled = clientPanel.Enabled = false;
        }
        private static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private void StartServer()
        {
            Socket socket = server.AcceptSocket();

            while (_connectionsCount < MAX_CONNECTION || MAX_CONNECTION == 0)
            {
                Socket soc = server.AcceptSocket();
                _connectionsCount++;

                Thread t = new Thread((obj) =>
                {
                    DoWork((Socket)obj);
                });
                t.Start(soc);
            }
        }

        private void DoWork(Socket socket)
        {
            try
            {
                Invoke(new Action(() =>
                {
                    clientList.Items.Add(socket.RemoteEndPoint + " connected");
                }));

                // 2. receive
                byte[] receiveDataSizeByte = new byte[BUFFER_SIZE];
                socket.Receive(receiveDataSizeByte);
                int size = int.Parse(Encoding.ASCII.GetString(receiveDataSizeByte));

                byte[] _buffer = new byte[BUFFER_SIZE];
                byte[] data = new byte[size];
                socket.Receive(_buffer);
                Array.Copy(_buffer, data, size);

                //MessageBox.Show(Encoding.ASCII.GetString(data));

                // 3. handle
                String dir = Encoding.ASCII.GetString(data);
                Dir directoryCollection = LoadDirectory(dir);
                byte[] sendData = ObjectToByteArray(directoryCollection);

                // 4. send
                socket.Send(sendData);

                // 5. close
                socket.Shutdown(SocketShutdown.Both);
                //server.Stop();

            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.ToString(), this.Name);
            }
        }

        public Dir LoadDirectory(String receiveDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(receiveDirectory);

            Dir currentDir = new Dir(directoryInfo.Name, directoryInfo.FullName);

            //Load tất cả các file bên trong đường dẫn cha
            LoadFiles(receiveDirectory, currentDir);

            //Load tất cả các thư mục con bên trong đường dẫn cha
            LoadSubDirectories(receiveDirectory, currentDir);

            return currentDir;
        }

        private void LoadSubDirectories(String parentDirectory, Dir directory)
        {
            // Lấy tất cả các thư mục con trong đường dẫn cha  
            String[] subdirectoryEntries = Directory.GetDirectories(parentDirectory);

            // Lặp qua tất cả các đường dẫn đó
            foreach (String subdirectory in subdirectoryEntries)
            {
                DirectoryInfo di = new DirectoryInfo(subdirectory);
                Dir currentDir = new Dir(di.Name, di.FullName);
                directory.SubDirectories.Add(currentDir);

                LoadFiles(subdirectory, currentDir);
                LoadSubDirectories(subdirectory, currentDir);
            }
        }

        private void LoadFiles(String parentDirectory, Dir directory)
        {
            String[] Files = Directory.GetFiles(parentDirectory);

            // Lặp qua các file trong thư mục 
            foreach (String file in Files)
            {
                FileInfo fi = new FileInfo(file);
                FileDir fileDir = new FileDir(fi.Name, fi.FullName);
                directory.SubFiles.Add(fileDir);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (acceptAll.Checked)
            {
                host = IPAddress.Any;
            }
            else
            {
                address = txtAddress.Text;
                host = IPAddress.Parse(address);
            }
            port = int.Parse(txtPort.Text);


            try
            {
                server = new TcpListener(host, port);

                // 1. listen
                server.Start();
                lbStatus.Text = "Activated";
                lbDetail.Caption = "Server started on " + server.LocalEndpoint;

                txtAddress.Enabled = txtPort.Enabled = acceptAll.Enabled = btnStart.Enabled = false;
                btnRestart.Enabled = btnStop.Enabled = clientPanel.Enabled = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Name);
            }


            serverTheard = new Thread(StartServer);
            serverTheard.Start();
        }

        private void btnExit_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        [Obsolete]
        private void btnStop_Click(object sender, EventArgs e)
        {
            serverTheard.Suspend();
            server.Stop();
            lbStatus.Text = "Not Activated";
            lbDetail.Caption = "Server stopped";

            btnRestart.Enabled = btnStop.Enabled = false;
            acceptAll.Enabled = txtPort.Enabled = btnStart.Enabled = true;
            if (acceptAll.Checked) txtAddress.Enabled = false;
            else txtAddress.Enabled = true;
        }

        [Obsolete]
        private void btnRestart_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                serverTheard.Suspend();
                server.Stop();

                lbStatus.Text = "Not Activated";
                lbDetail.Caption = "Server stopped";

                server = new TcpListener(host, port);

                // 1. listen
                server.Start();
                lbStatus.Text = "Activated";
                lbDetail.Caption = "Server started on " + server.LocalEndpoint;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Name);
            }


            serverTheard = new Thread(StartServer);
            serverTheard.Start();
        }

        private void acceptAll_CheckedChanged(object sender, EventArgs e)
        {
            switch (acceptAll.Checked)
            {
                case true:
                    txtAddress.Enabled = false;
                    break;
                case false:
                    txtAddress.Enabled = true;
                    break;
            }
        }
    }
}