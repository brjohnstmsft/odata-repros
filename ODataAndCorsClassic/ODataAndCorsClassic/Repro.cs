using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataAndCorsClassic
{
    public class Field
    {
        public string Name { get; set; }

        public string Type { get; set; }
    }

    public class Index
    {
        [Key]
        public string Name { get; set; }

        public IList<Field> Fields { get; set; }
    }

    public class IndexesController : ODataController
    {
        [HttpGet]
        [EnableCors("*", "*", "*")]
        public IList<Index> Get() =>
            new[] {
                new Index()
                {
                    Name = "myindex",
                    Fields = new[]
                    {
                        new Field() { Name = "a", Type = "int" },
                        new Field() { Name = "b", Type = "string" }
                    }
                }
            };

        [HttpGet]
        [EnableCors("*", "*", "*")]
        public int DoSomething() => 0;
    }

    public class PingController : ApiController
    {
        [HttpGet]
        [EnableCors("*", "*", "*")]
        public IHttpActionResult Get() => Ok();

        [HttpGet]
        [EnableCors("*", "*", "*")]
        public int DoSomething() => 0;
    }

    public class WorkaroundHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine("Running workaround...");
            EnsureRouteData(request);
            return await base.SendAsync(request, cancellationToken);
        }

        private static void EnsureRouteData(HttpRequestMessage request)
        {
            IHttpRouteData routeData = request.GetRouteData();
            if (routeData == null)
            {
                HttpConfiguration config = request.GetConfiguration();

                if (config != null)
                {
                    config.Routes.GetRouteData(request);
                }
            }
        }
    }

    public static class Startup
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            IEdmModel model = BuildModel();
            httpConfig.Routes.MapHttpRoute("ping", "ping", new { controller = "Ping", action = "Get" });
            httpConfig.Routes.MapHttpRoute("dosomething", "ping/do", new { controller = "Ping", action = "DoSomething" });
            httpConfig.MapODataServiceRoute("odata", "odata", model);
            httpConfig.EnableDependencyInjection();
            httpConfig.EnableCors();
            httpConfig.MessageHandlers.Insert(0, new WorkaroundHandler());  // Must be before the CORS handler.
        }

        private static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder
                .EntitySet<Index>("Indexes")
                .EntityType
                .Function("DoSomething")
                .Returns<int>();

            return builder.GetEdmModel();
        }
    }

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
    }
}
