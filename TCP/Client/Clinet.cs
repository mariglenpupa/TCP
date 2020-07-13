using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Clinet : Form
    {
        public Clinet()
        {
            InitializeComponent();
	    Thread.Sleep(100);
	    Start();
        }
        TcpClient tcpclnt;
        Thread thread;
        private void button2_Click(object sender, EventArgs e)
        {
            //while (true)
            //{
                Start();
            //}
        }
	void Start()
	{
		try
                {
                    tcpclnt = new TcpClient();

                    tcpclnt.Connect("127.0.0.1", 4444);
                    // use the ipaddress as in the server program

                    listBox1.Items.Add("Connected");
                    Send("Info~Dde32d|127.0.0.1|USA|Dde32d|daniel|Win10 build...|Win defender|0.1 Beta|N/A|Dell Workstation");
                    thread = new Thread(GET);
                    thread.IsBackground = true;
                    thread.Start();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.StackTrace); thread.Abort(); }
                Thread.Sleep(2000);
	}

        private void button1_Click(object sender, EventArgs e)
        {
            Send(textBox1.Text);
        }
        void GET()
        {
            while(true)
            {
                Stream stm = tcpclnt.GetStream();
                byte[] bb = new byte[100];
                int k = stm.Read(bb, 0, 100);
                string msg = "";
                for (int i = 0; i < k; i++)
                    msg += Convert.ToChar(bb[i]);
                    updateUI(() => listBox1.Items.Add(msg));
            }
        }

        void Send(string text)
        {
            Stream stm = tcpclnt.GetStream();

            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(text);
            updateUI(() => listBox1.Items.Add("Sending: " + text));

            stm.Write(ba, 0, ba.Length);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        //ui update
        private void updateUI(Action action)
        {
            this.Invoke(new Action(action), null);
        }
    }
}
