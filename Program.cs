using System; 
using System.Text; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Threading; 


namespace sockettest {
    class Program {
        static byte[] m_result = new byte[1024]; 
        const int m_port = 9999; 
        static string m_localIp = "0.0.0.0"; 
        static Socket m_serverSocket; 
        static List < Socket > m_clientSocketList = new List < Socket > (); 
        static void Main(string[] args) {
            IPAddress ipAddress = IPAddress.Parse(m_localIp); 
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, m_port); 
            m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
            m_serverSocket.Bind(ipEndPoint); 
            m_serverSocket.Listen(10); 
            Console.WriteLine("Start listening {0} success", m_serverSocket.LocalEndPoint.ToString()); 
            Thread thread = new Thread(ClientConnectListen); 
            thread.Start(); 
            Console.ReadLine(); 
        }
 

        static void ClientConnectListen() {
            while (true) {
                Socket clientSocket = m_serverSocket.Accept(); 
                m_clientSocketList.Add(clientSocket); 
                Console.WriteLine("client {0} connected success", clientSocket.RemoteEndPoint.ToString()); 
 
                NetBufferWriter writer = new NetBufferWriter(); 
                writer.WriteString("Connected Server Success:" + m_clientSocketList.Count.ToString() + "\n"); 
                clientSocket.Send(writer.Finish()); 
 
                Thread thread = new Thread(RecieveMessage); 
                thread.Start(clientSocket); 
            }
        }
 
        static void RecieveMessage(object clientSocket) {
            Socket mClientSocket = (Socket)clientSocket; 
            while (true) {
                try {
                    int receiveNumber = mClientSocket.Receive(m_result); 
                    Console.WriteLine("Recived client {0} message, len={1}", mClientSocket.RemoteEndPoint.ToString(), receiveNumber); 
                    
                    if (receiveNumber == 0) {
                        Console.WriteLine("Disconnect client {0}", mClientSocket.RemoteEndPoint.ToString()); 
                        RemoveClientSocket(mClientSocket); 
                        break; 
                    }
 
                    string data = Encoding.UTF8.GetString(m_result, 0, receiveNumber); 
                    Console.WriteLine("Recived message:{0}", data); 
                    NetBufferWriter writer = new NetBufferWriter(); 

                    if (data.Contains("HEART_BEAT")) {
                        writer.WriteString("HEART_BEAT\n"); 
                        mClientSocket.Send(writer.Finish()); 	
                    }		
                    else {
                        writer.WriteString(data); 
                        foreach (Socket socket in m_clientSocketList) {
                            if (socket.RemoteEndPoint.ToString() != mClientSocket.RemoteEndPoint.ToString()) {
                                socket.Send(writer.Finish()); 
                            }
                        }
                    }
                }catch(Exception ex) {
                    Console.WriteLine(ex.Message); 
                    RemoveClientSocket(mClientSocket); 
                    break; 
                }
            }
        }
 
        static void RemoveClientSocket(Socket clientSocket) {
            clientSocket.Shutdown(SocketShutdown.Both); 
            clientSocket.Close(); 
            m_clientSocketList.Remove(clientSocket); 
        }
    }
}
