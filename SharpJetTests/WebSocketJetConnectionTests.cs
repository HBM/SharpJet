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

namespace SharpJetTests
{
    using NUnit.Framework;
    using Hbm.Devices.Jet;
    using Hbm.Devices.Jet.Utils;
    using System.Net.Security;
    using System.Security.Authentication;
    using FakeItEasy;
    using System;
    using SharpJetTests.Utils;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using WebSocketSharp;

    [TestFixture]
    public class WebSocketJetConnectionTests
    {
        [Test]
        public void TestConstructorIsNotConnected()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            Assert.IsFalse(webSocketJetConnection.IsConnected,
                $"Expected {nameof(webSocketJetConnection.IsConnected)} to be true but received false.");
        }

        [Test]
        public void TestConstructorInitializesTimer()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            Assert.IsAssignableFrom<TimerAdapter>(webSocketJetConnection.ConnectTimer);
        }

        [Test]
        public void TestContructorSetsCertificationCallback()
        {
            RemoteCertificateValidationCallback certificationCallback = A.Dummy<RemoteCertificateValidationCallback>();
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081",
                certificationCallback);

            Assert.AreEqual(SslProtocols.Tls, webSocketJetConnection.WebSocket.SslConfiguration.EnabledSslProtocols);
            Assert.AreEqual(certificationCallback, webSocketJetConnection.WebSocket.SslConfiguration.ServerCertificateValidationCallback);
        }

        [Test]
        public void TestConnectCallsWebSocketConnectAsync()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Dummy<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            IWebSocket webSocket = A.Fake<IWebSocket>();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            A.CallTo(() => webSocket.ConnectAsync()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestConnectSucceeds()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Dummy<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            Assert.IsTrue(webSocketJetConnection.IsConnected);
        }

        [Test]
        public void TestConnectInvokesCompletedAction()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Dummy<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsSuccessful();
            A.CallTo(() => webSocket.IsAlive).Returns(true);
            webSocketJetConnection.SetWebSocket(webSocket);
            Action<bool> completed = A.Fake<Action<bool>>();

            webSocketJetConnection.Connect(completed, 1000.0);

            A.CallTo(() => completed(true)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestConnectCallsTimer()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);
            ITimer timer = A.Fake<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;

            double timeoutMs = 4242.42;
            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), timeoutMs);

            AssertTimerCallsOnConnect(timer, timeoutMs);
        }

        [Test]
        public void TestConnectThrowsExceptionIfAlreadyConnected()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Dummy<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);

            Assert.Throws<JetPeerException>(() =>
            {
                //Second call.
                webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            });
        }

        [Test]
        public void TestConnectTimeoutClosesWebSocketIfAlive()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatFailsConnectDueTimeout(timer);
            webSocketJetConnection.SetWebSocket(webSocket);
            webSocketJetConnection.ConnectTimer = timer;
            A.CallTo(() => webSocket.IsAlive).Returns(true);
            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);

            A.CallTo(() => webSocket.Close()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestConnectTimeoutCallsTimer()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatFailsConnectDueTimeout(timer);
            webSocketJetConnection.SetWebSocket(webSocket);
            webSocketJetConnection.ConnectTimer = timer;

            Action<bool> connectCallback = A.Fake<Action<bool>>();
            A.CallTo(() => connectCallback(false)).Invokes(() => {
                webSocket.OnOpen += Raise.WithEmpty();
            });

            webSocketJetConnection.Connect(connectCallback, 1234.56);

            AssertTimerCallsOnConnect(timer, 1234.56);
            A.CallTo(() => connectCallback(true)).MustNotHaveHappened();
            A.CallTo(() => connectCallback(false)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestConnectTimeoutInvokesCompleteAction()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatFailsConnectDueTimeout(timer);
            webSocketJetConnection.SetWebSocket(webSocket);
            webSocketJetConnection.ConnectTimer = timer;
            A.CallTo(() => webSocket.IsAlive).Returns(false);
            Action<bool> completed = A.Fake<Action<bool>>();

            webSocketJetConnection.Connect(completed, 10.0);

            A.CallTo(() => completed(false)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestDisconnectThrowsExceptionIfNotConnected()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            Assert.Throws<JetPeerException>(() =>
            {
                webSocketJetConnection.Disconnect();
            });
        }

        [Test]
        public void TestDisconnectSucceeds()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsAndClosesSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            webSocketJetConnection.Disconnect();

            Assert.IsFalse(webSocketJetConnection.IsConnected, $"Expected {webSocketJetConnection.IsConnected} to be false but received true.");
        }

        [Test]
        public void TestSendThrowsExceptionIfNotConnected()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            Assert.Throws<JetPeerException>(() =>
            {
                webSocketJetConnection.SendMessage("msg");
            });
        }

        [Test]
        public void TestSendCallsWebSocketSend()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsAndClosesSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            webSocketJetConnection.SendMessage("msg");
            A.CallTo(() => webSocket.Send("msg")).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestDiconnectCallsWebSocketCloseAsync()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsAndClosesSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            webSocketJetConnection.Disconnect();

            A.CallTo(() => webSocket.CloseAsync(CloseStatusCode.Away)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        [Ignore("Websocket sharp Opcode internal")]
        public void TestHandleIncomingMessageInvokesEventHandlerMessageIsText()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = A.Fake<IWebSocket>();
            webSocketJetConnection.SetWebSocket(webSocket);
            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);
            List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
            webSocketJetConnection.HandleIncomingMessage += delegate (object sender, StringEventArgs args)
            {
                receivedEvents.Add(args);
            };

            int numOfMessageEvents = 5;
            for (int i = 0; i < numOfMessageEvents; i++)
            {
                webSocket.OnMessage += Raise.With(webSocket, CreateMessageEventArgsUsingReflection(true));
            }

            Assert.AreEqual(numOfMessageEvents, receivedEvents.Count);
        }

        [Test]
        [Ignore("Websocket sharp Opcode internal")]
        public void TestHandleIncomingMessageDoesNotInvokeEventHandlerIfMessageIsNotText()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = A.Fake<IWebSocket>();
            webSocketJetConnection.SetWebSocket(webSocket);

            List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
            webSocketJetConnection.HandleIncomingMessage += delegate (object sender, StringEventArgs args)
            {
                receivedEvents.Add(args);
            };

            webSocket.OnMessage += Raise.With(webSocket, CreateMessageEventArgsUsingReflection(false));

            Assert.AreEqual(0, receivedEvents.Count);
        }

        [Test]
        public void TestDisposeTimer()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            timer.Enabled = false;
            webSocketJetConnection.Dispose();
            A.CallTo(() => timer.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestDisposeStopsTimerIfEnabled()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            timer.Enabled = true;
            webSocketJetConnection.Dispose();
            A.CallTo(() => timer.Stop()).MustHaveHappened(Repeated.Exactly.Once).Then(
                A.CallTo(() => timer.Dispose()).MustHaveHappened(Repeated.Exactly.Once));
        }

        [Test]
        public void TestDisposeWebSocket()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = A.Fake<IWebSocket>();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Dispose();

            A.CallTo(() => webSocket.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestDisposeMultipleCalls()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            ITimer timer = A.Fake<ITimer>();
            webSocketJetConnection.ConnectTimer = timer;
            IWebSocket webSocket = A.Fake<IWebSocket>();
            webSocketJetConnection.SetWebSocket(webSocket);

            webSocketJetConnection.Dispose();
            webSocketJetConnection.Dispose();
            webSocketJetConnection.Dispose();

            A.CallTo(() => timer.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => webSocket.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestDisposeClosesWebSocketIfConnected()
        {
            WebSocketJetConnection webSocketJetConnection = new WebSocketJetConnection("ws://172.19.191.179:8081");
            IWebSocket webSocket = WebSocketFakesFactory.CreateWebSocketThatConnectsAndClosesSuccessful();
            webSocketJetConnection.SetWebSocket(webSocket);
            webSocketJetConnection.Connect(A.Dummy<Action<bool>>(), 1000.0);

            webSocketJetConnection.Dispose();

            A.CallTo(() => webSocket.Close(CloseStatusCode.Away)).MustHaveHappened(Repeated.Exactly.Once).Then(
                A.CallTo(() => webSocket.Dispose()).MustHaveHappened(Repeated.Exactly.Once));
        }

        private void AssertTimerCallsOnConnect(ITimer timer, double interval)
        {
            A.CallTo(() => timer.Start())
                .MustHaveHappened(Repeated.Exactly.Once)
                .Then(A.CallTo(() => timer.Stop()).MustHaveHappened(Repeated.Exactly.Once));
            Assert.AreEqual(interval, timer.Interval);
            Assert.IsFalse(timer.AutoReset, $"Expected {nameof(timer.AutoReset)} to be false but received true.");
        }

        private MessageEventArgs CreateMessageEventArgsUsingReflection(bool isText)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var parameter = isText ?
                //new object[] { OpCode.Text, Encoding.ASCII.GetBytes("text") } :
                //new object[] { OpCode.Binary, new byte[4] };
                new object[] { 1, Encoding.ASCII.GetBytes("text") } :
                new object[] { 2, new byte[4] };

            MessageEventArgs messageEventArgs =
                (MessageEventArgs)Activator.CreateInstance(typeof(MessageEventArgs), flags, null, parameter, null);
            
            return messageEventArgs;
        }
    }
}
