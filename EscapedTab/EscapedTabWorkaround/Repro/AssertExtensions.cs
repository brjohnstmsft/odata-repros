using System;
using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Repro
{
    public static class AssertExtensions
    {
        public static HttpResponseMessage AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            Assert.AreEqual(
                expectedStatusCode,
                response.StatusCode,
                $"Expected matching statuses for request {response.RequestMessage.Method} {response.RequestMessage.RequestUri}. Actual content: {response.GetContent()}");

            return response;
        }

        public static HttpResponseMessage AssertBadRequest(this HttpResponseMessage response) =>
            response.AssertStatusCode(HttpStatusCode.BadRequest);

        public static HttpResponseMessage AssertContains(this HttpResponseMessage response, string expectedString)
        {
            string content = response.GetContent();
            Assert.IsTrue(
                content.Contains(expectedString),
                $"Expected to find '{expectedString}' in response: {content}");
            return response;
        }

        public static string GetContent(this HttpResponseMessage response) => response.Content.ReadAsStringAsync().Result;
    }
}
