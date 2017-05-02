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
        private IWebSocket webSocket;
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
                this.webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls;
                this.webSocket.SslConfiguration.ServerCertificateValidationCallback = certificationCallback;
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

                this.connectCompleted = completed;
                this.ConnectTimer.Interval = timeoutMs;
                this.ConnectTimer.Elapsed += this.OnOpenElapsed;
                this.ConnectTimer.AutoReset = false;
                this.ConnectTimer.Start();
                this.webSocket.ConnectAsync();
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
                this.webSocket.CloseAsync(WebSocketSharp.CloseStatusCode.Away);
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

                this.webSocket.Send(json);
            }
        }

        internal void SetWebSocket(IWebSocket webSocket)
        {
            if (this.webSocket != null)
            {
                UnsubscribeWebSocket(this.webSocket);
            }

            this.webSocket = webSocket;
            SubscribeWebSocket(this.webSocket);
            this.webSocket.SslConfiguration.ServerCertificateValidationCallback = delegate { return false; };
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
                        this.webSocket.Close(WebSocketSharp.CloseStatusCode.Away);
                    }
                    UnsubscribeWebSocket(webSocket);
                    webSocket.Dispose();

                    if (ConnectTimer.Enabled)
                    {
                        ConnectTimer.Stop();
                    }
                    ConnectTimer.Dispose();
                }
            }

            isDisposed = true;
        }

        private void UnsubscribeWebSocket(IWebSocket webSocket)
        {
            webSocket.OnOpen -= this.OnOpen;
            webSocket.OnClose -= this.OnClose;
            webSocket.OnError -= this.OnError;
            webSocket.OnMessage -= this.OnMessage;
        }

        private void SubscribeWebSocket(IWebSocket webSocket)
        {
            webSocket.OnOpen += this.OnOpen;
            webSocket.OnClose += this.OnClose;
            webSocket.OnError += this.OnError;
            webSocket.OnMessage += this.OnMessage;
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
                    this.connectCompleted(this.webSocket.IsAlive);
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
                this.ConnectTimer.Elapsed -= this.OnOpenElapsed;
                if (this.webSocket.IsAlive)                 
                {
                    this.webSocket.Close();
                }

                if (this.connectCompleted != null)
                {
                    this.connectCompleted(this.webSocket.IsAlive);
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

        private void OnError(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
