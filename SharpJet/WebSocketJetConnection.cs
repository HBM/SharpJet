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
    using System.Timers;
    using WebSocketSharp;

    public class WebSocketJetConnection : IJetConnection, IDisposable
    {
        private WebSocket webSocket;
        private Action<bool> connectCompleted;
        private Timer connectTimer;
        private ConnectionState connectionState;

        public WebSocketJetConnection(string url)
        {
            this.connectionState = ConnectionState.closed;
            this.webSocket = new WebSocket(url, "jet");
            this.webSocket.OnOpen += this.OnOpen;
            this.webSocket.OnClose += this.OnClose;
            this.webSocket.OnError += this.OnError;
            this.webSocket.OnMessage += this.OnMessage;
        }

        public event EventHandler<StringEventArgs> HandleIncomingMessage;

        private enum ConnectionState
        {
            closed,
            connected,
            closing
        }

        public bool IsConnected
        {
            get
            {
                return this.connectionState == ConnectionState.connected;
            }
        }

        public void Connect(Action<bool> completed, double timeoutMs)
        {
            lock (this)
            {
                if (this.IsConnected)
                {
                    throw new JetPeerException("Websocket already connected");
                }

                this.connectCompleted = completed;
                this.connectTimer = new Timer(timeoutMs);
                this.connectTimer.Elapsed += this.OnOpenElapsed;
                this.connectTimer.AutoReset = false;
                this.connectTimer.Enabled = true;
                this.webSocket.ConnectAsync();
            }
        }

        public void Disconnect()
        {
            lock (this)
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
            lock (this)
            {
                if (!this.IsConnected)
                {
                    throw new JetPeerException("Websocket disconnected");
                }

                this.webSocket.Send(json);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    if (this.IsConnected)
                    {
                        this.webSocket.Close(WebSocketSharp.CloseStatusCode.Away);
                    }
                }
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            lock (this)
            {
                this.connectTimer.Stop();
                this.connectionState = ConnectionState.connected;

                if (this.connectCompleted != null)
                {
                    this.connectCompleted(this.webSocket.IsAlive);
                }
            }
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            lock (this)
            {
                this.connectionState = ConnectionState.closed;
            }
        }

        private void OnOpenElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (this)
            {
                this.connectTimer.Stop();
                if (!this.webSocket.IsAlive)
                {
                    this.webSocket.Close();
                    if (this.connectCompleted != null)
                    {
                        this.connectCompleted(this.webSocket.IsAlive);
                    }
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
