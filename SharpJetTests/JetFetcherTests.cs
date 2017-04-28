namespace SharpJetTests
{
    using System;
    using FakeItEasy;
    using Hbm.Devices.Jet;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class JetFetcherTests
    {
        [Test]
        public void TestCallFetchCallbackInvokesAction()
        {
            Action<JToken> callback = A.Fake<Action<JToken>>();
            JetFetcher fetcher = new JetFetcher(callback);
            JToken token = JToken.FromObject(42);

            fetcher.CallFetchCallback(token);

            A.CallTo(() => callback.Invoke(token)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void TestCallFetchCallbackDoesNothingIfActionIsNull()
        {
            JetFetcher fetcher = new JetFetcher(null);
            JToken token = JToken.FromObject(42);

            Assert.DoesNotThrow(() => fetcher.CallFetchCallback(token));
        }
    }
}
