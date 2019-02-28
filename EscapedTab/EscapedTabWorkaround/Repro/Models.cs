using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;

namespace Repro
{
    public class Indexer
    {
        [Key]
        public string Name { get; set; }

        public IndexerConfiguration Configuration { get; set; }
    }

    public class IndexerConfiguration
    {
        public IDictionary<string, object> Properties { get; set; }
    }

    public static class Model
    {
        public static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Indexer>("Indexers");
            return builder.GetEdmModel();
        }
    }
}
