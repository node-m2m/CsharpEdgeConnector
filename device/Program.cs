using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace serverLib.TcpServerAsync{
      
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }  
      
    public static class AsynchronousSocketListener
    {
        private const int port = 5400;
        private const string ip = "127.0.0.1";
        public static string rvcdData = "";

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        
        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();  
      
            // Get the socket that handles the client request.  
            Socket listener = (Socket) ar.AsyncState;  
            Socket handler = listener.EndAccept(ar);  
      
            // Create the state object.  
            StateObject state = new StateObject();  
            state.workSocket = handler;  
            handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);  
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;  
      
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject) ar.AsyncState;  
            Socket handler = state.workSocket;  

            Random rnd = new Random();
            int rd = rnd.Next(0, 100);

            Console.WriteLine();
      
            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);  
      
            if (bytesRead > 0) {  
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));  
      
                // Check for end-of-file tag. If it is not there, read more data.  
                content = state.sb.ToString();
       
                try{
                    JsonNode jsonData = JsonNode.Parse(content)!; // content with EOF
 
                    var topicProp = jsonData["topic"]; // jsonData.topic
                    Console.WriteLine("jsonData.topic: " + topicProp);

                    if(string.Equals("random-data", topicProp.ToString())){ 
                      jsonData["value"] = rd;
                    }
                    else{
                      Console.WriteLine("invalid payload topic");
                      jsonData["error"] = "invalid payload topic";
                    }
                    content = jsonData.ToJsonString();
                }
                catch(Exception e){
                    Console.WriteLine("Json parse error: " + e.Message);
                }
                finally{
                     Send(handler, content);  
                     Console.WriteLine("json string result:" + content);
                }
            }  
        }

        public static void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer
            IPHostEntry ipHostInfo = Dns.GetHostEntry(ip); 
            IPAddress ipAddress = ipHostInfo.AddressList[0];  
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);  

            Console.WriteLine("\n*** C# Tcp Edge Connector Server ***");
            Console.WriteLine("\nServer listening on: " + ip + ":" + port);
      
            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);  
      
            // Bind the socket to the local endpoint and listen for incoming connections.  
            try {  
                listener.Bind(localEndPoint);  
                listener.Listen(100);  
      
                while (true) {  
                    // Set the event to nonsignaled state.  
                    allDone.Reset();  
      
                    // Start an asynchronous socket to listen for connections.  
                    listener.BeginAccept( new AsyncCallback(AcceptCallback), listener );  
      
                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();  
                }  
      
            } catch (Exception e) {  
                Console.WriteLine(e.ToString());  
            }  
      
            //Console.WriteLine("\nPress ENTER to continue...");  
            Console.Read();  
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);  
      
            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,  
                new AsyncCallback(SendCallback), handler);  
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket) ar.AsyncState;  
      
                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);  
      
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();  
      
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());  
            }  
        }

        public static int Main(String[] args)
        {
            StartListening();  
            return 0;  
        }
    }
}
