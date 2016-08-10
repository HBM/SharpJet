// <copyright file="JetMethod.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

namespace Hbm.Devices.Jet
{
    using System;
    using System.Threading;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class JetMethod
    {
        internal const string Info = "info";
        internal const string Set = "set";
        internal const string Fetch = "fetch";
        internal const string Unfetch = "unfetch";
        internal const string Call = "call";
        internal const string Add = "add";
        internal const string Remove = "remove";
        internal const string Change = "change";

        internal System.Timers.Timer requestTimer;

        private static int requestIdCounter;
        private JObject json;
        private Action<bool, JToken> responseCallback;
        private int requestId;

        internal JetMethod(string method, JObject parameters, Action<bool, JToken> responseCallback)
        {
            this.responseCallback = responseCallback;
            if (responseCallback != null)
            {
                this.requestTimer = new System.Timers.Timer();
                this.requestId = Interlocked.Increment(ref requestIdCounter);
            }

            JObject json = new JObject();
            json["jsonrpc"] = "2.0";
            json["method"] = method;
            json["id"] = this.requestId;
            if (parameters != null)
            {
                json["params"] = parameters;
            }

            this.json = json;
        }

        internal bool HasResponseCallback()
        {
            return this.responseCallback != null;
        }

        internal int GetRequestId()
        {
            return this.requestId;
        }

        internal void CallResponseCallback(bool completed, JToken response)
        {
            if (this.responseCallback != null)
            {
                this.responseCallback(completed, response);
            }
        }

        internal string GetJson()
        {
            return JsonConvert.SerializeObject(this.json);
        }
    }
}
