using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class StaticallyTypedRepro : TestBase
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

        // Works in Microsoft.AspNet.OData 7.1.0
        [TestMethod]
        public void UnknownCollectionFieldsOnNonOpenEntityTypeDisallowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""NotExactlyFields"": []
}";

            const string ExpectedError = "Cannot find nested property 'NotExactlyFields' on the resource type 'Repro.Index'.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Works in Microsoft.AspNet.OData 7.1.0
        [TestMethod]
        public void UnknownCollectionFieldsOnNonOpenComplexTypeDisallowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": [{ ""Name"": ""id"", ""Type"": ""String"", ""NoSubFieldsHere"": [] }]
}";

            const string ExpectedError = "Cannot find nested property 'NoSubFieldsHere' on the resource type 'Repro.Field'.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // In Microsoft.AspNet.OData 7.1.0, with Microsoft.OData.Core 7.5.3:
        // Error message is unhelpful when ODataMessageReaderSettings.ReadUntypedAsString is true.
        // Fails with nullref when ODataMessageReaderSettings.ReadUntypedAsString is false.
        [TestMethod]
        public void MissingODataTypeAnnotationResultsInHelpfulErrorMessage()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": [],
    ""Analyzers"": [
        {
            ""Name"": ""myanalyzer"",
            ""TokenizerName"": ""mytokenizer"",
            ""TokenFilters"": [
                ""mytokenfilter""
            ]
        }
    ]
}";

            // The error message here doesn't exist in the OData stack; It's just a suggestion on what would actually help end users.
            const string ExpectedError =
                "Failed to deserialize an object at path 'Analyzers[0]' because no @odata.type annotation could be found that maps " +
                "to a concrete type.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }
    }
}
