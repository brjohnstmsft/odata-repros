using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Repro
{
    [TestClass]
    public sealed class Repro : TestBase
    {
        [TestMethod]
        public void ClrBasedCollectionsAreNullableByDefault()
        {
            IEdmModel model = StaticModel.BuildModel();
            var indexType = (IEdmEntityType)model.FindDeclaredType(typeof(Index).FullName);
            IEdmProperty fieldsProperty = indexType.FindProperty(nameof(Index.Fields));

            Assert.AreEqual(true, fieldsProperty.Type.IsCollection());

            IEdmCollectionTypeReference collectionType = fieldsProperty.Type.AsCollection();

            Assert.AreEqual(true, collectionType.IsNullable);
            Assert.AreEqual(true, collectionType.ElementType().IsNullable);
        }

        // It seems fair that this doesn't work, since the OData V4 spec says that empty collections must be
        // represented as empty arrays: http://docs.oasis-open.org/odata/odata-json-format/v4.01/cs01/odata-json-format-v4.01-cs01.html#sec_CollectionofPrimitiveValues
        // However, the returned error message mentions the incorrect type (it should be Nullable=True).
        [TestMethod]
        public void NullClrCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": null
}";

            const string ExpectedError =
                "A null value was found for the property named 'Fields', which has the expected type " +
                "'Collection(Repro.Field)[Nullable=False]'. The expected type 'Collection(Repro.Field)[Nullable=False]' " +
                "does not allow null values.";

            Post("odata/Indexes", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // It seems fair that this doesn't work, since the OData V4 spec says that empty collections must be
        // represented as empty arrays: http://docs.oasis-open.org/odata/odata-json-format/v4.01/cs01/odata-json-format-v4.01-cs01.html#sec_CollectionofPrimitiveValues
        [TestMethod]
        public void NullDynamicComplexCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Main Street"",
                    ""city"": ""Bellevue""
                }
            ],
            ""tags"": [""x"", ""y""]
        },
        {
            ""id"": ""2"",
            ""addresses"": null,
            ""tags"": [""a"", ""b""]
        }
    ]
}";

            const string ExpectedError =
                "A null value was found for the property named 'addresses', which has the expected type " +
                "'Collection(search.address)[Nullable=False]'. The expected type 'Collection(search.address)[Nullable=False]' " +
                "does not allow null values.";

            Post("odata/Docs/search.Index()", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // It seems fair that this doesn't work, since the OData V4 spec says that empty collections must be
        // represented as empty arrays: http://docs.oasis-open.org/odata/odata-json-format/v4.01/cs01/odata-json-format-v4.01-cs01.html#sec_CollectionofPrimitiveValues
        [TestMethod]
        public void NullDynamicPrimitiveCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""tags"": [""x"", ""y""]
        },
        {
            ""id"": ""2"",
            ""tags"": null
        }
    ]
}";

            const string ExpectedError =
                "A null value was found for the property named 'tags', which has the expected type " +
                "'Collection(Edm.String)[Nullable=False]'. The expected type 'Collection(Edm.String)[Nullable=False]' " +
                "does not allow null values.";

            Post("odata/Docs/search.Index()", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        [TestMethod]
        public void EmptyClrCollectionsAreAllowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": []
}";

            string actualjson = Post("odata/Indexes", Json).AssertOK().GetContent();
            actualjson.AssertJsonEquals(Json);
        }

        [TestMethod]
        public void EmptyDynamicComplexCollectionsAreAllowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Main Street"",
                    ""city"": ""Bellevue""
                }
            ],
            ""tags"": [""x"", ""y""]
        },
        {
            ""id"": ""2"",
            ""addresses"": [],
            ""tags"": [""a"", ""b""]
        }
    ]
}";

            Post("odata/Docs/search.Index()", Json).AssertNoContent();
            string docsJson = Get("odata/Docs").AssertOK().GetContent();
            Delete("odata/Docs('0')").AssertNoContent();

            docsJson.AssertJsonEquals(Json);
        }

        [TestMethod]
        public void EmptyDynamicPrimitiveCollectionsAreAllowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Main Street"",
                    ""city"": ""Bellevue""
                }
            ],
            ""tags"": [""x"", ""y""]
        },
        {
            ""id"": ""2"",
            ""addresses"": [
                {
                    ""street"": ""Broadway"",
                    ""city"": ""Vancouver""
                }
            ],
            ""tags"": []
        }
    ]
}";

            Post("odata/Docs/search.Index()", Json).AssertNoContent();
            string docsJson = Get("odata/Docs").AssertOK().GetContent();
            Delete("odata/Docs('0')").AssertNoContent();

            docsJson.AssertJsonEquals(Json);
        }

        [TestMethod]
        public void OmittedClrCollectionsComeBackEmpty()
        {
            const string InputJson =
@"{
    ""Name"": ""test""
}";

            const string ExpectedJson =
