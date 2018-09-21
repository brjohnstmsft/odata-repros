using System;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Repro
{
    public static class AssertExtensions
    {
        public static HttpResponseMessage AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            Console.WriteLine(response.GetContent());
            Assert.AreEqual(
                expectedStatusCode,
                response.StatusCode,
                $"Expected matching statuses for request {response.RequestMessage.Method} {response.RequestMessage.RequestUri}");

            return response;
        }

        public static HttpResponseMessage AssertOK(this HttpResponseMessage response) =>
            response.AssertStatusCode(HttpStatusCode.OK);

        public static HttpResponseMessage AssertNoContent(this HttpResponseMessage response) =>
            response.AssertStatusCode(HttpStatusCode.NoContent);

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

        public static void AssertJsonEquals(this string actualJson, string expectedJson, string message = null) =>
            JToken.Parse(actualJson).AssertJsonEquals(JToken.Parse(expectedJson), message);

        public static void AssertJsonEquals(this JToken actual, JToken expected, string message = null) =>
            Assert.IsTrue(
                JToken.DeepEquals(expected, actual),
                $"Expected JSON to match. Expected: <{expected}>\nActual: <{actual}> {message}");

        public static void AssertXmlEquals(
            this string actualXml,
            string expectedXml,
            string message,
            params string[] parameters)
        {
            bool NormalizeAndCompare(XElement left, XElement right)
            {
                left = left.Normalize();
                right = right.Normalize();

                return XNode.DeepEquals(left, right);
            }

            var actual = XElement.Parse(actualXml);
            var expected = XElement.Parse(expectedXml);
            if (!NormalizeAndCompare(expected, actual))
            {
                Assert.Fail(message, parameters);
            }
        }
    }
}
