// <copyright file="JetPeer.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpJetTests")]

namespace Hbm.Devices.Jet
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JetPeer
    {
        private const double DefaultRoutingTimeout = 1000.0;

        private IJetConnection connection;
        private int fetchIdCounter;
        private Dictionary<int, JetMethod> openRequests;
        private Dictionary<int, JetFetcher> openFetches;
        private Dictionary<string, Func<string, JToken, JToken>> stateCallbacks;
        private Dictionary<string, Func<string, JToken, JToken>> methodCallbacks;
        private HashSet<FetchId> allFetches;

        public JetPeer(IJetConnection connection)
        {
            this.openRequests = new Dictionary<int, JetMethod>();
            this.openFetches = new Dictionary<int, JetFetcher>();
            this.stateCallbacks = new Dictionary<string, Func<string, JToken, JToken>>();
            this.methodCallbacks = new Dictionary<string, Func<string, JToken, JToken>>();
            this.allFetches = new HashSet<FetchId>();

            this.connection = connection;
            connection.HandleIncomingMessage += this.HandleIncomingJson;
        }

        public void Connect(Action<bool> completed, double timeoutMs)
        {
            this.connection.Connect(completed, timeoutMs);
        }

        public void Disconnect()
        {
            removeAllStates();
            removeAllMethods();
            removeAllFetches();

            this.connection.Disconnect();
        }

        public JObject Info(Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            JetMethod info = new JetMethod(JetMethod.Info, null, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(info);
        }

        public JObject Authenticate(string user, string password, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            JObject credentials = new JObject();
            credentials["user"] = user;
            credentials["password"] = password;
            JetMethod authenticate = new JetMethod(JetMethod.Authenticate, credentials, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(authenticate);
        }


        /// <summary>
        /// Adds a state to a Jet daemon.
        /// </summary>
        /// <param name="path">The path under which the state will be reachable.</param>
        /// <param name="value">The initial value of the state to be added.</param>
        /// <param name="stateCallback">
        /// <para>The callback method that will be called if somebody calls a "Set" on the state to be added.</para>
        /// <para>If this parameter is null, the state is registered read-only (cannot be changed via "Set") on the Jet daemon.</para>
        /// <para>
        /// The callback method must conform to the following prototype JToken callback(string path, JToken value).
        /// "path" is the path under which the state was registered. This might be useful to use a single callback method for several
        /// states and multiplex via "path".
        /// </para>
        /// <para>
        /// "value" contains the new value that should be set.
        /// The callback MUST return the new state value, regardless if the value was adapted or accepted without any change.
        /// If callback can't accept the value to be set, it must throw a <see cref="JsonRpcException"/>.
        /// </para>
        /// </param>
        /// <param name="responseCallback">A callback method that will be called if this method succeeds or fails.</param>
        /// <param name="responseTimeoutMilliseconds">The timeout how long the operation might take before failing.</param>
        /// <param name="stateSetTimeoutMilliseconds">The timeout how long a set operation on this state might take before the daemon signals timeout to the peer calling set().</param>
        /// <returns>A JObject representing the Add message send to the Jet daemon.</returns>
        public JObject AddState(
            string path,
            JToken value,
            Func<string, JToken, JToken> stateCallback,
            Action<bool, JToken> responseCallback,
            double responseTimeoutMilliseconds, 
            double stateSetTimeoutMilliseconds = DefaultRoutingTimeout)
        {
            return this.AddState(path, value, null, null, stateCallback, responseCallback, responseTimeoutMilliseconds, stateSetTimeoutMilliseconds);
        }

        public JObject AddState(
            string path,
            JToken value,
            string[] setGroups,
            string[] fetchGroups,
            Func<string, JToken, JToken> stateCallback,
            Action<bool, JToken> responseCallback,
            double responseTimeoutMs,
            double stateSetTimeoutMs = DefaultRoutingTimeout)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            parameters["timeout"] = stateSetTimeoutMs / 1000.0;
            if (stateCallback == null)
            {
                parameters["fetchOnly"] = true;
            }
            else
            {
                this.RegisterStateCallback(path, stateCallback);
            }

            if (((setGroups != null) && (setGroups.Length > 0)) || ((fetchGroups != null) && (fetchGroups.Length > 0)))
            {
                JObject access = new JObject();
                parameters["access"] = access;
                access["setGroups"] = new JArray(setGroups);
                access["fetchGroups"] = new JArray(fetchGroups);
            }

            JetMethod add = new JetMethod(JetMethod.Add, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(add);
        }

        public JObject AddMethod(
            string path,
            Func<string, JToken, JToken> methodCallback,
            Action<bool, JToken> responseCallback,
            double responseTimeoutMilliseconds,
            double methodCallTimeoutMilliseconds = DefaultRoutingTimeout)
        {
            return this.AddMethod(path, null, null, methodCallback, responseCallback, responseTimeoutMilliseconds, methodCallTimeoutMilliseconds);
        }

        public JObject AddMethod(
            string path,
            string[] callGroups,
            string[] fetchGroups,
            Func<string, JToken, JToken> methodCallback,
            Action<bool, JToken> responseCallback,
            double responseTimeoutMs,
            double methodCallTimeoutMs = DefaultRoutingTimeout)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (methodCallback == null)
            {
                throw new ArgumentNullException("methodCallback");
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["timeout"] = methodCallTimeoutMs / 1000.0;
            if (((callGroups != null) && (callGroups.Length > 0)) || ((fetchGroups != null) && (fetchGroups.Length > 0)))
            {
                JObject access = new JObject();
                parameters["access"] = access;
                access["callGroups"] = new JArray(callGroups);
                access["fetchGroups"] = new JArray(fetchGroups);
            }

            this.RegisterMethodCallback(path, methodCallback);
            JetMethod add = new JetMethod(JetMethod.Add, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(add);
        }

        public JObject RemoveState(string path, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            return this.RemoveStateOrMethod(true, path, responseCallback, responseTimeoutMs);
        }

        public JObject RemoveMethod(string path, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            return this.RemoveStateOrMethod(false, path, responseCallback, responseTimeoutMs);
        }

        public JObject Set(string path, JToken value, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            lock (this.stateCallbacks)
            {
                if (this.stateCallbacks.ContainsKey(path))
                {
                    throw new ArgumentException("Don't call Set() on a state you own, use Change() instead!", "path");
                }
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            parameters["timeout"] = responseTimeoutMs / 1000.0;
            JetMethod set = new JetMethod(JetMethod.Set, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(set);
        }

        public JObject Change(string path, JToken value, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            lock (this.stateCallbacks)
            {
                if (!this.stateCallbacks.ContainsKey(path))
                {
                    throw new ArgumentException("You can't call Change() on a state you don't own!", "path");
                }
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            JetMethod change = new JetMethod(JetMethod.Change, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(change);
        }

        public JObject Call(string path, JToken args, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            lock (this.methodCallbacks)
            {
                if (this.methodCallbacks.ContainsKey(path))
                {
                    throw new ArgumentException("You can't call Call() on a method you don't own!", "path");
                }
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["timeout"] = responseTimeoutMs / 1000.0;
            if (args.Type != JTokenType.Null)
            {
                parameters["args"] = args;
            }

            JetMethod call = new JetMethod(JetMethod.Call, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(call);
        }

        public JObject Fetch(out FetchId id, Matcher matcher, Action<JToken> fetchCallback, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            int fetchId = Interlocked.Increment(ref this.fetchIdCounter);
            JetFetcher fetcher = new JetFetcher(fetchCallback);
            this.RegisterFetcher(fetchId, fetcher);

            JObject parameters = new JObject();
            JObject path = this.FillPath(matcher);
            if (path != null)
            {
                parameters["path"] = path;
            }

            parameters["caseInsensitive"] = matcher.CaseInsensitive;
            parameters["id"] = fetchId;
            JetMethod fetch = new JetMethod(JetMethod.Fetch, parameters, responseCallback, responseTimeoutMs);
            id = new FetchId(fetchId);
            lock (allFetches)
            {
                allFetches.Add(id);
            }

            return this.ExecuteMethod(fetch);
        }

        public JObject Unfetch(FetchId fetchId, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            this.UnregisterFetcher(fetchId.GetId());

            JObject parameters = new JObject();
            parameters["id"] = fetchId.GetId();
            JetMethod unfetch = new JetMethod(JetMethod.Unfetch, parameters, responseCallback, responseTimeoutMs);
            lock (allFetches)
            {
                allFetches.Remove(fetchId);
            }

            return this.ExecuteMethod(unfetch);
        }

        private static void RequestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e, JetMethod method)
        {
            JetPeer peer = (JetPeer)sender;
            lock (peer.openRequests)
            {
                method.RequestTimer.Stop();
                int id = method.GetRequestId();

                if (peer.openRequests.ContainsKey(id))
                {
                    method = peer.openRequests[id];
                    peer.openRequests.Remove(id);

                    JObject response = new JObject();
                    response["jsonrpc"] = "2.0";
                    response["id"] = id;
                    JObject error = new JObject();
                    error["code"] = -32100;
                    error["message"] = "timeout while waiting for response";
                    response["error"] = error;
                    lock (method)
                    {
                        method.CallResponseCallback(false, response);
                    }
                }
            }
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

        private JObject ExecuteMethod(JetMethod method)
        {
            double timeoutMs = method.getTimeoutMs();
            if (timeoutMs < 0.0)
            {
                throw new ArgumentException("timeoutMs");
            }

            if (method.HasResponseCallback())
            {
                int id = method.GetRequestId();
                lock (this.openRequests)
                {
                    method.RequestTimer.Interval = timeoutMs;
                    method.RequestTimer.Elapsed += (sender, e) => RequestTimer_Elapsed(this, e, method);
                    method.RequestTimer.Start();

                    this.openRequests.Add(id, method);
                }
            }

            JObject request = method.GetJson();
            this.connection.SendMessage(JsonConvert.SerializeObject(request));
            return request;
        }

        internal int NumberOfRegisteredStateCallbacks()
        {
            lock (this.stateCallbacks)
            {
                return this.stateCallbacks.Count;
            }
        }

        private void removeAllFetches()
        {
            var tempSet = new HashSet<FetchId>();
            lock (allFetches)
            {
                foreach (var fetchId in allFetches)
                {
                    tempSet.Add(fetchId);
                }
            }

            foreach (var fetchId in tempSet)
            {
                this.Unfetch(fetchId, null, 0);
            }
        }

        private void removeAllStates()
        {
            var tempDictionary = new Dictionary<string, Func<string, JToken, JToken>>();

            lock (this.stateCallbacks)
            {
                foreach (KeyValuePair<string, Func<string, JToken, JToken>> entry in stateCallbacks)
                {
                    tempDictionary.Add(entry.Key, entry.Value);
                }
            }

            foreach (KeyValuePair<string, Func<string, JToken, JToken>> entry in tempDictionary)
            {
                this.RemoveState(entry.Key, null, 0);
            }
        }

        private void removeAllMethods()
        {
            var tempDictionary = new Dictionary<string, Func<string, JToken, JToken>>();

            lock (this.methodCallbacks)
            {
                foreach (KeyValuePair<string, Func<string, JToken, JToken>> entry in methodCallbacks)
                {
                    tempDictionary.Add(entry.Key, entry.Value);
                }
            }

            foreach (KeyValuePair<string, Func<string, JToken, JToken>> entry in tempDictionary)
            {
                this.RemoveMethod(entry.Key, null, 0);
            }
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

        private void RegisterMethodCallback(string path, Func<string, JToken, JToken> callback)
        {
            lock (this.methodCallbacks)
            {
                this.methodCallbacks.Add(path, callback);
            }
        }

        private void UnregisterMethodCallback(string path)
        {
            lock (this.methodCallbacks)
            {
                this.methodCallbacks.Remove(path);
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
            try
            {
                JObject result = new JObject();
                JToken methodToken = json["method"];
                if (methodToken == null)
                {
                    throw new JsonRpcException(JsonRpcException.MethodNotFound, "no method given");
                }

                string jetPath = methodToken.ToObject<string>();
                if ((jetPath == null) || (jetPath.Length == 0))
                {
                    throw new JsonRpcException(JsonRpcException.MethodNotFound, "method is not a string or emtpy");
                }

                bool stateHandled = this.HandleStateCallback(json, jetPath);
                if (stateHandled)
                {
                    return;
                }
                else
                {
                    this.HandleMethod(json, jetPath);
                }
            }
            catch (JsonRpcException e)
            {
                this.SendResponse(json, e.GetJson());
            }
        }

        private bool HandleStateCallback(JToken json, string jetPath)
        {
            Func<string, JToken, JToken> callback = null;
            bool stateFound = false;

            lock (this.stateCallbacks)
            {
                stateFound = this.stateCallbacks.TryGetValue(jetPath, out callback);
            }

            if (stateFound)
            {
                if (callback == null)
                {
                    throw new JsonRpcException(JsonRpcException.InvalidRequest, "state is read-only");
                }

                JToken parameters = json["params"];
                if (parameters == null)
                {
                    throw new JsonRpcException(JsonRpcException.InvalidParams, "no parameters in Json");
                }

                JToken value = parameters["value"];
                if (value == null)
                {
                    throw new JsonRpcException(JsonRpcException.InvalidParams, "no value in parameter sub-object in Json");
                }

                JToken newValue = callback(jetPath, value);
                if (newValue != null)
                {
                    this.Change(jetPath, newValue, null, 0);
                }
                else
                {
                    this.Change(jetPath, value, null, 0);
                }

                JObject result = new JObject();
                result["result"] = true;
                this.SendResponse(json, result);

                return true;
            }

            return false;
        }

        private void HandleMethod(JToken json, string jetPath)
        {
            Func<string, JToken, JToken> callback = null;
            bool methodFound = false;

            lock (this.methodCallbacks)
            {
                methodFound = this.methodCallbacks.TryGetValue(jetPath, out callback);
            }

            if (methodFound)
            {
                JToken parameters = json["params"];
                JObject result = new JObject();
                result["result"] = callback(jetPath, parameters);
                this.SendResponse(json, result);

                return;
            }
        }

        private void SendError(JToken incomingJson, int errorCode, string message)
        {
            JObject result = new JObject();
            JObject error = new JObject();
            error["code"] = errorCode;
            error["message"] = message;
            result["error"] = error;
            this.SendResponse(incomingJson, result);
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
                if ((parameters != null) && (parameters.Type != JTokenType.Null))
                {
                    fetcher.CallFetchCallback(parameters);
                }
                else
                {
                    // Todo: Log error
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

            if (!string.IsNullOrEmpty(matcher.StartsWith))
            {
                path["startsWith"] = matcher.StartsWith;
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

            if ((matcher.ContainsAllOf != null) && matcher.ContainsAllOf.Length > 0)
            {
                path["containsAllOf"] = JToken.FromObject(matcher.ContainsAllOf);
            }
            
            if (path.Count == 0)
            {
                return null;
            }
            else
            {
                return path;
            }
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

        private JObject RemoveStateOrMethod(bool isState, string path, Action<bool, JToken> responseCallback, double responseTimeoutMs)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            if (isState)
            {
                this.UnregisterStateCallback(path);
            }
            else
            {
                this.UnregisterMethodCallback(path);
            }

            JObject parameters = new JObject();
            parameters["path"] = path;
            JetMethod remove = new JetMethod(JetMethod.Remove, parameters, responseCallback, responseTimeoutMs);
            return this.ExecuteMethod(remove);
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
                        method.RequestTimer.Stop();
                        lock (method)
                        {
                            method.CallResponseCallback(true, json);
                        }
                    }
                }
            }
        }
    }
}
