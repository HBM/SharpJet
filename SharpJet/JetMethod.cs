// <copyright file="JetMethod.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

using Hbm.Devices.Jet.Utils;

namespace Hbm.Devices.Jet
{
    using System;
    using System.Threading;
    using Newtonsoft.Json.Linq;

    internal class JetMethod : DisposableBase
    {
        internal const string Authenticate = "authenticate";
        internal const string Passwd = "passwd";
        internal const string Config = "config";
        internal const string Info = "info";
        internal const string Set = "set";
        internal const string Fetch = "fetch";
        internal const string Unfetch = "unfetch";
        internal const string Call = "call";
        internal const string Add = "add";
        internal const string Remove = "remove";
        internal const string Change = "change";
        internal const string Get = "get";

        private bool _isDisposed;
        private static int requestIdCounter;
        private readonly JObject json;
        private readonly Action<bool, JToken> responseCallback;
        private readonly int requestId;
        private readonly double responseTimeoutMs;

        internal ITimer RequestTimer { get; set; }
        
        internal JetMethod(string method, JObject parameters, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            this.responseTimeoutMs = responseTimeoutMs;
            this.responseCallback = responseCallback;

            JObject json = new JObject();
            json["jsonrpc"] = "2.0";
            json["method"] = method;
            if (responseCallback != null)
            {
                this.RequestTimer = new TimerAdapter();
                this.requestId = Interlocked.Increment(ref requestIdCounter);
                json["id"] = this.requestId;
            }

            if (parameters != null)
            {
                json["params"] = parameters;
            }

            this.json = json;
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                RequestTimer.Dispose();
            }

            _isDisposed = true;
        }

        internal double GetTimeoutMs()
        {
            return responseTimeoutMs;
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

        internal JObject GetJson()
        {
            return this.json;
        }
    }
}
