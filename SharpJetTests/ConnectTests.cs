// <copyright file="ConnectTests.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using NUnit.Framework;
    using System.Collections;

    public class ConnectTests
    {
        private bool connectCallbackCalled;
        private bool connectCompleted;

        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(Behaviour.ConnectionSuccess).Returns(true);
                yield return new TestCaseData(Behaviour.ConnectionFail).Returns(false);
            }
        }

        [SetUp]
        public void Setup()
        {
            this.connectCallbackCalled = false;
        }

        [Test, TestCaseSource(typeof(ConnectTests), "TestCases")]
        public bool ConnectTest(Behaviour behaviour)
        {
            var connection = new TestJetConnection(behaviour);
            var peer = new JetPeer(connection);
            peer.Connect(OnConnect, 1);
            Assert.AreEqual(this.connectCallbackCalled, true);
            return this.connectCompleted;
        }

        private void OnConnect(bool completed)
        {
            this.connectCallbackCalled = true;
            this.connectCompleted = completed;
        }
    }
}
