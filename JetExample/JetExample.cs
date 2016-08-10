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

using System;
using System.Threading;
using Hbm.Devices.Jet;
using Newtonsoft.Json.Linq;

namespace JetExample
{
    class JetExample
    {
        static void Main(string[] args)
        {
            var example = new JetExample();
            Console.ReadKey(true);
        }

        private JetPeer peer;
        private const string stateName = "theState";
        private int counter;

        JetExample()
        {
            var connection = new WebSocketJetConnection("wss://172.19.1.1");
            //var connection = new WebSocketJetConnection("ws://172.19.1.1:11123");
            peer = new JetPeer(connection);
            peer.Connect(OnConnect, new TimeSpan(0, 0, 20));
        }

        private void OnConnect(bool completed)
        {
            if (completed)
            {
                Console.WriteLine("Successfully connected to Jet daemon!");
                JValue stateValue = new JValue(12);
                peer.Add(stateName, stateValue, StateCallback, AddResponseCallback);
            }
        }

        private JToken StateCallback(string path, JToken value)
        {
            return new JValue(42);
        }

        private void AddResponseCallback(JToken response)
        {
            Console.WriteLine("State \"" + stateName + "\" successfully added!");
            counter = 0;
            var val = new JValue(counter);
            peer.Change(stateName, val, ChangeResponseCallback);
        }

        private void ChangeResponseCallback(JToken response)
        {
            Console.WriteLine(response);
            counter++;
            if (counter < 10)
            {
                Thread.Sleep(1000);
                JValue val = new JValue(counter);
                peer.Change(stateName, val, ChangeResponseCallback);
            }
            else
            {
                peer.Remove(stateName, RemoveStateCallback);
            }
        }

        private void RemoveStateCallback(JToken response)
        {
            Console.WriteLine("State \"" + stateName + "\" successfully removed!");
        }
    }
}
