using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using FireSharp.Config;
using FireSharp.Interfaces;
using System.Threading;
using static WindowsFormsApp1.Server;

namespace WindowsFormsApp1
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }
        IFirebaseConfig config = new FirebaseConfig()
        {
            AuthSecret = "TQ9okrz0EmBOl0zMIrtvZJfmpxMQ00cnZtYdm0vn",
            BasePath = "https://daltm-fd12b-default-rtdb.firebaseio.com/"
        };
        IFirebaseClient client;
        public register BestMatch { get; set; }
        public async void Sendmes(string mes, string str)
        {

            register bestMatchUser = BestMatch;
            string senderUserId = str.Replace('.', ','); // ID người gửi tin nhắn
            string receiverUserId = "";
            if (bestMatchUser == null)
            {
                var res = client.Get($"chats");
                var mess = res.ResultAs<Dictionary<string, Message>>();

                foreach (var message in mess)
                {
                    string[] node = message.Key.Split('-');

                    if (node[0] == senderUserId)
                    {
                        receiverUserId = node[1]; // Chuyển đổi lại định dạng Email
                        break; // Tìm thấy người nhận, thoát khỏi vòng lặp
                    }
                    else if (node[1] == senderUserId)
                    {
                        receiverUserId = senderUserId;
                        senderUserId = node[0];// Chuyển đổi lại định dạng Email
                        break; // Tìm thấy người nhận, thoát khỏi vòng lặp
                    }
                }
            }
            // Đặt node tin nhắn theo định dạng "{senderUserId}-{receiverUserId}"
            else
            {
                receiverUserId = bestMatchUser.Email.Replace('.', ','); // ID người nhận tin nhắn

            }
            string chatNode = $"{senderUserId}-{receiverUserId}";
            //Lưu tin nhắn trên database
            var tn = new Message
            {
                sender = str,
                zContent = mes
            };

            var response = await client.PushAsync($"chats/{chatNode}", tn);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {

            }
            else
            {
                MessageBox.Show("An error occurred while sending the message!");
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                client = new FireSharp.FirebaseClient(config);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "check your tk!");
            }
        }

        public class register
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Sex { get; set; }
            public string With { get; set; }
            public string Password { get; set; }
            public int Dating { get; set; }
            public int Age { get; set; }
            public List<string> Favorites { get; set; }
        }
        public class Message
        {
            public string sender { get; set; }
            public string zContent { get; set; }
        }
        Socket serverSocket;
        Socket clientSocket;
        private List<Socket> connectedClients = new List<Socket>(); // Danh sách các kết nối client
        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add("Server listen ....");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8085;
            serverSocket.Bind(new IPEndPoint(ipAddress, port));
            serverSocket.Listen(10);
            // Tạo và khởi động một luồng mới cho việc lắng nghe từ client
            Thread listenThread = new Thread(() =>
            {
                while (true) // Vòng lặp vô hạn để tiếp tục lắng nghe
                {
                    try
                    {
                        Socket clientSocket = serverSocket.Accept();
                        // Lấy địa chỉ IP và cổng của client
                        string clientAddress = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                        int clientPort = ((IPEndPoint)clientSocket.RemoteEndPoint).Port;
                        // Thêm vào listBox1
                        listBox1.Invoke(new Action(() =>
                        {
                            listBox1.Items.Add("Client connected from: " + clientAddress + ":" + clientPort);
                        }));
                        // Thêm kết nối client vào danh sách các kết nối
                        connectedClients.Add(clientSocket);

                        // Tạo và khởi động một luồng mới để xử lý việc giao tiếp với client
                        Thread handleClientThread = new Thread(() =>
                        {
                            Client(clientSocket, clientAddress, clientPort);
                        });

                        handleClientThread.Start();
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }
            });
            listenThread.Start();
        }

        private void Client(Socket clientSocket, string clientAddress, int clientPort)
        {
            try
            {
                while (true)
                {
                    // Xử lý việc nhận tin nhắn từ client,
                    byte[] receiveData = new byte[1024];
                    int bytesRead = clientSocket.Receive(receiveData);
                    string Message = Encoding.ASCII.GetString(receiveData, 0, bytesRead);
                    byte[] receiveData2 = new byte[1024];
                    int bytesRead2 = clientSocket.Receive(receiveData2);
                    string Send = Encoding.ASCII.GetString(receiveData2, 0, bytesRead2);
                    listBox1.Invoke(new Action(() =>
                    {
                        listBox1.Items.Add(Send);
                        listBox1.Items.Add(Message);

                    }));
                    if(Message == "NULL")
                    {
                        break;
                    }

                    Sendmes(Message, Send);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR!!!! {ex}");
            }
            finally
            {
                // Đóng kết nối với client khi client ngắt kết nối
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();

                // Xóa kết nối client khỏi danh sách các kết nối
                connectedClients.Remove(clientSocket);

                listBox1.Invoke(new Action(() =>
                {
                    listBox1.Items.Add("Client disconnected: " + clientAddress + ":" + clientPort);
                }));
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Đóng serverSocket nếu đang mở
            if (serverSocket != null)
            {
                serverSocket.Close();
            }
            // Đóng tất cả các kết nối client đang mở
            foreach (Socket clientSocket in connectedClients)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
        }
    }
}
