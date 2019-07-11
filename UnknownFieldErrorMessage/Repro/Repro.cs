using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class Repro : TestBase
    {
        // Fixed in Microsoft.AspNet.OData 7.1.0
        [TestMethod]
        public void UnknownFieldsOnNonOpenEntityTypeDisallowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": [],
    ""Unknown"": 123
}";

            const string ExpectedError =
                "The property 'Unknown' does not exist on type 'Repro.Index'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Fixed in Microsoft.ASpNet.OData 7.1.0
        [TestMethod]
        public void UnknownFieldsOnNonOpenComplexTypeDisallowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": [{ ""Name"": ""id"", ""Type"": ""String"", ""FavoriteColor"": ""Green"" }]
}";

            const string ExpectedError =
                "The property 'FavoriteColor' does not exist on type 'Repro.Field'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }
    }
}
