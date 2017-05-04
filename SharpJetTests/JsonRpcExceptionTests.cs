// <copyright file="JsonRpcException.cs" company="Hottinger Baldwin Messtechnik GmbH">
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
    using Hbm.Devices.Jet;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class JsonRpcExceptionTests
    {
        [Test]
        public void TestGetJsonWithoutData()
        {
            JsonRpcException exception = new JsonRpcException(42, "msg");
            JObject json = exception.GetJson();
            Assert.AreEqual(42, (int)json["code"]);
            Assert.AreEqual("msg", (string)json["message"]);
            Assert.IsNull(json["data"]);
        }

        [Test]
        public void TestGetJsonWithData()
        {
            JObject data = new JObject();
            data["d1"] = 42;
            data["d2"] = "42";
            JsonRpcException exception = new JsonRpcException(42, "msg", data);
            JObject json = exception.GetJson();
            Assert.AreEqual(42, (int)json["code"]);
            Assert.AreEqual("msg", (string)json["message"]);
            Assert.AreEqual(42, (int)json["data"]["d1"]);
            Assert.AreEqual("42", (string)json["data"]["d2"]);
        }
    }
}
