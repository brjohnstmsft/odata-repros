using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class DynamicallyTypedRepro : TestBase
    {
        // Works in Microsoft.AspNet.OData 7.1.0
        [TestMethod]
        public void UnknownFieldsOnNonOpenEntityTypeDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""tags"": [""a""],
    ""notAField"": 123
}";

            const string ExpectedError =
                "The property 'notAField' does not exist on type 'search.document'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Works in Microsoft.ASpNet.OData 7.1.0
        [TestMethod]
        public void UnknownFieldsOnNonOpenComplexTypeDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""123 Main Street"",
            ""city"": ""Redmond"",
            ""planet"": ""Earth""
        }
    ],
    ""tags"": [""a""]
}";

            const string ExpectedError =
                "The property 'planet' does not exist on type 'search.address'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Fails with nullref in Microsoft.AspNet.OData 7.1.0 when ODataMessageReaderSettings.ReadUntypedAsString is false.
        [TestMethod]
        public void UnknownCollectionFieldsOnNonOpenEntityTypeDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""123 Main Street"",
            ""city"": ""Redmond""
        }
    ],
    ""notTags"": [""a""]
}";

            const string ExpectedError =
                "The property 'notTags' does not exist on type 'search.document'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Fails with nullref in Microsoft.AspNet.OData 7.1.0 when ODataMessageReaderSettings.ReadUntypedAsString is false.
        [TestMethod]
        public void UnknownCollectionFieldsOnNonOpenComplexTypeDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""123 Main Street"",
            ""city"": ""Redmond"",
            ""mailboxes"": [""x"", ""y""]
        }
    ],
    ""tags"": [""a""]
}";

            const string ExpectedError =
                "The property 'mailboxes' does not exist on type 'search.address'. Make sure to only use property names that are " +
                "defined by the type.";

            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Pinning test to make sure the model-building code does the right thing.
        [TestMethod]
        public void ReturnsExpectedEdmModel()
        {
            const string ExpectedEdmModel =
@"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Repro"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""Index"">
        <Key>
          <PropertyRef Name=""Name"" />
        </Key>
        <Property Name=""Name"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Fields"" Type=""Collection(Repro.Field)"" />
        <Property Name=""Analyzers"" Type=""Collection(Repro.Analyzer)"" />
      </EntityType>
      <ComplexType Name=""Field"">
        <Property Name=""Name"" Type=""Edm.String"" />
        <Property Name=""Type"" Type=""Edm.String"" />
      </ComplexType>
      <ComplexType Name=""Analyzer"" Abstract=""true"">
        <Property Name=""Name"" Type=""Edm.String"" />
      </ComplexType>
      <ComplexType Name=""CustomAnalyzer"" BaseType=""Repro.Analyzer"">
        <Property Name=""TokenizerName"" Type=""Edm.String"" />
        <Property Name=""TokenFilters"" Type=""Collection(Edm.String)"" />
      </ComplexType>
    </Schema>
    <Schema Namespace=""Default"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"">
        <EntitySet Name=""Indexes"" EntityType=""Repro.Index"" />
        <EntitySet Name=""Docs"" EntityType=""search.document"" />
      </EntityContainer>
    </Schema>
    <Schema Namespace=""search"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <ComplexType Name=""address"">
        <Property Name=""street"" Type=""Edm.String"" />
        <Property Name=""city"" Type=""Edm.String"" />
      </ComplexType>
      <EntityType Name=""document"">
        <Key>
          <PropertyRef Name=""id"" />
        </Key>
        <Property Name=""id"" Type=""Edm.String"" />
        <Property Name=""addresses"" Type=""Collection(search.address)"" Nullable=""false"" />
        <Property Name=""tags"" Type=""Collection(Edm.String)"" Nullable=""false"" />
      </EntityType>
      <Action Name=""UploadAddresses"" IsBound=""true"">
        <Parameter Name=""bindingParameter"" Type=""Collection(search.document)"" />
        <Parameter Name=""value"" Type=""Collection(search.address)"" Nullable=""false"" />
      </Action>
      <Term Name=""action"" Type=""Edm.String"" />
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            string response = Get("odata/$metadata").GetContent();

            response.AssertXmlEquals(ExpectedEdmModel);
        }
    }
}
