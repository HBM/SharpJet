// <copyright file="JetState.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

namespace JetExamples
{
    using System;
    using System.Net;
    using System.Threading;
    using Hbm.Devices.Jet;
    using Newtonsoft.Json.Linq;

    class JetState
    {
        static void Main(string[] args)
        {
            var example = new JetState();
            Console.ReadKey(true);
        }

        private JetPeer peer;
        private const string stateName = "theState";
        private int counter;

        JetState()
        {
            //var connection = new WebSocketJetConnection("wss://172.19.1.1");
            // var connection = new WebSocketJetConnection("ws://172.19.1.1:11123");
            IPAddress[] ips;
            ips = Dns.GetHostAddresses("172.19.1.1");
            var connection = new SocketJetConnection(ips[0], 11122);
            peer = new JetPeer(connection);
            peer.Connect(OnConnect, 5000);
        }

        private void OnConnect(bool completed)
        {
            if (completed)
            {
                Console.WriteLine("Successfully connected to Jet daemon!");
                JValue stateValue = new JValue(12);
                peer.Add(stateName, stateValue, StateCallback, AddResponseCallback, 5000);
            }
            else
            {
                Console.WriteLine("Connection to Jet daemon failed!");
            }
        }

        private JToken StateCallback(string path, JToken value)
        {
            return new JValue(42);
        }

        private void AddResponseCallback(bool completed, JToken response)
        {
            Console.WriteLine("State \"" + stateName + "\" successfully added!");
            counter = 0;
            var val = new JValue(counter);
            peer.Change(stateName, val, ChangeResponseCallback, 5000);
        }

        private void ChangeResponseCallback(bool completed, JToken response)
        {
            Console.WriteLine(response);
            counter++;
            if (counter < 10)
            {
                Thread.Sleep(1000);
                JValue val = new JValue(counter);
                peer.Change(stateName, val, ChangeResponseCallback, 5000);
            }
            else
            {
                peer.Remove(stateName, RemoveStateCallback, 5000);
                peer.Disconnect();
            }
        }

        private void RemoveStateCallback(bool completed, JToken response)
        {
            Console.WriteLine("State \"" + stateName + "\" successfully removed!");
        }
    }
}
