using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.SPOT;

namespace MyHome.Utilities
{
    public sealed class SocketClient
    {
        private Socket _socketConnection;
        private int _recieveTimeOut;

        public SocketClient()
        {
            _socketConnection = null;
            _recieveTimeOut = 30;
        }

        public bool ConnectSocket(String server, Int32 port)
        {
            try
            {
                if (_socketConnection != null && _socketConnection.Available != 0) //close connection if already active
                {
                    _socketConnection.Close();
                }
                // Get server's IP address.
                IPHostEntry hostEntry = Dns.GetHostEntry(server);
                _socketConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SetRecieveTimeout(5);
                _socketConnection.Connect(new IPEndPoint(hostEntry.AddressList[0], port)); //sometimes gets stuck here if trying to connect
                //may need to check to see if connection is active here, however i think it throws an exception if it cant connect

                return true;
            }
            catch
            {
                Debug.Print("Failed to connect to " + server + ":" + port);
                return false; //all ports already used, unable to resolve hostname, or unable to connect to socket
            }
        }

        public void CloseConnection()
        {
            try
            {
                _socketConnection.Close();
            }
            catch
            {
                Debug.Print("Failed to close connection.");
            }
        }

        public bool SendMessage(string request)
        {
            if (_socketConnection == null)
                return false;
            try
            {
                // Send request to the server.
                Byte[] bytesToSend = Encoding.UTF8.GetBytes(request);
                _socketConnection.Send(bytesToSend, bytesToSend.Length, 0);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool GetMessage(out string message)
        {
            if (_socketConnection == null)
            {
                message = string.Empty;
                return false;
            }
            const Int32 cMicrosecondsPerSecond = 1000000;
            // Accumulates the received page as it is built from the buffer.
            String recieved = String.Empty;
            try
            {
                // Reusable buffer for receiving chunks of the document.
                Byte[] buffer = new Byte[1024];

                // Wait up to 5 seconds for initial data to be available.  Throws 
                // an exception if the connection is closed with no data sent.
                DateTime timeoutAt = DateTime.Now.AddSeconds(_recieveTimeOut);
                while (_socketConnection.Available == 0 && DateTime.Now < timeoutAt)
                {
                    System.Threading.Thread.Sleep(100);
                }

                while (_socketConnection.Poll(_recieveTimeOut * cMicrosecondsPerSecond, SelectMode.SelectRead))
                {
                    // If there are 0 bytes in the buffer, then the connection is 
                    // closed, or we have timed out.
                    if (_socketConnection.Available == 0)
                        break;

                    // Zero all bytes in the re-usable buffer.
                    Array.Clear(buffer, 0, buffer.Length);

                    // Read a buffer-sized chunk.
                    Int32 bytesRead = _socketConnection.Receive(buffer);

                    // Append the chunk to the string.
                    recieved = recieved + new String(Encoding.UTF8.GetChars(buffer));
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                message = recieved;
            }
            return true;
        }

        public void SetRecieveTimeout(int time)
        {
            if (_socketConnection == null) return;
            _socketConnection.ReceiveTimeout = time;
            _recieveTimeOut = time;
        }

        public void SetSendTimeout(int time)
        {
            if (_socketConnection == null) return;
            _socketConnection.SendTimeout = time;
        }

        ~SocketClient()
        {
            if (_socketConnection != null)
            {
                try
                {
                    _socketConnection.Close();
                }
                catch
                {
                    Debug.Print("Failed to close connection on Dispose.");
                }
            }
        }
    }
}
