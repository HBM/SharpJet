// <copyright file="TestJetConnection.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    public class TestSetConnection : IJetConnection
    {
        public event EventHandler<StringEventArgs> HandleIncomingMessage;
        public static String successPath = "success";

        public void Connect(Action<bool> completed, double timeoutMs)
        {
            completed(true);
        }

        public void SendMessage(string message)
        {
            JToken json = JToken.Parse(message);
            JToken parameters = json["params"];
            JToken path = parameters["path"];

            if (path.ToString().Equals(successPath))
            {
                emitSuccessResponse(json);
            }
        }

        private void emitSuccessResponse(JToken json)
        {
            JObject response = new JObject();
            response["jsonrpc"] = "2.0";
            response["id"] = json["id"];
            response["result"] = true;

            HandleIncomingMessage(this, new StringEventArgs(JsonConvert.SerializeObject(response)));
        }

        public void Disconnect()
        {
        }
    }
}
