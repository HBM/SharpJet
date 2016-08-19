// <copyright file="SocketJetConnection.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Timers;

    internal enum PeerOperation
    {
        READ_LENGTH,
        READ_MESSAGE
    }

    public class SocketJetConnection : IJetConnection
    {
        private static readonly int ReceivebufferSize = 20000;
        private byte[] receiveBuffer = new byte[ReceivebufferSize];
        private int currentReadIndex = 0;
        private int currentWriteIndex = 0;

        private PeerOperation operation = PeerOperation.READ_LENGTH;

        private IPAddress address;
        private int port;
        private ConnectionState connectionState;
        private Timer connectTimer;
        private Socket client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Action<bool> connectCompleted;

        private bool enoughDataInBuffer = true;
        private int messageLength;

        public SocketJetConnection(IPAddress address, int port)
        {
            this.address = address;
            this.port = port;
            this.connectionState = ConnectionState.closed;
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
                IPEndPoint remoteEP = new IPEndPoint(this.address, this.port);
                this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.connectTimer = new Timer(timeoutMs);
                this.connectTimer.Elapsed += this.OnOpenElapsed;
                this.connectTimer.AutoReset = false;
                this.connectTimer.Enabled = true;
                this.client.BeginConnect(remoteEP, new AsyncCallback(this.ConnectCallback), null);
            }
        }

        public void Disconnect()
        {
            lock (this)
            {
                if (!this.IsConnected)
                {
                    throw new JetPeerException("disconnecting an already disconnected socket");
                }

                this.connectionState = ConnectionState.closing;
                this.client.BeginDisconnect(false, this.DisconnectCallback, null);
            }
        }

        public void SendMessage(string json)
        {
            lock (this)
            {
                if (!this.IsConnected)
                {
                    throw new JetPeerException("socket disconnected");
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            int length = IPAddress.HostToNetworkOrder(buffer.Length);
            SocketAsyncEventArgs buf = new SocketAsyncEventArgs();
            var list = new List<ArraySegment<byte>>();
            list.Add(new ArraySegment<byte>(BitConverter.GetBytes(length)));
            list.Add(new ArraySegment<byte>(buffer));
            buf.BufferList = list;
            this.client.SendAsync(buf);
        }

        private int DataInBuffer()
        {
            return this.currentWriteIndex - this.currentReadIndex;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            lock (this)
            {
                try
                {
                    this.connectTimer.Stop();
                    this.client.EndConnect(ar);
                    this.connectionState = ConnectionState.connected;
                    this.stream = new NetworkStream(this.client);
                    this.writer = new StreamWriter(this.stream);
                    this.reader = new StreamReader(this.stream);
                    this.client.BeginReceive(this.receiveBuffer, 0, ReceivebufferSize, 0, new AsyncCallback(this.ReceiveLength), null);
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                if (this.connectCompleted != null)
                {
                    this.connectCompleted(this.client.Connected);
                }
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            lock (this)
            {
                try
                {
                    this.client.EndDisconnect(ar);
                    this.client.Close();
                }
                catch (SocketException)
                {
                }

                this.connectionState = ConnectionState.closed;
            }
        }

        private void OnOpenElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (this)
            {
                this.connectTimer.Stop();
                if (!this.IsConnected)
                {
                    this.client.Disconnect(false);
                    this.client.Close();
                    if (this.connectCompleted != null)
                    {
                        this.connectCompleted(false);
                    }
                }
            }
        }

        private void ReceiveLength(IAsyncResult ar)
        {
            int bytesRead = this.client.EndReceive(ar);
            this.enoughDataInBuffer = true;
            if (bytesRead <= 0)
            {
                this.client.Disconnect(false);
                this.client.Close();
                this.connectionState = ConnectionState.closed;
                return;
            }
            else
            {
                this.currentWriteIndex += bytesRead;
            }

            while (this.enoughDataInBuffer)
            {
                switch (this.operation)
                {
                    case PeerOperation.READ_LENGTH:
                        if (this.DataInBuffer() >= 4)
                        {
                            int length = BitConverter.ToInt32(this.receiveBuffer, this.currentReadIndex);
                            this.currentReadIndex += 4;
                            this.messageLength = IPAddress.NetworkToHostOrder(length);
                            if (this.messageLength + 4 > ReceivebufferSize)
                            {
                                // handle error: log, close socket etc.
                            }

                            this.operation = PeerOperation.READ_MESSAGE;
                        }
                        else
                        {
                            this.enoughDataInBuffer = false;
                        }

                        break;

                    case PeerOperation.READ_MESSAGE:
                        if (this.DataInBuffer() >= this.messageLength)
                        {
                            string json = Encoding.UTF8.GetString(this.receiveBuffer, this.currentReadIndex, this.messageLength);
                            this.currentReadIndex += this.messageLength;
                            int remainingData = this.DataInBuffer();
                            if (remainingData > 0)
                            {
                                Buffer.BlockCopy(this.receiveBuffer, this.currentReadIndex, this.receiveBuffer, 0, remainingData);
                                this.currentWriteIndex = remainingData;
                            }
                            else
                            {
                                this.currentWriteIndex = 0;
                            }

                            this.currentReadIndex = 0;
                            this.operation = PeerOperation.READ_LENGTH;

                            if (this.HandleIncomingMessage != null)
                            {
                                this.HandleIncomingMessage(this, new StringEventArgs(json));
                            }
                        }
                        else
                        {
                            this.enoughDataInBuffer = false;
                        }

                        break;
                }
            }

            try
            {
                this.client.BeginReceive(this.receiveBuffer, this.currentWriteIndex, ReceivebufferSize - this.DataInBuffer(), 0, new AsyncCallback(this.ReceiveLength), null);
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
