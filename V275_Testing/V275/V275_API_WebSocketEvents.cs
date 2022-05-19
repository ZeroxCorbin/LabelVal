using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using V275_Testing.V275.Models;

namespace V275_Testing.V275
{
    public class V275_API_WebSocketEvents
    {
        private ClientWebSocket Socket;
        private CancellationTokenSource SocketLoopTokenSource;

        public delegate void MessageRecievedDelegate(string message);
        public event MessageRecievedDelegate MessageRecieved;

        public delegate void HeartbeatDelegate(V275_Events_System ev);
        public event HeartbeatDelegate Heartbeat;

        public delegate void SetupCaptureDelegate(V275_Events_System ev);
        public event SetupCaptureDelegate SetupCapture;

        public delegate void SessionStateChangeDelegate(V275_Events_System ev);
        public event SessionStateChangeDelegate SessionStateChange;

        public async Task<bool> StartAsync(string wsUri)
            => await StartAsync(new Uri(wsUri));

        public async Task<bool> StartAsync(Uri wsUri)
        {
            SocketLoopTokenSource = new CancellationTokenSource();

            try
            {
                Socket = new ClientWebSocket();
                await Socket.ConnectAsync(wsUri, CancellationToken.None);

                if (Socket.State == WebSocketState.Open)
                {
                    _ = Task.Run(() => SocketProcessingLoopAsync().ConfigureAwait(false));

                    MessageRecieved -= V275_API_WebSocketEvents_MessageRecieved;
                    MessageRecieved += V275_API_WebSocketEvents_MessageRecieved;
                    return true;
                }
                else
                    return false;
            }
            catch (OperationCanceledException)
            {
                return false;
                // normal upon task/token cancellation, disregard
            }
        }

        private void V275_API_WebSocketEvents_MessageRecieved(string message)
        {
            string tmp;
            tmp = message.Remove(2, 15);
            tmp = tmp.Remove(tmp.LastIndexOf('}'), 1);
            V275_Events_System ev = JsonConvert.DeserializeObject<V275_Events_System>(tmp);

            if (ev.source == "system")
                if (ev.name == "heartbeat")
                    return;
                else
                    using (StreamWriter sw = File.AppendText("capture_system.txt"))
                        sw.WriteLine(message);

            else if (ev.name != "heartbeat")
                using (StreamWriter sw = File.AppendText("capture_node.txt"))
                    sw.WriteLine(message);

            if (ev.name == "heartbeat")
            {
                Heartbeat?.Invoke(ev);
                return;
            }

            if (ev.name == "setupCapture")
            {
                //JObject obj = (JObject)JsonConvert.DeserializeObject(message);
                SetupCapture?.Invoke(ev);
                return;
            }

            if (ev.name == "sessionStateChange")
            {
                //JObject obj = (JObject)JsonConvert.DeserializeObject(message);
                SessionStateChange?.Invoke(ev);
                return;
            }
        }

        public async Task StopAsync()
        {
            if (Socket == null || Socket.State != WebSocketState.Open) return;
            // close the socket first, because ReceiveAsync leaves an invalid socket (state = aborted) when the token is cancelled
            var timeout = new CancellationTokenSource(5000);
            try
            {
                // after this, the socket state which change to CloseSent
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                // now we wait for the server response, which will close the socket
                while (Socket != null && Socket.State != WebSocketState.Closed && !timeout.Token.IsCancellationRequested) ;
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch { }
            // whether we closed the socket or timed out, we cancel the token causing RecieveAsync to abort the socket
            SocketLoopTokenSource.Cancel();
            // the finally block at the end of the processing loop will dispose and null the Socket object
        }

        public WebSocketState State
        {
            get => Socket?.State ?? WebSocketState.None;
        }

        private async Task SocketProcessingLoopAsync()
        {
            var cancellationToken = SocketLoopTokenSource.Token;
            try
            {
                var buffer = WebSocket.CreateClientBuffer(4096, 4096);
                while (Socket.State != WebSocketState.Closed && !cancellationToken.IsCancellationRequested)
                {
                    var receiveResult = await Socket.ReceiveAsync(buffer, cancellationToken);
                    // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // the server is notifying us that the connection will close; send acknowledgement
                        if (Socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                        }

                        // display text or binary data
                        if (Socket.State == WebSocketState.Open && receiveResult.MessageType != WebSocketMessageType.Close)
                        {
                            string message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                            if (message.Length > 1)
                                MessageRecieved?.Invoke(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                //Program.ReportException(ex);
            }
            finally
            {
                MessageRecieved -= V275_API_WebSocketEvents_MessageRecieved;

                if (Socket != null)
                {
                    Socket.Dispose();
                    Socket = null;
                }

            }
        }
    }
}
