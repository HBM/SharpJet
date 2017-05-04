

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
