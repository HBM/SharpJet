// <copyright file="ConnectTests.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using System;
    using FakeItEasy;
    using Hbm.Devices.Jet;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Hbm.Devices.Jet.Utils;

    [TestFixture]
    public class JetMethodTests
    {
        [Test]
        public void TestConstructorInitializesTimer()
        {
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                null,
                A.Dummy<Action<bool, JToken>>(),
                1000.0);

            Assert.IsNotNull(jetMethod.RequestTimer as TimerAdapter, "ctor did not initialize a TimerAdapter.");
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestConstructorDoesNotSetRequestIdIfResponseCallbackIsNull()
        {
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                null,
                null, //No callback specified
                1000.0);

            Assert.AreEqual(0, jetMethod.GetRequestId());
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestConstructorIncreasesRequestIdIfResponseCallbackIsNotNull()
        {
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                null,
                A.Dummy<Action<bool, JToken>>(),
                1000.0);

            JetMethod nextJetMethod = new JetMethod(JetMethod.Info,
                null,
                A.Dummy<Action<bool, JToken>>(),
                1000.0);

            Assert.AreEqual(1, nextJetMethod.GetRequestId() - jetMethod.GetRequestId());
        }

        [Test]
        public void TestConstructorInitializesResponseTimeout()
        {
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                null,
                A.Dummy<Action<bool, JToken>>(),
                1337.42);

            Assert.AreEqual(1337.42, jetMethod.GetTimeoutMs());
        }

        [Test]
        public void TestConstructorInitializesJson()
        {
            JObject parameter = new JObject();
            parameter.Add("p1", JToken.FromObject(15));
            parameter.Add("p2", JToken.FromObject("hello world"));
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                parameter,
                A.Dummy<Action<bool, JToken>>(),
                1000.0);

            JObject json = jetMethod.GetJson();
            Assert.AreEqual((string)json["jsonrpc"], "2.0");
            Assert.AreEqual((string)json["method"], JetMethod.Info);
            Assert.AreEqual((int)json["params"]["p1"], 15);
            Assert.AreEqual((string)json["params"]["p2"], "hello world");
        }

        [Test]
        public void TestCallResponseCallbackInvokesActions()
        {
            Action<bool, JToken> responseCallback = A.Fake<Action<bool, JToken>>();
            JetMethod jetMethod = new JetMethod(JetMethod.Info, new JObject(), responseCallback, 1000.0);

            JToken token = JToken.FromObject(15);
            jetMethod.CallResponseCallback(true, token);
            jetMethod.CallResponseCallback(false, token);
            A.CallTo(() => responseCallback.Invoke(true, token)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => responseCallback.Invoke(false, token)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestHasResponseCallback()
        {
            JetMethod jetMethod1 = new JetMethod(JetMethod.Info,
                new JObject(), 
                A.Dummy<Action<bool, JToken>>(), 
                1000.0);

            JetMethod jetMethod2 = new JetMethod(JetMethod.Info,
                new JObject(),
                null,
                1000.0);

            Assert.IsTrue(jetMethod1.HasResponseCallback(), $"Expected {nameof(jetMethod1)} to have a response callback but received false.");
            Assert.IsFalse(jetMethod2.HasResponseCallback(), $"Expected {nameof(jetMethod2)} to not have a response callback but received true.");
        }

        [Test]
        public void TestDispose()
        {
            JetMethod jetMethod = new JetMethod(JetMethod.Info,
                new JObject(),
                A.Dummy<Action<bool, JToken>>(),
                1000.0);

            ITimer timer = A.Fake<ITimer>();
            jetMethod.RequestTimer = timer;
            jetMethod.Dispose();
            
            //Second call does nothing.
            jetMethod.Dispose();

            A.CallTo(() => timer.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
