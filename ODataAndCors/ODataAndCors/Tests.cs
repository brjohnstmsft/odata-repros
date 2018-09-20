using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataAndCors
{
    public static class AssertExtensions
    {
        public static void AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(
                expectedStatusCode,
                response.StatusCode,
                $"Expected matching statuses for request {response.RequestMessage.Method} {response.RequestMessage.RequestUri}");
        }

        public static void AssertOK(this HttpResponseMessage response) => response.AssertStatusCode(HttpStatusCode.OK);
    }

    [TestClass]
    public class ODataAndCorsRepro
    {
        private const string ServerUrl = "http://localhost:9999/";

        private IWebHost _server;
        private HttpClient _client;

        [TestInitialize]
        public void TestInitialize()
        {
            _server = BuildWebHost();
            _server.Start();

            _client = new HttpClient() { BaseAddress = new Uri(ServerUrl) };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _client?.Dispose();
            _server?.StopAsync().Wait();
        }

        // PASSES
        [TestMethod]
        public void MakeNonCorsODataRequestExpectSuccess()
        {
            Get("odata/$metadata").AssertOK();
            Get("odata/Indexes").AssertOK();
            Get("odata/Indexes/DoSomething").AssertOK();
        }

        // PASSES
        [TestMethod]
        public void MakeNonCorsNonODataRequestExpectSuccess()
        {
            Get("ping").AssertOK();
            Get("ping/do").AssertOK();
        }

        // PASSES
        [TestMethod]
        public void MakeCorsNonODataRequestExpectSuccess()
        {
            GetPreflight("ping").AssertOK();
            GetPreflight("ping/do").AssertOK();
        }

        // FAILS
        [TestMethod]
        public void MakeCorsODataRequestExpectSuccess()
        {
            GetPreflight("odata/Indexes").AssertOK();
            GetPreflight("odata/Indexes/DoSomething").AssertOK();
        }

        private HttpResponseMessage Get(string url)
        {
            return _client.GetAsync(new Uri(url, UriKind.Relative)).Result;
        }

        private HttpResponseMessage GetPreflight(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, url);
            request.Headers.Add("Origin", "http://localhost");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            return _client.SendAsync(request).Result;
        }

        private static IWebHost BuildWebHost() =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(ServerUrl)
                .Build();
    }
}
