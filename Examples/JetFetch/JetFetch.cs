// <copyright file="JetFetch.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

    public class JetFetch
    {
        private JetPeer peer;
        private FetchId fetchId;
        private Timer timer;

        private JetFetch()
        {
            // var connection = new WebSocketJetConnection("wss://172.19.1.1");
            // var connection = new WebSocketJetConnection("ws://172.19.1.1:11123");
            var ips = Dns.GetHostAddresses("172.19.1.1");
            var connection = new SocketJetConnection(ips[0], 11122);
            this.peer = new JetPeer(connection);
            this.peer.Connect(this.OnConnect, 5000);
        }

        public static void Main(string[] args)
        {
            var example = new JetFetch();
            Console.ReadKey(true);
        }

        private void OnConnect(bool completed)
        {
            if (completed)
            {
                Console.WriteLine("Successfully connected to Jet daemon!");
                Matcher matcher = new Matcher();
                // matcher.ContainsAllOf = new string[] { "theState", "foo", "bar" };
                this.peer.Fetch(out this.fetchId, matcher, this.FetchCallback, this.FetchResponseCallback, 5000);
            }
            else
            {
                Console.WriteLine("Connection to Jet daemon failed!");
            }
        }

        private void FetchResponseCallback(bool completed, JToken response)
        {
            if (completed && this.IsSuccessResponse(response))
            {
                Console.WriteLine("States successfully fetched!");
                this.timer = new Timer(this.Elapsed, null, 5000, 0);
            }
            else
            {
                Console.WriteLine("fetching states failed!");
                Console.WriteLine(response);
                this.peer.Disconnect();
            }
        }

        private void Elapsed(object stateInfo)
        {
            if (this.fetchId != null)
            {
                this.peer.Unfetch(this.fetchId, this.UnfetchResponseCallback, 5000);
            }
        }

        private void UnfetchResponseCallback(bool completed, JToken response)
        {
            if (completed && this.IsSuccessResponse(response))
            {
                Console.WriteLine("States successfully unfetched!");
            }
            else
            {
                Console.WriteLine("unfetching states failed!");
                Console.WriteLine(response);
            }

            this.peer.Disconnect();
        }

        private bool IsSuccessResponse(JToken response)
        {
            JToken result = response["result"];
            return ((result != null) && (result.Type == JTokenType.Boolean) && (result.Value<bool>() == true));
        }

        private void FetchCallback(JToken fetchEvent)
        {
            Console.WriteLine(fetchEvent);
        }
    }
}
