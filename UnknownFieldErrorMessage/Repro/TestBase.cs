using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http.SelfHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public abstract class TestBase
    {
        private const string AcceptNoMetadata = "application/json;odata.metadata=none";
        private const string ServerUrl = "http://localhost:9999/";

        private HttpClient _client;
        private HttpSelfHostServer _server;

        [TestInitialize]
        public void TestInitialize()
        {
            var testUrl = new Uri(ServerUrl);
            var config = new HttpSelfHostConfiguration(testUrl);
            Startup.Configure(config);

            _server = new HttpSelfHostServer(config);
            _server.OpenAsync().Wait();

            _client = new HttpClient { BaseAddress = testUrl };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _client?.Dispose();
            _server?.CloseAsync().Wait();
        }

        protected HttpResponseMessage Post(string url, string json)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Post, MakeRelative(url))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(AcceptNoMetadata));
            return _client.SendAsync(request).Result;
        }

        private static Uri MakeRelative(string url) => new Uri(url, UriKind.Relative);
    }
}
