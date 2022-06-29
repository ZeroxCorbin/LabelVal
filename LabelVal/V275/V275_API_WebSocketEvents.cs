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
using LabelVal.V275.Models;

namespace LabelVal.V275
{
    public class V275_API_WebSocketEvents
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ClientWebSocket Socket;
        private CancellationTokenSource SocketLoopTokenSource;

        public delegate void MessageRecievedDelegate(string message);
        public event MessageRecievedDelegate MessageRecieved;

        public delegate void InspectionEventDelegate(V275_Events_System ev);

        public event InspectionEventDelegate Heartbeat;
        public event InspectionEventDelegate LabelEnd;
        public event InspectionEventDelegate SetupCapture;
        public event InspectionEventDelegate SessionStateChange;
        public event InspectionEventDelegate StateChange;

        public delegate void SetupDetectDelegate(V275_Events_System ev, bool end);
        public event SetupDetectDelegate SetupDetect;

        public async Task<bool> StartAsync(string wsUri)
            => await StartAsync(new Uri(wsUri));

        public async Task<bool> StartAsync(Uri wsUri)
        {
            SocketLoopTokenSource = new CancellationTokenSource();

            Logger.Info("WS Staring: {uri}", wsUri.OriginalString);

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
            catch (Exception e)
            {
                Logger.Error(e, "WS ConnectAsync Exception");

                return false;
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
            //    else
            //        using (StreamWriter sw = File.AppendText("capture_system.txt"))
            //            sw.WriteLine(message);

            //else if (ev.name != "heartbeat")


            if (ev.name == "heartbeat")
            {
                Heartbeat?.Invoke(ev);
                return;
            }

            using (StreamWriter sw = File.AppendText("capture_node.txt"))
                sw.WriteLine(message);

            if (ev.name == "setupCapture")
            {
                Logger.Debug("WSE: setupCapture {node}; {name}", ev.source, ev.name);
                SetupCapture?.Invoke(ev);
                return;
            }

            if (ev.name.StartsWith("setupDetect"))
            {
                if (ev.name.EndsWith("End"))
                {
                    Logger.Debug("WSE: setupDetect {node}; {name}", ev.source, ev.name);

                    SetupDetect?.Invoke(ev, true);
                    return;
                }
                Logger.Debug("WSE: setupDetect {node}; {name}", ev.source, ev.name);
                SetupDetect?.Invoke(ev, false);
                return;
            }

            if (ev.name == "stateChange")
            {
                Logger.Debug("WSE: stateChange {node}; {name}", ev.source, ev.name);
                StateChange?.Invoke(ev);
                return;
            }

            if (ev.name == "sessionStateChange")
            {
                Logger.Debug("WSE: sessionStateChange {node}; {name}", ev.source, ev.name);
                SessionStateChange?.Invoke(ev);
                return;
            }

            if (ev.name == "labelEnd")
            {
                Logger.Debug("WSE: labelEnd {node}; {name}", ev.source, ev.name);
                LabelEnd?.Invoke(ev);
                return;
            }
        }

        public async Task StopAsync()
        {
            Logger.Info("WS Stopping");

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
            catch (Exception ex)
            {
                Logger.Error(ex, "WS Close Output Async Exception");
            }
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
                Logger.Error(ex, "WS Processing Loop Exception");
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
