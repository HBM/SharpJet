// <copyright file="JetPeer.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using System.Collections.Generic;
    using System.Threading;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JetPeer
    {
        private IJetConnection connection;
        private int fetchIdCounter;
        private Dictionary<int, JetMethod> openRequests;
        private Dictionary<int, JetFetcher> openFetches;
        private Dictionary<string, Func<string, JToken, JToken>> stateCallbacks;

        public JetPeer(IJetConnection connection)
        {
            this.openRequests = new Dictionary<int, JetMethod>();
            this.openFetches = new Dictionary<int, JetFetcher>();
            this.stateCallbacks = new Dictionary<string, Func<string, JToken, JToken>>();
            this.connection = connection;
            connection.HandleIncomingMessage += this.HandleIncomingJson;
        }

        public void Connect(Action<bool> completed, TimeSpan timeout)
        {
            this.connection.Connect(completed, timeout);
        }

        public void Info(Action<JToken> responseCallback)
        {
            JetMethod info = new JetMethod(JetMethod.Info, null, responseCallback);
            this.ExecuteMethod(info);
        }

        public void Add(string path, JToken value, Func<string, JToken, JToken> stateCallback, Action<JToken> responseCallback)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            if (stateCallback == null)
            {
                parameters["fetchOnly"] = true;
            }

            this.RegisterStateCallback(path, stateCallback);
            JetMethod add = new JetMethod(JetMethod.Add, parameters, responseCallback);
            this.ExecuteMethod(add);
        }

        public void Remove(string path, Action<JToken> responseCallback)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            this.UnregisterStateCallback(path);
            JObject parameters = new JObject();
            parameters["path"] = path;
            JetMethod remove = new JetMethod(JetMethod.Remove, parameters, responseCallback);
            this.ExecuteMethod(remove);
        }

        public void Set(string path, JToken value, Action<JToken> responseCallback)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            JetMethod set = new JetMethod(JetMethod.Set, parameters, responseCallback);
            this.ExecuteMethod(set);
        }

        public void Change(string path, JToken value, Action<JToken> responseCallback)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            JetMethod change = new JetMethod(JetMethod.Change, parameters, responseCallback);
            this.ExecuteMethod(change);
        }

        public void Call(string path, JToken args, Action<JToken> responseCallback)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            if (args.Type != JTokenType.Null)
            {
                parameters["args"] = args;
            }

            JetMethod call = new JetMethod(JetMethod.Call, parameters, responseCallback);
            this.ExecuteMethod(call);
        }

        public FetchId Fetch(Matcher matcher, Action<JToken> fetchCallback, Action<JToken> responseCallback)
        {
            int fetchId = Interlocked.Increment(ref this.fetchIdCounter);
            JetFetcher fetcher = new JetFetcher(fetchCallback);
            this.RegisterFetcher(fetchId, fetcher);

            JObject parameters = new JObject();
            parameters["path"] = this.FillPath(matcher);
            parameters["caseInsensitive"] = matcher.CaseInsensitive;
            parameters["id"] = fetchId;
            JetMethod fetch = new JetMethod(JetMethod.Fetch, parameters, responseCallback);
            this.ExecuteMethod(fetch);
            return new FetchId(fetchId);
        }

        public void Unfetch(FetchId fetchId, Action<JToken> responseCallback)
        {
            this.UnregisterFetcher(fetchId.GetId());

            JObject parameters = new JObject();
            parameters["id"] = fetchId.GetId();
            JetMethod unfetch = new JetMethod(JetMethod.Unfetch, parameters, responseCallback);
            this.ExecuteMethod(unfetch);
        }

        private void HandleIncomingJson(object obj, StringEventArgs e)
        {
            JToken json = JToken.Parse(e.Message);
            if (json == null)
            {
                return;
            }

            if (json.Type == JTokenType.Object)
            {
                this.HandleJsonMessage((JObject)json);
                return;
            }

            if (json.Type == JTokenType.Array)
            {
                foreach (var item in json.Children())
                {
                    if (item.Type == JTokenType.Object)
                    {
                        this.HandleJsonMessage((JObject)item);
                    }
                }

                return;
            }
        }

        private void HandleJsonMessage(JObject json)
        {
            JToken fetchIdToken = this.GetFetchId(json);
            if (fetchIdToken != null)
            {
                this.HandleFetch(fetchIdToken.ToObject<int>(), json);
                return;
            }

            if (this.IsResponse(json))
            {
                this.HandleResponse(json);
                return;
            }

            this.HandleStateOrMethodCallbacks(json);
        }

        private void ExecuteMethod(JetMethod method)
        {
            if (method.HasResponseCallback())
            {
                int id = method.GetRequestId();
                lock (this.openRequests)
                {
                    this.openRequests.Add(id, method);
                }
            }

            this.connection.SendMessage(method.GetJson());
        }

        private void RegisterStateCallback(string path, Func<string, JToken, JToken> callback)
        {
            lock (this.stateCallbacks)
            {
                this.stateCallbacks.Add(path, callback);
            }
        }

        private void UnregisterStateCallback(string path)
        {
            lock (this.stateCallbacks)
            {
                this.stateCallbacks.Remove(path);
            }
        }

        private void RegisterFetcher(int fetchId, JetFetcher fetcher)
        {
            lock (this.openFetches)
            {
                this.openFetches.Add(fetchId, fetcher);
            }
        }

        private void UnregisterFetcher(int fetchId)
        {
            lock (this.openFetches)
            {
                this.openFetches.Remove(fetchId);
            }
        }

        private JToken GetFetchId(JObject json)
        {
            JToken methodToken = json["method"];
            if ((methodToken != null) && (methodToken.Type == JTokenType.Integer))
            {
                return methodToken;
            }

            return null;
        }

        private void HandleStateOrMethodCallbacks(JToken json)
        {
            JObject result = new JObject();
            JToken methodToken = json["method"];
            if (methodToken == null)
            {
                result["error"]["code"] = -32601;
                result["error"]["message"] = "No method given!";
                this.SendResponse(json, result);
                return;
            }

            string method = methodToken.ToObject<string>();
            if ((method == null) || (method.Length == 0))
            {
                result["error"]["code"] = -32601;
                result["error"]["message"] = "Method is not a string or empty!";
                this.SendResponse(json, result);
                return;
            }

            Func<string, JToken, JToken> callback = null;
            lock (this.stateCallbacks)
            {
                callback = this.stateCallbacks[method];
            }

            if (callback == null)
            {
                result["error"]["code"] = -32000;
                result["error"]["message"] = "State is read-only!";
                this.SendResponse(json, result);
                return;
            }

            JToken newValue = callback(method, json["params"]["value"]);
            if (newValue != null)
            {
                this.Change(method, newValue, null);
            }

            result["result"] = true;
            this.SendResponse(json, result);
            return;
        }

        private void SendResponse(JToken json, JObject result)
        {
            JToken id = json["id"];
            if ((id != null) && ((id.Type == JTokenType.String) || (id.Type == JTokenType.Integer)))
            {
                result["id"] = id;
                this.connection.SendMessage(JsonConvert.SerializeObject(result));
            }
        }

        private void HandleFetch(int fetchId, JObject json)
        {
            JetFetcher fetcher = null;
            lock (this.openFetches)
            {
                if (this.openFetches.ContainsKey(fetchId))
                {
                    fetcher = this.openFetches[fetchId];
                }
            }

            if (fetcher != null)
            {
                JToken parameters = json["params"];
                if (parameters.Type != JTokenType.Null)
                {
                    fetcher.CallFetchCallback(parameters);
                }
            }
        }

        private JObject FillPath(Matcher matcher)
        {
            JObject path = new JObject();
            if (!string.IsNullOrEmpty(matcher.Contains))
            {
                path["contains"] = matcher.Contains;
            }

            if (!string.IsNullOrEmpty(matcher.CtartsWith))
            {
                path["startsWith"] = matcher.CtartsWith;
            }

            if (!string.IsNullOrEmpty(matcher.EndsWith))
            {
                path["endsWith"] = matcher.EndsWith;
            }

            if (!string.IsNullOrEmpty(matcher.EqualsTo))
            {
                path["equals"] = matcher.EqualsTo;
            }

            if (!string.IsNullOrEmpty(matcher.EqualsNotTo))
            {
                path["equalsNot"] = matcher.EqualsNotTo;
            }

            return path;
        }

        private bool IsResponse(JObject json)
        {
            JToken methodToken = json["method"];
            if ((methodToken == null) || (methodToken.ToObject<string>() == null))
            {
                return true;
            }

            return false;
        }

        private void HandleResponse(JObject json)
        {
            JToken token = json["id"];
            if (token != null)
            {
                int id = token.ToObject<int>();
                JetMethod method = null;
                lock (this.openRequests)
                {
                    if (this.openRequests.ContainsKey(id))
                    {
                        method = this.openRequests[id];
                        this.openRequests.Remove(id);
                    }
                }

                if (method != null)
                {
                    method.CallResponseCallback(json);
                }
            }
        }
    }
}
