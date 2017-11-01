// <copyright file="WebSocketJetConnection.cs" company="Hottinger Baldwin Messtechnik GmbH">
//
// SharpJet, a library to communicate with Jet IPC.
//
// The MIT License (MIT)
//
// Copyright (C) Hottinger Baldwin Messtechnik GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// </copyright>

namespace Hbm.Devices.Jet
{
    using System;
    using System.Net.Security;
    using WebSocketSharp;
    using Hbm.Devices.Jet.Utils;

    public class WebSocketJetConnection : DisposableBase, IJetConnection
    {
        private bool isDisposed;
        private readonly object lockObject = new object();
        internal IWebSocket WebSocket { get; private set; }
        internal ITimer ConnectTimer { get; set; }
        private Action<bool> connectCompleted;
        private ConnectionState connectionState;

        public WebSocketJetConnection(string url)
        {
            this.connectionState = ConnectionState.closed;
            SetWebSocket(new WebSocketAdapter(url, "jet"));
            this.ConnectTimer = new TimerAdapter();
        }

        public WebSocketJetConnection(string url, RemoteCertificateValidationCallback certificationCallback)
            : this(url)
        {
            if (certificationCallback != null)
            {
                this.WebSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls;
                this.WebSocket.SslConfiguration.ServerCertificateValidationCallback = certificationCallback;
            }
        }
        
        public event EventHandler<StringEventArgs> HandleIncomingMessage;

        public bool IsConnected => this.connectionState == ConnectionState.connected;

        public void Connect(Action<bool> completed, double timeoutMs)
        {
            lock (lockObject)
            {
                if (this.IsConnected)
                {
                    throw new JetPeerException("Websocket already connected");
                }
                SubscribeWebSocket();
                this.connectCompleted = completed;
                this.ConnectTimer.Interval = timeoutMs;
                this.ConnectTimer.Elapsed += this.OnOpenElapsed;
                this.ConnectTimer.AutoReset = false;
                this.ConnectTimer.Start();
                this.WebSocket.ConnectAsync();
            }
        }

        public void Disconnect()
        {
            lock (lockObject)
            {
                if (!this.IsConnected)
                {
                    throw new JetPeerException("disconnecting an already disconnected websocket");
                }

                this.connectionState = ConnectionState.closing;
                this.WebSocket.CloseAsync(WebSocketSharp.CloseStatusCode.Away);
            }
        }

        public void SendMessage(string json)
        {
            lock (lockObject)
            {
                if (!this.IsConnected)
                {
                    throw new JetPeerException("Websocket disconnected");
                }

                this.WebSocket.Send(json);
            }
        }

        internal void SetWebSocket(IWebSocket webSocket)
        {
            //if (this.WebSocket != null)
            //{
            //    UnsubscribeWebSocket();
            //}

            this.WebSocket = webSocket;
            this.WebSocket.SslConfiguration.ServerCertificateValidationCallback = delegate { return true; };
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                lock (lockObject)
                {
                    if (this.IsConnected)
                    {
                        this.WebSocket.Close(WebSocketSharp.CloseStatusCode.Away);
                    }
                    UnsubscribeWebSocket();
                    WebSocket.Dispose();

                    if (ConnectTimer.Enabled)
                    {
                        ConnectTimer.Stop();
                    }
                    ConnectTimer.Dispose();
                }
            }

            isDisposed = true;
        }

        private void UnsubscribeWebSocket()
        {
            WebSocket.OnOpen -= this.OnOpen;
            WebSocket.OnClose -= this.OnClose;
            WebSocket.OnMessage -= this.OnMessage;
        }

        private void SubscribeWebSocket()
        {
            UnsubscribeWebSocket();
            WebSocket.OnOpen += this.OnOpen;
            WebSocket.OnClose += this.OnClose;
            WebSocket.OnMessage += this.OnMessage;
        }

        private void OnOpen(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                this.ConnectTimer.Stop();
                this.ConnectTimer.Elapsed -= this.OnOpenElapsed;
                this.connectionState = ConnectionState.connected;

                if (this.connectCompleted != null)
                {
                    this.connectCompleted(this.WebSocket.IsAlive);
                }
            }
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            lock (lockObject)
            {
                this.connectionState = ConnectionState.closed;
            }
        }

        private void OnOpenElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (lockObject)
            {
                this.ConnectTimer.Stop();
                UnsubscribeWebSocket();
                this.ConnectTimer.Elapsed -= this.OnOpenElapsed;
                if (this.WebSocket.IsAlive)                 
                {
                    this.WebSocket.Close();
                }

                if (this.connectCompleted != null)
                {
                    this.connectCompleted(this.WebSocket.IsAlive);
                }
            }
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            if ((this.HandleIncomingMessage != null) && e.IsText)
            {
                this.HandleIncomingMessage(this, new StringEventArgs(e.Data));
            }
        }
    }
}