@"{
    ""Name"": ""test"",
    ""Fields"": []
}";

            string actualJson = Post("odata/Indexes", InputJson).AssertOK().GetContent();
            actualJson.AssertJsonEquals(ExpectedJson);
        }

        [TestMethod]
        public void OmittedDynamicComplexCollectionsComeBackEmpty()
        {
            const string InputJson =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""tags"": [""a"", ""b""]
        }
    ]
}";

            const string ExpectedJson =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [],
            ""tags"": [""a"", ""b""]
        }
    ]
}";

            Post("odata/Docs/search.Index()", InputJson).AssertNoContent();
            string docsJson = Get("odata/Docs").AssertOK().GetContent();
            Delete("odata/Docs('0')").AssertNoContent();

            docsJson.AssertJsonEquals(ExpectedJson);
        }

        [TestMethod]
        public void OmittedDynamicPrimitiveCollectionsComeBackEmpty()
        {
            const string InputJson =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Broadway"",
                    ""city"": ""Vancouver""
                }
            ]
        }
    ]
}";

            const string ExpectedJson =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Broadway"",
                    ""city"": ""Vancouver""
                }
            ],
            ""tags"": []
        }
    ]
}";

            Post("odata/Docs/search.Index()", InputJson).AssertNoContent();
            string docsJson = Get("odata/Docs").AssertOK().GetContent();
            Delete("odata/Docs('0')").AssertNoContent();

            docsJson.AssertJsonEquals(ExpectedJson);
        }

        [TestMethod]
        public void NullsInClrCollectionsAreAllowed()
        {
            const string Json =
@"{
    ""Name"": ""test"",
    ""Fields"": [
        { ""Name"": ""Id"", ""Type"": ""String"" },
        null,
        { ""Name"": ""Rating"", ""Type"": ""Int32"" }
    ]
}";

            string actualJson = Post("odata/Indexes", Json).AssertOK().GetContent();
            actualJson.AssertJsonEquals(Json);
        }

        // True for our test model in particular, not in general.
        [TestMethod]
        public void NullsInDynamicComplexCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Main Street"",
                    ""city"": ""Bellevue""
                },
                null
            ],
            ""tags"": [""x"", ""y""]
        }
    ]
}";

            const string ExpectedError =
                "A null value was found with the expected type 'search.address[Nullable=False]'. The expected type " +
                "'search.address[Nullable=False]' does not allow null values.";

            Post("odata/Docs/search.Index()", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // True for our test model in particular, not in general.
        [TestMethod]
        public void NullsInDynamicPrimitiveCollectionsAreDisallowed()
        {
            const string Json =
@"{
    ""value"": [
        {
            ""id"": ""1"",
            ""addresses"": [
                {
                    ""street"": ""Main Street"",
                    ""city"": ""Bellevue""
                }
            ],
            ""tags"": [""x"", null, ""y""]
        }
    ]
}";

            const string ExpectedError =
                "A null value was found with the expected type 'Edm.String[Nullable=False]'. The expected type " +
                "'Edm.String[Nullable=False]' does not allow null values.";

            Post("odata/Docs/search.Index()", Json)
                .AssertBadRequest()
                .AssertContains(ExpectedError);
        }

        // Pinning test to make sure the model-building code does the right thing.
        [TestMethod]
        public void ReturnsExpectedEdmModel()
        {
            const string ExpectedEdmModel =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Repro"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""Index"">
        <Key>
          <PropertyRef Name=""Name"" />
        </Key>
        <Property Name=""Name"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Fields"" Type=""Collection(Repro.Field)"" />
      </EntityType>
      <ComplexType Name=""Field"">
        <Property Name=""Name"" Type=""Edm.String"" />
        <Property Name=""Type"" Type=""Edm.String"" />
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
      <ComplexType Name=""documentFields"">
        <Property Name=""id"" Type=""Edm.String"" />
        <Property Name=""addresses"" Type=""Collection(search.address)"" Nullable=""false"" />
        <Property Name=""tags"" Type=""Collection(Edm.String)"" Nullable=""false"" />
      </ComplexType>
      <Action Name=""Index"" IsBound=""true"">
        <Parameter Name=""bindingParameter"" Type=""Collection(search.document)"" />
        <Parameter Name=""value"" Type=""Collection(search.documentFields)"" Nullable=""false"" />
      </Action>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            string response = Get("odata/$metadata").GetContent();

            response.AssertXmlEquals(ExpectedEdmModel, "EDM model shouldn't differ from baseline.");
        }
    }
}
