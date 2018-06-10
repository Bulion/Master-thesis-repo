/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// this class requires .NET 4.5+ to compile and Windows 8+ to work

using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Text;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

namespace RosSharp.RosBridgeClient.Protocols
{
    public class WebSocketUWPProtocol : IProtocol
    {
        public event EventHandler OnReceive;

        private string Uri;
        private const int bufferSize = 1024;
#if WINDOWS_UWP
        private MessageWebSocket messageWebSocket;
        private DataWriter messageWriter;
#endif

        public WebSocketUWPProtocol(string uri)
        {
#if WINDOWS_UWP
            messageWebSocket = new MessageWebSocket();
            messageWebSocket.Control.MessageType = SocketMessageType.Binary;
            messageWebSocket.MessageReceived += MessageReceived;
            messageWebSocket.Closed += Closed;
            Uri = uri;
#endif
        }

        public void Connect()
        {
#if WINDOWS_UWP
            messageWebSocket.ConnectAsync(new Uri(Uri));
            messageWriter = new DataWriter(messageWebSocket.OutputStream);
#endif
        }

        public void Close()
        {
#if WINDOWS_UWP
            messageWebSocket.Close(1, "closed by user");
#endif
        }

#if WINDOWS_UWP
        public void Closed(IWebSocket webSocket, WebSocketClosedEventArgs args)
        {
            messageWebSocket.Close(1, "closed external");
        }
#endif

        public bool IsAlive()
        {
            return true;
        }

        public void Send(byte[] data)
        {
#if WINDOWS_UWP
            try
            {
                messageWriter.WriteBytes(data);
                messageWriter.StoreAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Cos poszlo nie tak z wysylaniem");
            }
#endif
        }

        //private async void StartListen()
        //{
        //    while (ClientWebSocket.State == WebSocketState.Open)
        //    {
        //        var buffer = new byte[bufferSize];
        //        var message = new byte[0];

        //        WebSocketReceiveResult result;
        //        do
        //        {
        //            result = await ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //            message = message.Concat(buffer).ToArray();
        //        } while (!result.EndOfMessage);

        //        if (result.MessageType == WebSocketMessageType.Close)
        //            await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        //        else
        //            OnReceive.Invoke(this, new MessageEventArgs(message));
        //    }
        //}

#if WINDOWS_UWP
        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string read = reader.ReadString(reader.UnconsumedBufferLength);
#if DEBUG
                    Debug.WriteLine("Odczytalem: " + read);
#endif
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(read);
                    OnReceive.Invoke(this, new MessageEventArgs(bytes));
                }
            }
            catch (Exception e)
            {

            }
        }
#endif

    }
}
