using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class Repro : TestBase
    {
        // True for our test model in particular, not in general.
        [TestMethod]
        public void NullsInDynamicComplexCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""Main Street"",
            ""city"": ""Bellevue""
        },
        null
    ],
    ""tags"": [""x"", ""y""]
}";

            const string ExpectedError =
                "A null value was found with the expected type 'search.address[Nullable=False]'. The expected type " +
                "'search.address[Nullable=False]' does not allow null values.";

            // FAILS by returning 200 (OK) instead of 400 (BadRequest), even though the complex type is non-nullable.
            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // True for our test model in particular, not in general.
        [TestMethod]
        public void NullsInDynamicPrimitiveCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""Main Street"",
            ""city"": ""Bellevue""
        }
    ],
    ""tags"": [""x"", null, ""y""]
}";

            const string ExpectedError =
                "A null value was found with the expected type 'Edm.String[Nullable=False]'. The expected type " +
                "'Edm.String[Nullable=False]' does not allow null values.";

            Post("odata/Docs", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        [TestMethod]
        public void NonNullsInDynamicComplexCollectionsAreAllowed()
        {
            const string Json =
@"{
    ""id"": ""1"",
    ""addresses"": [
        {
            ""street"": ""Main Street"",
            ""city"": ""Bellevue""
        }
    ],
    ""tags"": [""x"", ""y""]
}";

            string actualJson = Post("odata/Docs", Json).AssertOK().GetContent();
            actualJson.AssertJsonEquals(Json);
        }

        // Pinning test to make sure the model-building code does the right thing.
        [TestMethod]
        public void ReturnsExpectedEdmModel()
        {
            const string ExpectedEdmModel =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Default"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"">
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
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            string response = Get("odata/$metadata").GetContent();

            response.AssertXmlEquals(ExpectedEdmModel);
        }
    }
}
