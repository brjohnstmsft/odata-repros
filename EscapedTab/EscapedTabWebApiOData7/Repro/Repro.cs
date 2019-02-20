using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class Repro : TestBase
    {
        [TestMethod]
        public void EscapedTabShouldRoundTripCorrectly()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Configuration"": {
        ""myconfig"": ""\t""
    }
}";

            Post("odata/Indexers", Json)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContains(@"""myconfig"":""\t""");
        }
    }
}
