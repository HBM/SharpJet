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

namespace Hbm.Devices.Jet
{
    using NUnit.Framework;
    using Newtonsoft.Json.Linq;
    using System;

    [TestFixture]
    public class SetTests
    {
        private bool setSucceeded;
        private bool setCallbackCalled;
        private JetPeer peer;

        [SetUp]
        public void Setup()
        {
            this.setCallbackCalled = false;
            this.setSucceeded = false;

            var connection = new TestSetConnection();
            peer = new JetPeer(connection);
            peer.Connect(this.OnConnect, 1);
        }

        [Test]
        public void SetTestSuccess()
        {
            JValue stateValue = new JValue(12);
            JObject message = peer.Set(TestSetConnection.successPath, stateValue, this.SetResponseCallback, 3000);
            Assert.True(this.setCallbackCalled, "SetCallback was not called");
            Assert.True(this.setSucceeded, "SetCallback was completed successfully");
        }

        [Test]
        public void SetTestOnOwnState()
        {
            JValue stateValue = new JValue(12);
            JObject message = peer.AddState(TestSetConnection.successPath, stateValue, this.OnSet, this.AddResponseCallback, 3000);
            Assert.Throws<ArgumentException>(delegate
            {
                message = peer.Set(TestSetConnection.successPath, stateValue, this.SetResponseCallback, 3000);
            }, "Setting a state that is owned by the peer didn't failed");
        }

        [Test]
        public void SetTestWithWrongPath()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                JValue stateValue = new JValue(12);
                JObject message = peer.Set(null, stateValue, this.SetResponseCallback, 3000);
            }, "Setting a state with \"null\" path didn't failed");

            Assert.Throws<ArgumentNullException>(delegate
            {
                JValue stateValue = new JValue(12);
                JObject message = peer.Set("", stateValue, this.SetResponseCallback, 3000);
            }, "Setting a state with empty path didn't failed");
        }

        private void SetResponseCallback(bool completed, JToken response)
        {
            this.setSucceeded = completed;
            this.setCallbackCalled = true;
        }

        private void AddResponseCallback(bool completed, JToken response)
        {
            this.setSucceeded = completed;
            this.setCallbackCalled = true;
        }

        private JToken OnSet(string path, JToken newValue)
        {
            return null;
        }

        private void OnConnect(bool completed)
        {
        }
    }
}
