using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Web.Http;
using Xunit;
using ODataRoutingPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace SelectTokenizeBug
{
    public class Doc
    {
        [Key]
        public string Id { get; set; }

        public int Rating { get; set; }
    }

    public class SelectReproTests
    {
        private static ODataQueryOptions CreateQueryOptions(string queryString)
        {
            const string DocsEntitySetName = "Docs";

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Doc>(DocsEntitySetName);
            var model = builder.GetEdmModel();

            var config = new HttpConfiguration();
            config.EnableDependencyInjection();
            config.MapODataServiceRoute("odata", null, model);

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost/{DocsEntitySetName}?{queryString}");
            request.SetConfiguration(config);

            var path = new ODataRoutingPath(new EntitySetSegment(model.FindDeclaredEntitySet(DocsEntitySetName)));
            var context = new ODataQueryContext(model, typeof(Doc), path);
            return new ODataQueryOptions(context, request);
        }

        [Fact]
        public void SelectWithSystemTokenThrowsODataException()
        {
            ODataQueryOptions queryOptions = CreateQueryOptions("$select=Rating,$count");
            var e = Assert.Throws<ODataException>(() => queryOptions.SelectExpand.SelectExpandClause);
            Assert.Equal("Found system token '$count' in select or expand clause 'Rating,$count'.", e.Message);
        }

        [Fact]
        public void SelectWithDottedNameThrowsODataException()
        {
            ODataQueryOptions queryOptions = CreateQueryOptions("$select=Rating,Dotted.Name");
            var e = Assert.Throws<ODataException>(() => queryOptions.SelectExpand.SelectExpandClause);

            // This error message doesn't exist in the OData libraries. Consider it a suggestion for what the
            // error message should be.
            const string ExpectedMessage =
                "Found invalid identifier 'Dotted.Name' in $select. If this is the name of a property of a complex type, make sure to " +
                "use slash (/) as the separator instead of dot (.).";

            Assert.Equal(ExpectedMessage, e.Message);
        }
    }
}
