// <copyright file="JetExample.cs" company="Hottinger Baldwin Messtechnik GmbH">
//
// CS Jet, a library to communicate with Jet IPC.
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

using Hbm.Devices.Jet;
using Newtonsoft.Json.Linq;
using System;

namespace JetExample
{
    class JetExample
    {
        static void Main(string[] args)
        {
            var example = new JetExample();
            Console.ReadKey(true);
            /*
            var connection = new WebSocketJetConnection("wss://172.19.1.1");
            //var connection = new WebSocketJetConnection("ws://172.19.1.1:11123");
            var peer = new JetPeer(connection);
            peer.connect(OnConnect, new TimeSpan(0, 0, 20));
            Thread.Sleep(1000);

            JValue stateValue = new JValue(12);
            peer.add("theState", stateValue, StateCallback, ResponseCallback);
            // Thread.Sleep(500000);

            for (int i = 0; i < 10; i++)
            {
                JValue val = new JValue(i);
                peer.change("theState", val, ResponseCallback);
                Thread.Sleep(1000);
            }

            Thread.Sleep(5000);
            peer.remove("theState", ResponseCallback);
            Thread.Sleep(5000);
            */
        }

        private JetPeer peer;

        JetExample()
        {
            var connection = new WebSocketJetConnection("wss://172.19.1.1");
            //var connection = new WebSocketJetConnection("ws://172.19.1.1:11123");
            peer = new JetPeer(connection);
            peer.Connect(OnConnect, new TimeSpan(0, 0, 20));

        }

        private void OnConnect(bool completed)
        {
            Console.WriteLine(completed);
        }

        public static JToken StateCallback(string path, JToken value)
        {
            return new JValue(42);
        }

        public static void ResponseCallback(JToken response)
        {
            Console.WriteLine("Got response!");
            Console.WriteLine(response);
        }
    }
}
